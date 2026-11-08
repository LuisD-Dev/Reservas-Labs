using SistemaReservas.Domain.Entities;
using SistemaReservas.Application.Abstractions.Persistence;

namespace SistemaReservas.Application.Services
{
    public class DisponibilidadService
    {
        private readonly IDisponibilidadRepository _disponibilidadRepository;

        // NFR2 - Separar acceso a datos: el acceso a Disponibilidades está en IDisponibilidadRepository.
        public DisponibilidadService(IDisponibilidadRepository disponibilidadRepository)
        {
            _disponibilidadRepository = disponibilidadRepository;
        }


        public async Task<List<Disponibilidad>> ObtenerDisponibilidadAsync(
            int laboratorioId)
        {
            return await _disponibilidadRepository
                .ObtenerPorLaboratorioAsync(laboratorioId);
        }
    }
}