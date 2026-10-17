using SistemaReservas.API.Models;

namespace SistemaReservas.API.Repositories
{
    // NFR2 - Separa el acceso a datos de la lógica de reservas.
    public interface IReservaRepository
    {
        Task<Reserva> CrearAsync(Reserva reserva);
    }
}
