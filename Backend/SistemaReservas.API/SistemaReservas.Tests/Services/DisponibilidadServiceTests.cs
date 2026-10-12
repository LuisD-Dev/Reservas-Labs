using SistemaReservas.API.Models;
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

            var resultado = await servicio.ObtenerDisponibilidadAsync(1);

            Assert.Same(disponibilidades, resultado);
            Assert.Equal(1, repositorio.LaboratorioConsultado);
        }
    }
}
