using Microsoft.EntityFrameworkCore;
using SistemaReservas.API.Data;
using SistemaReservas.API.Models;

namespace SistemaReservas.API.Repositories
{
    // NFR2 - Separar acceso a datos: implementación con SQL Server.
    // Las consultas son las mismas que antes estaban en AuthService.
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly SistemaReservasDbContext _context;

        public UsuarioRepository(SistemaReservasDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> ObtenerPorUsernameAsync(string username)
        {
            return await _context.Usuarios
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Username == username);
        }

        public async Task<int> RegistrarIntentoFallidoAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario is null)
            {
                throw new InvalidOperationException($"Usuario {usuarioId} no encontrado.");
            }

            usuario.IntentosFallidos++;
            await _context.SaveChangesAsync();

            return usuario.IntentosFallidos;
        }

        public async Task ReiniciarIntentosAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario is null)
            {
                throw new InvalidOperationException($"Usuario {usuarioId} no encontrado.");
            }

            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
            await _context.SaveChangesAsync();
        }

        public async Task BloquearAsync(int usuarioId, DateTime bloqueadoHasta)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario is null)
            {
                throw new InvalidOperationException($"Usuario {usuarioId} no encontrado.");
            }

            usuario.BloqueadoHasta = bloqueadoHasta;
            usuario.IntentosFallidos = 0;
            await _context.SaveChangesAsync();
        }
    }
}
