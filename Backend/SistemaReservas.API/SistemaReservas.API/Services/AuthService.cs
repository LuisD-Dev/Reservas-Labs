using Microsoft.AspNetCore.Identity;
using SistemaReservas.API.DTOs;
using SistemaReservas.API.Models;
using SistemaReservas.API.Reglas;
using SistemaReservas.API.Repositories;

namespace SistemaReservas.API.Services
{
    public class AuthService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly TimeProvider _timeProvider;
        private readonly PasswordHasher<Usuario> _passwordHasher = new();

        // NFR2 - Separar acceso a datos: el SQL de Usuarios está en IUsuarioRepository.
        // NFR2 - Tiempo inyectable: la hora actual viene de TimeProvider.
        public AuthService(IUsuarioRepository usuarioRepository, TimeProvider timeProvider)
        {
            _usuarioRepository = usuarioRepository;
            _timeProvider = timeProvider;
        }

        // HU1 - #19 Implementar autenticación: validar usuario/password
        // NFR2 - Las reglas del bloqueo (HU1 - #21) están en ReglasBloqueo.
        public async Task<LoginResult> LoginAsync(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return new LoginResult(LoginEstado.CredencialesInvalidas);
            }

            var usuario = await _usuarioRepository.ObtenerPorUsernameAsync(request.Username);

            // Mismo resultado si el usuario no existe o si la contraseña es incorrecta,
            // para no revelar cuál de los dos datos falló.
            if (usuario is null)
            {
                return new LoginResult(LoginEstado.CredencialesInvalidas);
            }

            // HU1 - #21 Si el usuario sigue bloqueado, se rechaza sin revisar la contraseña.
            if (ReglasBloqueo.EstaBloqueado(usuario.BloqueadoHasta, _timeProvider.GetUtcNow().UtcDateTime))
            {
                return new LoginResult(LoginEstado.Bloqueado, BloqueadoHasta: usuario.BloqueadoHasta);
            }

            if (!VerificarPassword(usuario, request.Password))
            {
                // HU1 - #20 Implementar intentos fallidos: llevar contador
                var intentos = await _usuarioRepository.RegistrarIntentoFallidoAsync(usuario.Id);

                // HU1 - #21 Al llegar al 5.º intento fallido se bloquea el acceso.
                if (ReglasBloqueo.DebeBloquear(intentos))
                {
                    var bloqueadoHasta = ReglasBloqueo.CalcularBloqueadoHasta(_timeProvider.GetUtcNow().UtcDateTime);
                    await _usuarioRepository.BloquearAsync(usuario.Id, bloqueadoHasta);
                    return new LoginResult(LoginEstado.Bloqueado, BloqueadoHasta: bloqueadoHasta);
                }

                return new LoginResult(LoginEstado.CredencialesInvalidas);
            }

            // Login exitoso: el contador vuelve a cero y se limpia cualquier bloqueo vencido.
            if (ReglasBloqueo.DebeReiniciarIntentos(usuario.IntentosFallidos, usuario.BloqueadoHasta))
            {
                await _usuarioRepository.ReiniciarIntentosAsync(usuario.Id);
            }

            return new LoginResult(LoginEstado.Exitoso, usuario.Id, usuario.Nombre, usuario.Rol);
        }

        // Genera el hash que se debe guardar en la columna PasswordHash.
        public string HashPassword(string password)
        {
            return _passwordHasher.HashPassword(new Usuario(), password);
        }

        private bool VerificarPassword(Usuario usuario, string password)
        {
            var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password);
            return resultado != PasswordVerificationResult.Failed;
        }
    }
}
