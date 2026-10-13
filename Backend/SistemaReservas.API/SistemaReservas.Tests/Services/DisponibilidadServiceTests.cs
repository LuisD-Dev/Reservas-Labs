using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SistemaReservas.API.Models;
using SistemaReservas.API.Repositories;
using SistemaReservas.API.Services;
using SistemaReservas.Tests.Fakes;

namespace SistemaReservas.Tests.Services
{
    // NFR2 - Prueba unitaria de DisponibilidadService (HU3) con un repositorio falso.
    public class DisponibilidadServiceTests
    {
        [Fact]
        public async Task ObtenerDisponibilidadAsync_DevuelveLoQueEntregaElRepositorioParaEseLaboratorio()
        {
            // Arrange
            var disponibilidades = new List<Disponibilidad>
            {
                new()
                {
                    Id = 1,
                    LaboratorioId = 1,
                    Fecha = new DateTime(2026, 10, 1),
                    HoraInicio = new TimeSpan(8, 0, 0),
                    HoraFin = new TimeSpan(10, 0, 0),
                    Estado = "Disponible"
                }
            };
            var repositorio = new FakeDisponibilidadRepository(disponibilidades);
            var servicio = new DisponibilidadService(repositorio);

            // Act
            var resultado = await servicio.ObtenerDisponibilidadAsync(1);

            // Assert
            Assert.Same(disponibilidades, resultado);
            Assert.Equal(1, repositorio.LaboratorioConsultado);
        }

        // ---- Excepciones (NSubstitute) ----

        [Fact]
        public async Task ObtenerDisponibilidadAsync_RepositorioNoDisponible_LanzaExcepcion()
        {
            // Arrange
            var excepcion = new InvalidOperationException("No se pudo conectar a la base de datos.");
            var repositorio = Substitute.For<IDisponibilidadRepository>();
            repositorio.ObtenerPorLaboratorioAsync(1).ThrowsAsync(excepcion);
            var servicio = new DisponibilidadService(repositorio);

            // Act & Assert
            var lanzada = await Assert.ThrowsAsync<InvalidOperationException>(
                () => servicio.ObtenerDisponibilidadAsync(1));

            // Assert
            Assert.Same(excepcion, lanzada);
            await repositorio.Received(1).ObtenerPorLaboratorioAsync(1);
        }
    }
}
