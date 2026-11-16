using SistemaReservas.Domain.DTO;

namespace SistemaReservas.Application.Interfaces
{
    public interface IReservaService
    {
        Task<ResultadoCrearReserva> CrearAsync(int usuarioId, CrearReservaRequest request);

        Task<ResultadoCancelarReserva> CancelarAsync(
            int reservaId, int usuarioId, bool esAdministrador = false);

        Task<IReadOnlyList<MiReservaResponse>> ObtenerMisReservasAsync(int usuarioId);

        // Gestión de Reservas (Administrador): todas las reservas del sistema.
        Task<IReadOnlyList<ReservaGestionResponse>> ObtenerTodasAsync();

        // Gestión de Reservas (Administrador): registra una reserva a nombre de otro usuario.
        Task<ResultadoCrearReserva> CrearParaUsuarioAsync(CrearReservaParaUsuarioRequest request);
    }
}
