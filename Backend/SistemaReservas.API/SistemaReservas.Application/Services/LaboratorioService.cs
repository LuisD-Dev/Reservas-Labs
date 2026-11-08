using SistemaReservas.Domain.Entities;
using SistemaReservas.Application.Abstractions.Persistence;

namespace SistemaReservas.Application.Services
{
    public class LaboratorioService
    {
        private readonly ILaboratorioRepository _laboratorioRepository;

        // NFR2 - Separar acceso a datos: el acceso a Laboratorios está en ILaboratorioRepository.
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