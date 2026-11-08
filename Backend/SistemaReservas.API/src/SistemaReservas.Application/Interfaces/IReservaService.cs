using SistemaReservas.Domain.DTO;

namespace SistemaReservas.Application.Interfaces
{
    public interface IReservaService
    {
        Task<ResultadoCrearReserva> CrearAsync(int usuarioId, CrearReservaRequest request);

        Task<ResultadoCancelarReserva> CancelarAsync(
            int reservaId, int usuarioId, bool esAdministrador = false);

        Task<IReadOnlyList<MiReservaResponse>> ObtenerMisReservasAsync(int usuarioId);
    }
}
