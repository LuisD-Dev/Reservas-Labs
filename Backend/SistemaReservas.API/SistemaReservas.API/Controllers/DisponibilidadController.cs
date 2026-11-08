using Microsoft.AspNetCore.Mvc;
using SistemaReservas.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using SistemaReservas.Application.Services;

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


        [HttpPost("verificar")]
        public async Task<IActionResult> Verificar(
            int laboratorioId, ConsultaDisponibilidadRequest request,
            [FromServices] VerificacionDisponibilidadService servicio)
        {
            var resultado = await servicio.VerificarAsync(laboratorioId, request);
            if (resultado.Errores.Count > 0) return BadRequest(new { errores = resultado.Errores });
            return Ok(resultado);
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
