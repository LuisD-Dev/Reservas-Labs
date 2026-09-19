using Microsoft.AspNetCore.Mvc;
using SistemaReservas.API.DTOs;
using SistemaReservas.API.Services;

namespace SistemaReservas.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        // HU1 - #22 Crear endpoint Login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var resultado = await _authService.LoginAsync(request);

            switch (resultado.Estado)
            {
                case LoginEstado.Exitoso:
                    return Ok(new
                    {
                        usuarioId = resultado.UsuarioId,
                        nombre = resultado.Nombre,
                        rol = resultado.Rol
                    });

                case LoginEstado.Bloqueado:
                    var hastaUtc = DateTime.SpecifyKind(resultado.BloqueadoHasta!.Value, DateTimeKind.Utc);
                    var segundosRestantes = (int)Math.Ceiling((hastaUtc - DateTime.UtcNow).TotalSeconds);
                    return StatusCode(StatusCodes.Status423Locked, new
                    {
                        mensaje = "Demasiados intentos fallidos. Intente de nuevo en unos minutos.",
                        bloqueadoHasta = hastaUtc,
                        segundosRestantes = Math.Max(segundosRestantes, 0)
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
