using MapsterMapper;
using SistemaReservas.Domain.DTO;
using SistemaReservas.Application.Interfaces;
using SistemaReservas.Infrastructure.Interfaces;

namespace SistemaReservas.Application.Implementation
{
    public class DisponibilidadService : IDisponibilidadService
    {
        private readonly IMapper _mapper;
        private readonly IDisponibilidadRepository _disponibilidadRepository;

        // NFR2 - Separar acceso a datos: el acceso a Disponibilidades está en IDisponibilidadRepository.
        public DisponibilidadService(IDisponibilidadRepository disponibilidadRepository, IMapper mapper)
        {
            _mapper = mapper;
            _disponibilidadRepository = disponibilidadRepository;
        }


        public async Task<IReadOnlyList<DisponibilidadResponse>> ObtenerDisponibilidadAsync(
            int laboratorioId)
        {
            return _mapper.Map<List<DisponibilidadResponse>>(await _disponibilidadRepository
                .ObtenerPorLaboratorioAsync(laboratorioId));
        }
    }
}