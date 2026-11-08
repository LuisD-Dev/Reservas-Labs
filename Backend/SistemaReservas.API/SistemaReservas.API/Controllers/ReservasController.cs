using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaReservas.Application.DTOs;
using SistemaReservas.Application.Abstractions.Services;
using SistemaReservas.Domain.Rules;

namespace SistemaReservas.API.Controllers
{
    [ApiController]
    [Route("api/reservas")]
    [Authorize]
    public class ReservasController : ControllerBase
    {
        private readonly IReservaService _reservaService;

        public ReservasController(
            IReservaService reservaService)
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

        // HU8 - AB#101: cancela una reserva. El propietario cancela la suya
        // y el Administrador puede cancelar cualquiera.
        [HttpPost("{id:int}/cancelar")]
        public async Task<IActionResult> CancelarReserva(int id)
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
                await _reservaService.CancelarAsync(
                    id,
                    usuarioId,
                    User.IsInRole(ReglasRoles.Administrador));

            return resultado.Estado switch
            {
                EstadoResultadoCancelacion.Exitosa =>
                    Ok(new { mensaje = resultado.Mensaje }),

                EstadoResultadoCancelacion.NoEncontrada =>
                    NotFound(new { mensaje = resultado.Mensaje }),

                EstadoResultadoCancelacion.NoAutorizada =>
                    StatusCode(
                        StatusCodes.Status403Forbidden,
                        new { mensaje = resultado.Mensaje }),

                EstadoResultadoCancelacion.UsuarioInvalido =>
                    Unauthorized(new { mensaje = resultado.Mensaje }),

                _ =>
                    Conflict(new { mensaje = resultado.Mensaje })
            };
        }

        // HU7 - AB#92: consulta las reservas del usuario autenticado.
        [HttpGet("mias")]
        public async Task<IActionResult> ObtenerMisReservas()
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

            var reservas =
                await _reservaService.ObtenerMisReservasAsync(
                    usuarioId);

            return Ok(reservas);
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