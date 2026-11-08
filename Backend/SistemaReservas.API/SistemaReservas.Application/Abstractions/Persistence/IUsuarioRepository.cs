using SistemaReservas.API.Models;

namespace SistemaReservas.API.Repositories
{
    // NFR2 - Separar acceso a datos: operaciones sobre la tabla Usuarios
    // que usa el login (HU1). AuthService depende de esta interfaz.
    public interface IUsuarioRepository
    {
        // Devuelve null si no existe un usuario con ese username.
        Task<Usuario?> ObtenerPorUsernameAsync(string username);

        // Suma 1 al contador en la base de datos y devuelve el nuevo valor.
        Task<int> RegistrarIntentoFallidoAsync(int usuarioId);

        // Deja el contador en cero y limpia el bloqueo.
        Task ReiniciarIntentosAsync(int usuarioId);

        // Guarda hasta cuándo queda bloqueado y reinicia el contador
        // para que, al vencer el bloqueo, tenga otros 5 intentos.
        Task BloquearAsync(int usuarioId, DateTime bloqueadoHasta);
    }
}
