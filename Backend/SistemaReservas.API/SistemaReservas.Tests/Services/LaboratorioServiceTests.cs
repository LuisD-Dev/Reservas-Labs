using SistemaReservas.API.Models;
using SistemaReservas.API.Services;
using SistemaReservas.Tests.Fakes;

namespace SistemaReservas.Tests.Services
{
    // NFR2 - Prueba unitaria de LaboratorioService (HU2) con un repositorio falso.
    public class LaboratorioServiceTests
    {
        [Fact]
        public async Task ObtenerLaboratoriosAsync_DevuelveLoQueEntregaElRepositorio()
        {
            var laboratorios = new List<Laboratorio>
            {
                new() { Id = 2, Nombre = "Laboratorio de Redes", Ubicacion = "Edificio B", Capacidad = 25, Estado = "Habilitado" },
                new() { Id = 1, Nombre = "Laboratorio de Software", Ubicacion = "Edificio A", Capacidad = 30, Estado = "Habilitado" }
            };
            var servicio = new LaboratorioService(new FakeLaboratorioRepository(laboratorios));

            var resultado = await servicio.ObtenerLaboratoriosAsync();

            Assert.Same(laboratorios, resultado);
        }
    }
}
