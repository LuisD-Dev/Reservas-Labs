using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaReservas.API.Services;

namespace SistemaReservas.API.Controllers
{
    [ApiController]
    [Route("api/laboratorios/{laboratorioId}/disponibilidad")]
    [Authorize]
    public class DisponibilidadController : ControllerBase
    {
        private readonly DisponibilidadService _disponibilidadService;


        public DisponibilidadController(
            DisponibilidadService disponibilidadService)
        {
            _disponibilidadService = disponibilidadService;
        }


        [HttpGet]
        public async Task<IActionResult> ObtenerDisponibilidad(
            int laboratorioId)
        {
            var disponibilidades =
                await _disponibilidadService
                .ObtenerDisponibilidadAsync(laboratorioId);


            return Ok(disponibilidades);
        }
    }
}
