using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaReservas.API.DTOs;
using SistemaReservas.API.Services;
using SistemaReservas.API.Reglas;

namespace SistemaReservas.API.Controllers
{
    [ApiController]
    [Route("api/reservas")]
    [Authorize]
    public class ReservasController : ControllerBase
    {
        private readonly ReservaService _reservaService;

        public ReservasController(
            ReservaService reservaService)
        {
            _reservaService = reservaService;
        }

        [HttpPost]
        public async Task<IActionResult> CrearReserva(
            [FromBody] CrearReservaRequest request)
        {
            var claimUsuarioId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (!int.TryParse(
                    claimUsuarioId,
                    out var usuarioId) ||
                usuarioId <= 0)
            {
                return Unauthorized(new
                {
                    mensaje =
                        "No fue posible identificar al usuario autenticado."
                });
            }

            var resultado =
                await _reservaService.CrearAsync(
                    usuarioId,
                    request);

            if (!resultado.Exitoso)
            {
                return BadRequest(new
                {
                    mensaje =
                        "Revisa los datos de la reserva.",

                    errores = resultado.Errores
                });
            }

            var reserva = resultado.Reserva!;

            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    mensaje =
                        "Reserva registrada correctamente.",

                    reserva = new
                    {
                        reserva.Id,
                        reserva.LaboratorioId,

                        fecha =
                            DateOnly.FromDateTime(
                                reserva.Fecha),

                        reserva.HoraInicio,
                        reserva.HoraFin,

                        responsable =
                            User.Identity?.Name,

                        reserva.Estado
                    }
                });
        }
        // NFR1 - #44 Conserva la comprobación de acceso
        // exclusivo para el rol Administrador.
        [HttpPost("prueba-administrador")]
        [Authorize(Roles = ReglasRoles.Administrador)]
        public IActionResult ProbarAccesoAdministrador()
        {
            return Ok(new
            {
                mensaje =
                    "Acceso de administrador autorizado."
            });
        }
    }

}
