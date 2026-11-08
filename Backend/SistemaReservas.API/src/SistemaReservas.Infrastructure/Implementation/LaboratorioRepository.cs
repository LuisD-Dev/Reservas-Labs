using SistemaReservas.Domain.Rules;
using SistemaReservas.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using SistemaReservas.Infrastructure.Data;
using SistemaReservas.Domain.Entities;

namespace SistemaReservas.Infrastructure.Implementation
{
    // NFR2 - Separar acceso a datos: implementación con EF Core.
    public class LaboratorioRepository : ILaboratorioRepository
    {
        private readonly AppDbContext _context;

        public LaboratorioRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Laboratorio>> ObtenerTodosAsync(bool soloHabilitados = false)
        {
            return await _context.Laboratorios
                .AsNoTracking()
                .Where(l => !soloHabilitados || l.Estado == ReglasEstadoLaboratorio.Habilitado)
                .OrderBy(l => l.Nombre)
                .ToListAsync();
        }

        public Task<Laboratorio?> ObtenerPorIdAsync(int id)
        {
            return _context.Laboratorios.SingleOrDefaultAsync(l => l.Id == id);
        }

        public async Task GuardarCambiosAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
