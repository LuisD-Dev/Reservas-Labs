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

        // HU8: busca una reserva con seguimiento, para poder modificarla.
        public async Task<Reserva?> ObtenerPorIdAsync(int reservaId)
        {
            return await _context.Reservas
                .SingleOrDefaultAsync(reserva => reserva.Id == reservaId);
        }

        public async Task GuardarCambiosAsync()
        {
            await _context.SaveChangesAsync();
        }

        // HU7: reservas de un usuario, con el nombre del laboratorio.
        public async Task<IReadOnlyList<Reserva>> ObtenerPorUsuarioAsync(
            int usuarioId)
        {
            return await _context.Reservas
                .AsNoTracking()
                .Include(reserva => reserva.Laboratorio)
                .Where(reserva => reserva.UsuarioId == usuarioId)
                .OrderBy(reserva => reserva.Fecha)
                .ThenBy(reserva => reserva.HoraInicio)
                .ThenBy(reserva => reserva.Id)
                .ToListAsync();
        }
    }
}