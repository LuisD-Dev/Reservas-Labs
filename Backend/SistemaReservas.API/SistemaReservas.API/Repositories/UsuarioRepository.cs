using Microsoft.EntityFrameworkCore;
using SistemaReservas.API.Data;
using SistemaReservas.API.Models;

namespace SistemaReservas.API.Repositories
{
    // NFR2 - Separar acceso a datos: implementación con EF Core.
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

        // HU1 - #20 El incremento se hace en la base de datos con un solo UPDATE
        // (IntentosFallidos = IntentosFallidos + 1), así dos intentos simultáneos
        // no se pisan. Después se lee el valor actualizado.
        public async Task<int> RegistrarIntentoFallidoAsync(int usuarioId)
        {
            var filas = await _context.Usuarios
                .Where(u => u.Id == usuarioId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.IntentosFallidos, u => u.IntentosFallidos + 1));

            if (filas == 0)
            {
                throw new InvalidOperationException($"Usuario {usuarioId} no encontrado.");
            }

            return await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Id == usuarioId)
                .Select(u => u.IntentosFallidos)
                .SingleAsync();
        }

        // Un solo UPDATE en la base de datos, igual que el incremento: no depende
        // de que el usuario esté (o no) cargado en el contexto de EF.
        public async Task ReiniciarIntentosAsync(int usuarioId)
        {
            var filas = await _context.Usuarios
                .Where(u => u.Id == usuarioId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.IntentosFallidos, 0)
                    .SetProperty(u => u.BloqueadoHasta, (DateTime?)null));

            if (filas == 0)
            {
                throw new InvalidOperationException($"Usuario {usuarioId} no encontrado.");
            }
        }

        // HU1 - #21 Un solo UPDATE: guarda hasta cuándo queda bloqueado y reinicia
        // el contador para que, al vencer el bloqueo, tenga otros 5 intentos.
        public async Task BloquearAsync(int usuarioId, DateTime bloqueadoHasta)
        {
            var filas = await _context.Usuarios
                .Where(u => u.Id == usuarioId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.BloqueadoHasta, (DateTime?)bloqueadoHasta)
                    .SetProperty(u => u.IntentosFallidos, 0));

            if (filas == 0)
            {
                throw new InvalidOperationException($"Usuario {usuarioId} no encontrado.");
            }
        }
    }
}
