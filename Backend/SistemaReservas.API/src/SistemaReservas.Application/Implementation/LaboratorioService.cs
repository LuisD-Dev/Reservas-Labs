using MapsterMapper;
using SistemaReservas.Domain.DTO;
using SistemaReservas.Application.Interfaces;
using SistemaReservas.Infrastructure.Interfaces;

namespace SistemaReservas.Application.Implementation
{
    public class LaboratorioService : ILaboratorioService
    {
        private readonly IMapper _mapper;
        private readonly ILaboratorioRepository _laboratorioRepository;

        // NFR2 - Separar acceso a datos: el acceso a Laboratorios está en ILaboratorioRepository.
        public LaboratorioService(ILaboratorioRepository laboratorioRepository, IMapper mapper)
        {
            _mapper = mapper;
            _laboratorioRepository = laboratorioRepository;
        }

        public async Task<IReadOnlyList<LaboratorioResponse>> ObtenerLaboratoriosAsync()
        {
            return _mapper.Map<List<LaboratorioResponse>>(await _laboratorioRepository.ObtenerTodosAsync());
        }
    }
}