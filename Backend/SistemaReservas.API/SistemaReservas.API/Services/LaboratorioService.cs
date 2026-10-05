using SistemaReservas.API.Models;
using SistemaReservas.API.Repositories;

namespace SistemaReservas.API.Services
{
    public class LaboratorioService
    {
        private readonly ILaboratorioRepository _laboratorioRepository;

        // NFR2 - Separar acceso a datos: el SQL de Laboratorios está en ILaboratorioRepository.
        public LaboratorioService(ILaboratorioRepository laboratorioRepository)
        {
            _laboratorioRepository = laboratorioRepository;
        }

        public async Task<List<Laboratorio>> ObtenerLaboratoriosAsync()
        {
            return await _laboratorioRepository.ObtenerTodosAsync();
        }
    }
}