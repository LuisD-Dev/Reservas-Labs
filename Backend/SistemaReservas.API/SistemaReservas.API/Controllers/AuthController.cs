using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaReservas.API.DTOs;
using SistemaReservas.API.Reglas;
using SistemaReservas.API.Services;

namespace SistemaReservas.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly TokenService _tokenService;
        private readonly TimeProvider _timeProvider;

        // NFR2 - Tiempo inyectable: los segundos restantes del bloqueo usan la hora de TimeProvider.
        public AuthController(AuthService authService, TokenService tokenService, TimeProvider timeProvider)
        {
            _authService = authService;
            _tokenService = tokenService;
            _timeProvider = timeProvider;
        }

        // HU1 - #22 Crear endpoint Login
        // NFR1 - #44 El login debe quedar abierto: es donde se obtiene el token.
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var resultado = await _authService.LoginAsync(request);

            switch (resultado.Estado)
            {
                case LoginEstado.Exitoso:
                    // NFR1 - #43 El login entrega un token firmado que el
                    // frontend debe enviar en las peticiones protegidas.
                    var (token, expiraUtc) = _tokenService.GenerarToken(
                        resultado.UsuarioId!.Value,
                        resultado.Nombre!,
                        resultado.Rol!);

                    return Ok(new
                    {
                        usuarioId = resultado.UsuarioId,
                        nombre = resultado.Nombre,
                        rol = resultado.Rol,
                        token,
                        expiraUtc
                    });

                case LoginEstado.Bloqueado:
                    var hastaUtc = DateTime.SpecifyKind(resultado.BloqueadoHasta!.Value, DateTimeKind.Utc);
                    // NFR2 - El cálculo de los segundos restantes está en ReglasBloqueo.
                    var segundosRestantes = ReglasBloqueo.SegundosRestantes(hastaUtc, _timeProvider.GetUtcNow().UtcDateTime);
                    return StatusCode(StatusCodes.Status423Locked, new
                    {
                        mensaje = "Demasiados intentos fallidos. Intente de nuevo en unos minutos.",
                        bloqueadoHasta = hastaUtc,
                        segundosRestantes
                    });

                default:
                    return Unauthorized(new
                    {
                        mensaje = "Usuario o contraseña incorrectos."
                    });
            }
        }
    }
}
