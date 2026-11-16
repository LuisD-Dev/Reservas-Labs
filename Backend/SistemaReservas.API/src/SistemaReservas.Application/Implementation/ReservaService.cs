using MapsterMapper;
using SistemaReservas.Application.Interfaces;
using SistemaReservas.Domain.DTO;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Domain.Rules;
using SistemaReservas.Infrastructure.Interfaces;

namespace SistemaReservas.Application.Implementation
{
    public class ReservaService : IReservaService
    {
        public const string MensajeConflicto =
            ReglasTraslape.MensajeConflicto;

        private readonly IMapper _mapper;
        private readonly IVerificacionDisponibilidadService _verificacion;
        private readonly IReservaRepository _reservaRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly TimeProvider _timeProvider;

        public ReservaService(
            IReservaRepository reservaRepository,
            TimeProvider timeProvider, IMapper mapper,
            IVerificacionDisponibilidadService verificacion,
            IUsuarioRepository usuarioRepository)
        {
            _reservaRepository = reservaRepository;
            _timeProvider = timeProvider;
            _mapper = mapper;
            _verificacion = verificacion;
            _usuarioRepository = usuarioRepository;
        }

        public async Task<ResultadoCrearReserva> CrearAsync(
            int usuarioId,
            CrearReservaRequest request)
        {
            var errores = ReglasReserva.ValidarSolicitud(
                request.LaboratorioId, request.Fecha, request.HoraInicio, request.HoraFin,
                DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime));

            if (errores.Count > 0)
            {
                return ResultadoCrearReserva.Fallido(errores);
            }

            var disponibilidad = await _verificacion.VerificarAsync(
                request.LaboratorioId!.Value,
                new ConsultaDisponibilidadRequest(request.Fecha, request.HoraInicio, request.HoraFin));

            if (disponibilidad.Errores.Count > 0)
                return ResultadoCrearReserva.Fallido(disponibilidad.Errores);

            if (!disponibilidad.Disponible)
            {
                var clave = disponibilidad.Motivo == ReglasTraslape.MensajeConflicto
                    ? "conflicto" : "disponibilidad";
                return ResultadoCrearReserva.Fallido(new Dictionary<string, string>
                {
                    [clave] = disponibilidad.Motivo
                });
            }

            var reserva = new Reserva
            {
                LaboratorioId = request.LaboratorioId!.Value,
                UsuarioId = usuarioId,
                Fecha = request.Fecha!.Value
                    .ToDateTime(TimeOnly.MinValue),
                HoraInicio = request.HoraInicio!.Value,
                HoraFin = request.HoraFin!.Value,
                Estado = "Activa"
            };

            var guardada =
                await _reservaRepository.CrearAsync(reserva);

            return ResultadoCrearReserva.Correcto(_mapper.Map<ReservaCreadaResponse>(guardada));
        }

        // HU8 - AB#99 y AB#100: el propietario cancela la suya y el Administrador puede cancelar cualquiera.
        public async Task<ResultadoCancelarReserva> CancelarAsync(
            int reservaId,
            int usuarioId,
            bool esAdministrador = false)
        {
            if (usuarioId <= 0)
            {
                return new ResultadoCancelarReserva(
                    EstadoResultadoCancelacion.UsuarioInvalido,
                    "No fue posible identificar al usuario autenticado.");
            }

            var reserva =
                await _reservaRepository.ObtenerPorIdAsync(reservaId);

            if (reserva is null)
            {
                return new ResultadoCancelarReserva(
                    EstadoResultadoCancelacion.NoEncontrada,
                    "La reserva no existe.");
            }

            if (!ReglasCancelacion.PuedeCancelar(reserva.UsuarioId, usuarioId, esAdministrador))
            {
                return new ResultadoCancelarReserva(
                    EstadoResultadoCancelacion.NoAutorizada,
                    "No tienes permiso para cancelar esta reserva.");
            }

            if (reserva.Estado == "Cancelada")
            {
                return new ResultadoCancelarReserva(
                    EstadoResultadoCancelacion.YaCancelada,
                    "La reserva ya está cancelada.");
            }

            if (reserva.Estado != "Activa")
            {
                return new ResultadoCancelarReserva(
                    EstadoResultadoCancelacion.EstadoInvalido,
                    "La reserva no se encuentra en un estado que permita cancelarla.");
            }

            reserva.Estado = "Cancelada";

            await _reservaRepository.GuardarCambiosAsync();

            return new ResultadoCancelarReserva(
                EstadoResultadoCancelacion.Exitosa,
                "Reserva cancelada correctamente.");
        }
        public async Task<IReadOnlyList<MiReservaResponse>>
    ObtenerMisReservasAsync(int usuarioId)
        {
            if (usuarioId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(usuarioId));
            }

            var reservas =
                await _reservaRepository.ObtenerPorUsuarioAsync(usuarioId);

            return _mapper.Map<List<MiReservaResponse>>(reservas);
        }

        // Gestión de Reservas: el Administrador ve las reservas de todos los usuarios.
        public async Task<IReadOnlyList<ReservaGestionResponse>> ObtenerTodasAsync()
        {
            var reservas = await _reservaRepository.ObtenerTodasAsync();

            return _mapper.Map<List<ReservaGestionResponse>>(reservas);
        }

        // Gestión de Reservas: el Administrador registra una reserva a nombre de otro usuario.
        // Se aplican las mismas validaciones, disponibilidad y conflictos que en CrearAsync.
        public async Task<ResultadoCrearReserva> CrearParaUsuarioAsync(
            CrearReservaParaUsuarioRequest request)
        {
            var username = request.Username?.Trim();

            if (string.IsNullOrEmpty(username))
            {
                return ResultadoCrearReserva.Fallido(new Dictionary<string, string>
                {
                    ["username"] = "Indica el usuario responsable de la reserva."
                });
            }

            var usuario = await _usuarioRepository.ObtenerPorUsernameAsync(username);

            if (usuario is null)
            {
                return ResultadoCrearReserva.Fallido(new Dictionary<string, string>
                {
                    ["username"] = "No existe un usuario con ese nombre de usuario."
                });
            }

            return await CrearAsync(
                usuario.Id,
                new CrearReservaRequest(
                    request.LaboratorioId,
                    request.Fecha,
                    request.HoraInicio,
                    request.HoraFin));
        }
    }
}