using SistemaReservas.Application.DTOs;

namespace SistemaReservas.Application.Abstractions.Services
{
    public interface IReservaService
    {
        Task<ResultadoCrearReserva> CrearAsync(int usuarioId, CrearReservaRequest request);

        Task<ResultadoCancelarReserva> CancelarAsync(
            int reservaId, int usuarioId, bool esAdministrador = false);

        Task<IReadOnlyList<MiReservaResponse>> ObtenerMisReservasAsync(int usuarioId);
    }
}
