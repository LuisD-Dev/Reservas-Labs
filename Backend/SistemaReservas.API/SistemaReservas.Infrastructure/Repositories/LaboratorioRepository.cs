using SistemaReservas.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using SistemaReservas.Infrastructure.Data;
using SistemaReservas.Domain.Entities;

namespace SistemaReservas.Infrastructure.Repositories
{
    // NFR2 - Separar acceso a datos: implementación con EF Core.
    public class LaboratorioRepository : ILaboratorioRepository
    {
        private readonly SistemaReservasDbContext _context;

        public LaboratorioRepository(SistemaReservasDbContext context)
        {
            _context = context;
        }

        public async Task<List<Laboratorio>> ObtenerTodosAsync()
        {
            return await _context.Laboratorios
                .AsNoTracking()
                .OrderBy(l => l.Nombre)
                .ToListAsync();
        }
    }
}
