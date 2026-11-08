using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaReservas.Application.Interfaces;
using SistemaReservas.Domain.DTO;
using SistemaReservas.Domain.Rules;

namespace SistemaReservas.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LaboratoriosController : ControllerBase
    {
        private readonly ILaboratorioService _laboratorioService;

        public LaboratoriosController(ILaboratorioService laboratorioService)
        {
            _laboratorioService = laboratorioService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerLaboratorios([FromQuery] bool soloHabilitados = false)
        {
            var laboratorios = await _laboratorioService.ObtenerLaboratoriosAsync(soloHabilitados);

            return Ok(laboratorios);
        }

        [HttpPatch("{id}/estado")]
        [Authorize(Roles = ReglasRoles.Administrador)]
        public async Task<IActionResult> ActualizarEstado(int id, [FromBody] ActualizarEstadoLaboratorioRequest? request)
        {
            var resultado = await _laboratorioService.ActualizarEstadoAsync(id, request?.Estado);
            return resultado.Estado switch
            {
                EstadoActualizacionLaboratorio.DatosInvalidos => BadRequest(new { mensaje = resultado.Mensaje }),
                EstadoActualizacionLaboratorio.NoEncontrado => NotFound(new { mensaje = resultado.Mensaje }),
                _ => Ok(new { mensaje = resultado.Mensaje, laboratorio = resultado.Laboratorio })
            };
        }
    }
}
