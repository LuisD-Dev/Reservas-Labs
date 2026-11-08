using SistemaReservas.Domain.Entities;

namespace SistemaReservas.Application.Abstractions.Persistence
{
    // NFR2 - Separa el acceso a datos de la lógica de reservas.
    public interface IReservaRepository
    {
        Task<Reserva> CrearAsync(Reserva reserva);

        // HU6 - AB#75: reservas activas de un laboratorio en una fecha.
        Task<IReadOnlyList<Reserva>> ObtenerActivasPorLaboratorioYFechaAsync(
            int laboratorioId,
            DateTime fecha);

        // HU8: busca una reserva y guarda los cambios (cancelación).
        Task<Reserva?> ObtenerPorIdAsync(int reservaId);

        Task GuardarCambiosAsync();

        // HU7: reservas de un usuario, con el nombre del laboratorio.
        Task<IReadOnlyList<Reserva>> ObtenerPorUsuarioAsync(int usuarioId);
    }
}