using SistemaReservas.Application.DTOs;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Domain.Rules;
using SistemaReservas.Application.Abstractions.Persistence;

namespace SistemaReservas.Application.Services
{
    public class ReservaService
    {
        public const string MensajeConflicto =
            "El laboratorio ya tiene una reserva que se traslapa con ese horario.";

        private readonly IReservaRepository _reservaRepository;
        private readonly TimeProvider _timeProvider;

        public ReservaService(
            IReservaRepository reservaRepository,
            TimeProvider timeProvider)
        {
            _reservaRepository = reservaRepository;
            _timeProvider = timeProvider;
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

            // HU6 - AB#75: no se permite traslapar una reserva activa del mismo
            // laboratorio en la misma fecha.
            var reservasDelDia =
                await _reservaRepository
                    .ObtenerActivasPorLaboratorioYFechaAsync(
                        request.LaboratorioId!.Value,
                        request.Fecha!.Value.ToDateTime(TimeOnly.MinValue));

            var hayConflicto = reservasDelDia.Any(existente =>
                ReglasTraslape.HayTraslape(
                    request.HoraInicio!.Value,
                    request.HoraFin!.Value,
                    existente.HoraInicio,
                    existente.HoraFin));

            if (hayConflicto)
            {
                return ResultadoCrearReserva.Fallido(
                    new Dictionary<string, string>
                    {
                        ["conflicto"] = MensajeConflicto
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

            return ResultadoCrearReserva.Correcto(guardada);
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

            return reservas.Select(reserva => new MiReservaResponse(
                reserva.Id,
                reserva.LaboratorioId,
                reserva.Laboratorio.Nombre,
                DateOnly.FromDateTime(reserva.Fecha),
                reserva.HoraInicio,
                reserva.HoraFin,
                reserva.Estado
            )).ToList();
        }
    }
}