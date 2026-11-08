using SistemaReservas.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using SistemaReservas.Infrastructure.Data;
using SistemaReservas.Domain.Entities;

namespace SistemaReservas.Infrastructure.Repositories
{
    // NFR2 - Separar acceso a datos: implementación con EF Core.
    public class DisponibilidadRepository : IDisponibilidadRepository
    {
        private readonly SistemaReservasDbContext _context;

        public DisponibilidadRepository(SistemaReservasDbContext context)
        {
            _context = context;
        }

        public async Task<List<Disponibilidad>> ObtenerPorLaboratorioAsync(int laboratorioId)
        {
            return await _context.Disponibilidades
                .AsNoTracking()
                .Where(d => d.LaboratorioId == laboratorioId)
                .OrderBy(d => d.Fecha)
                .ThenBy(d => d.HoraInicio)
                .ToListAsync();
        }
    }
}
