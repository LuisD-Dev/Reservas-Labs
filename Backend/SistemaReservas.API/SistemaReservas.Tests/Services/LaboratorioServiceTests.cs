using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SistemaReservas.API.Models;
using SistemaReservas.API.Repositories;
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
            // Arrange
            var laboratorios = new List<Laboratorio>
            {
                new() { Id = 2, Nombre = "Laboratorio de Redes", Ubicacion = "Edificio B", Capacidad = 25, Estado = "Habilitado" },
                new() { Id = 1, Nombre = "Laboratorio de Software", Ubicacion = "Edificio A", Capacidad = 30, Estado = "Habilitado" }
            };
            var servicio = new LaboratorioService(new FakeLaboratorioRepository(laboratorios));

            // Act
            var resultado = await servicio.ObtenerLaboratoriosAsync();

            // Assert
            Assert.Same(laboratorios, resultado);
        }

        // ---- Excepciones (NSubstitute) ----

        [Fact]
        public async Task ObtenerLaboratoriosAsync_RepositorioNoDisponible_LanzaExcepcion()
        {
            // Arrange
            var excepcion = new InvalidOperationException("No se pudo conectar a la base de datos.");
            var repositorio = Substitute.For<ILaboratorioRepository>();
            repositorio.ObtenerTodosAsync().ThrowsAsync(excepcion);
            var servicio = new LaboratorioService(repositorio);

            // Act & Assert
            var lanzada = await Assert.ThrowsAsync<InvalidOperationException>(
                () => servicio.ObtenerLaboratoriosAsync());

            // Assert
            Assert.Same(excepcion, lanzada);
        }
    }
}
