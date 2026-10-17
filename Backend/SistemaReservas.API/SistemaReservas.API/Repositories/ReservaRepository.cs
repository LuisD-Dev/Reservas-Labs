using SistemaReservas.API.Data;
using SistemaReservas.API.Models;

namespace SistemaReservas.API.Repositories
{
    // NFR2 - Implementación de reservas mediante EF Core.
    public class ReservaRepository : IReservaRepository
    {
        private readonly SistemaReservasDbContext _context;

        public ReservaRepository(
            SistemaReservasDbContext context)
        {
            _context = context;
        }

        public async Task<Reserva> CrearAsync(
            Reserva reserva)
        {
            _context.Reservas.Add(reserva);

            await _context.SaveChangesAsync();

            return reserva;
        }
    }
}
