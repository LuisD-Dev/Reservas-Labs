using Microsoft.AspNetCore.Mvc;
using SistemaReservas.API.Services;

namespace SistemaReservas.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LaboratoriosController : ControllerBase
    {
        private readonly LaboratorioService _laboratorioService;

        public LaboratoriosController(LaboratorioService laboratorioService)
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