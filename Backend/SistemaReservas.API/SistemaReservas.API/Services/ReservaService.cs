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

        public ReservaService(
            IReservaRepository reservaRepository)
        {
            _reservaRepository = reservaRepository;
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
