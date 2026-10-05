using SistemaReservas.API.Models;
using SistemaReservas.API.Repositories;

namespace SistemaReservas.API.Services
{
    public class DisponibilidadService
    {
        private readonly IDisponibilidadRepository _disponibilidadRepository;

        // NFR2 - Separar acceso a datos: el SQL de Disponibilidades está en IDisponibilidadRepository.
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