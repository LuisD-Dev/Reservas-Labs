using SistemaReservas.Domain.Entities;
using SistemaReservas.Infrastructure.Interfaces;

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

        public Task<Laboratorio?> ObtenerPorIdAsync(int id)
        {
            return Task.FromResult(_laboratorios.SingleOrDefault(l => l.Id == id));
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }
}
