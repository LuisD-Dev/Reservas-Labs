using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaReservas.Application.Interfaces;

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
        public async Task<IActionResult> ObtenerLaboratorios()
        {
            var laboratorios = await _laboratorioService.ObtenerLaboratoriosAsync();

            return Ok(laboratorios);
        }
    }
}
