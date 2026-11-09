using Microsoft.AspNetCore.Mvc;
using SistemaReservas.Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using SistemaReservas.Application.Interfaces;

namespace SistemaReservas.API.Controllers
{
    [ApiController]
    [Route("api/laboratorios/{laboratorioId}/disponibilidad")]
    [Authorize]
    public class DisponibilidadController : ControllerBase
    {
        private readonly IDisponibilidadService _disponibilidadService;
        private readonly IVerificacionDisponibilidadService _verificacionDisponibilidadService;


        public DisponibilidadController(
            IDisponibilidadService disponibilidadService,
            IVerificacionDisponibilidadService verificacionDisponibilidadService)
        {
            _disponibilidadService = disponibilidadService;
            _verificacionDisponibilidadService = verificacionDisponibilidadService;
        }


        [HttpPost("verificar")]
        public async Task<IActionResult> Verificar(
            int laboratorioId, ConsultaDisponibilidadRequest request)
        {
            var resultado = await _verificacionDisponibilidadService.VerificarAsync(laboratorioId, request);
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
