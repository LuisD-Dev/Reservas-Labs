using SistemaReservas.API.DTOs;
using SistemaReservas.API.Models;
using SistemaReservas.API.Reglas;
using SistemaReservas.API.Repositories;

namespace SistemaReservas.API.Services
{
    public class ReservaService
    {
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

            var errorHoraFin =
                ReglasReserva.ValidarHoraFinalizacion(
                    request.HoraInicio,
                    request.HoraFin);

            if (errorHoraFin is not null)
            {
                errores["horaFin"] = errorHoraFin;
            }

            if (errores.Count > 0)
            {
                return ResultadoCrearReserva.Fallido(
                    errores);
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
