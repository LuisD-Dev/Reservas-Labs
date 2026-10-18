using SistemaReservas.API.DTOs;
using SistemaReservas.API.Models;
using SistemaReservas.API.Reglas;
using SistemaReservas.API.Repositories;

namespace SistemaReservas.API.Services
{
    public class ReservaService
    {
        public const string MensajeConflicto =
            "El laboratorio ya tiene una reserva que se traslapa con ese horario.";

        private readonly IReservaRepository
            _reservaRepository;
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
            var errores =
                ReglasReserva.ValidarCamposObligatorios(
                    request.LaboratorioId,
                    request.Fecha,
                    request.HoraInicio,
                    request.HoraFin);

            var errorHoraFin =
                ReglasReserva.ValidarHoraFinalizacion(
                    request.HoraInicio,
                    request.HoraFin);

            if (errorHoraFin is not null)
            {
                errores["horaFin"] = errorHoraFin;
            }

            // HU5 - AB#69 y AB#70: horario de funcionamiento y duración máxima.
            // Solo se evalúan si las horas ya son válidas, para no mezclar mensajes.
            if (!errores.ContainsKey("horaInicio") &&
                !errores.ContainsKey("horaFin"))
            {
                var errorHorario =
                    ReglasReserva.ValidarHorarioFuncionamiento(
                        request.HoraInicio,
                        request.HoraFin)
                    ?? ReglasReserva.ValidarDuracionMaxima(
                        request.HoraInicio,
                        request.HoraFin);

                if (errorHorario is not null)
                {
                    errores["horaFin"] = errorHorario;
                }
            }

            if (request.Fecha.HasValue)
            {
                var hoy = DateOnly.FromDateTime(
                    _timeProvider.GetLocalNow().DateTime);
                var errorFecha = ReglasReserva.ValidarFechaReserva(
                    request.Fecha.Value,
                    hoy);

                if (errorFecha is not null)
                {
                    errores["fecha"] = errorFecha;
                }
            }

            if (errores.Count > 0)
            {
                return ResultadoCrearReserva.Fallido(
                    errores);
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
                LaboratorioId =
                    request.LaboratorioId!.Value,

                UsuarioId = usuarioId,

                Fecha = request.Fecha!.Value
                    .ToDateTime(TimeOnly.MinValue),

                HoraInicio =
                    request.HoraInicio!.Value,

                HoraFin =
                    request.HoraFin!.Value,

                Estado = "Activa"
            };

            var guardada =
                await _reservaRepository
                    .CrearAsync(reserva);

            return ResultadoCrearReserva.Correcto(
                guardada);
        }
    }
}
