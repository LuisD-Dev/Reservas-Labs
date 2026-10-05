using SistemaReservas.API.Models;
using SistemaReservas.API.Repositories;

namespace SistemaReservas.Tests.Fakes
{
    // NFR2 - Doble de prueba de ILaboratorioRepository: devuelve una lista fija.
    public class FakeLaboratorioRepository : ILaboratorioRepository
    {
        private readonly List<Laboratorio> _laboratorios;

        public FakeLaboratorioRepository(List<Laboratorio> laboratorios)
        {
            _laboratorios = laboratorios;
        }

        public Task<List<Laboratorio>> ObtenerTodosAsync()
        {
            return Task.FromResult(_laboratorios);
        }
    }
}
