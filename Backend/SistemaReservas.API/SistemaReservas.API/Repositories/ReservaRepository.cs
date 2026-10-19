using Microsoft.EntityFrameworkCore;
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

        // HU6 - AB#75: solo las reservas activas bloquean el horario.
        public async Task<IReadOnlyList<Reserva>> ObtenerActivasPorLaboratorioYFechaAsync(
            int laboratorioId,
            DateTime fecha)
        {
            var dia = fecha.Date;

            return await _context.Reservas
                .AsNoTracking()
                .Where(reserva =>
                    reserva.LaboratorioId == laboratorioId &&
                    reserva.Fecha == dia &&
                    reserva.Estado == "Activa")
                .ToListAsync();
        }
    }
}
