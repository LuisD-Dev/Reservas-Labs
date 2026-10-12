using SistemaReservas.API.Models;
using SistemaReservas.API.Repositories;

namespace SistemaReservas.Tests.Fakes
{
    // NFR2 - Doble de prueba de IDisponibilidadRepository: devuelve una lista fija
    // y recuerda el laboratorio que se consultó.
    public class FakeDisponibilidadRepository : IDisponibilidadRepository
    {
        private readonly List<Disponibilidad> _disponibilidades;

        public int? LaboratorioConsultado { get; private set; }

        public FakeDisponibilidadRepository(List<Disponibilidad> disponibilidades)
        {
            _disponibilidades = disponibilidades;
        }

        public Task<List<Disponibilidad>> ObtenerPorLaboratorioAsync(int laboratorioId)
        {
            LaboratorioConsultado = laboratorioId;
            return Task.FromResult(_disponibilidades);
        }
    }
}
