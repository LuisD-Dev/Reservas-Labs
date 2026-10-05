using SistemaReservas.API.Models;
using SistemaReservas.API.Repositories;

namespace SistemaReservas.Tests.Fakes
{
    // NFR2 - Doble de prueba de IUsuarioRepository: guarda los usuarios en memoria
    // e imita lo que hacen las consultas SQL de UsuarioRepository.
    // Registra cada llamada para poder verificar el orden y las escrituras.
    public class FakeUsuarioRepository : IUsuarioRepository
    {
        private readonly Dictionary<string, Usuario> _usuarios = new();

        public List<string> Llamadas { get; } = new();

        public void Agregar(Usuario usuario)
        {
            _usuarios[usuario.Username] = usuario;
        }

        // Estado guardado, como quedaría en la tabla Usuarios.
        public Usuario Obtener(string username)
        {
            return _usuarios[username];
        }

        public Task<Usuario?> ObtenerPorUsernameAsync(string username)
        {
            Llamadas.Add(nameof(ObtenerPorUsernameAsync));

            // Devuelve una copia, como si se leyera de la base de datos.
            return Task.FromResult(_usuarios.TryGetValue(username, out var usuario)
                ? Copiar(usuario)
                : null);
        }

        // Igual que UPDATE ... OUTPUT INSERTED.IntentosFallidos: suma 1 y devuelve el nuevo valor.
        public Task<int> RegistrarIntentoFallidoAsync(int usuarioId)
        {
            Llamadas.Add(nameof(RegistrarIntentoFallidoAsync));

            var usuario = BuscarPorId(usuarioId);
            usuario.IntentosFallidos++;
            return Task.FromResult(usuario.IntentosFallidos);
        }

        public Task ReiniciarIntentosAsync(int usuarioId)
        {
            Llamadas.Add(nameof(ReiniciarIntentosAsync));

            var usuario = BuscarPorId(usuarioId);
            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
            return Task.CompletedTask;
        }

        public Task BloquearAsync(int usuarioId, DateTime bloqueadoHasta)
        {
            Llamadas.Add(nameof(BloquearAsync));

            var usuario = BuscarPorId(usuarioId);
            usuario.BloqueadoHasta = bloqueadoHasta;
            usuario.IntentosFallidos = 0;
            return Task.CompletedTask;
        }

        private Usuario BuscarPorId(int usuarioId)
        {
            return _usuarios.Values.Single(u => u.Id == usuarioId);
        }

        private static Usuario Copiar(Usuario usuario)
        {
            return new Usuario
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Username = usuario.Username,
                PasswordHash = usuario.PasswordHash,
                Rol = usuario.Rol,
                IntentosFallidos = usuario.IntentosFallidos,
                BloqueadoHasta = usuario.BloqueadoHasta
            };
        }
    }
}
