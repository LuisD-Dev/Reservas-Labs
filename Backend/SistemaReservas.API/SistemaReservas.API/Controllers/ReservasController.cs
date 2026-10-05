using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SistemaReservas.API.Controllers
{
    [ApiController]
    [Route("api/reservas")]
    public class ReservasController : ControllerBase
    {
        [HttpPost]
        // NFR1 - #44 Solo el rol Administrador puede crear reservas de prueba.
        [Authorize(Roles = "Administrador")]
        public IActionResult CrearReserva()
        {
            return Ok(new { mensaje = "Reserva creada" });
        }
    }
}
