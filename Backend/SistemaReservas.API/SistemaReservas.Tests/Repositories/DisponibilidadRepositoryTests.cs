using SistemaReservas.API.Models;
using SistemaReservas.API.Repositories;
using SistemaReservas.Tests.Infraestructura;

namespace SistemaReservas.Tests.Repositories
{
    // Taller V - Pruebas de DisponibilidadRepository (HU3) con EF Core sobre SQLite en memoria.
    // Patrón AAA: el Arrange común (dos laboratorios sin horarios) está en el constructor.
    // SQLite no conserva DateTime.Kind: las fechas se comparan por valor.
    public sealed class DisponibilidadRepositoryTests : IDisposable
    {
        private readonly BaseDatosEnMemoria _baseDatos = new();
        private readonly int _laboratorio1;
        private readonly int _laboratorio2;

        public DisponibilidadRepositoryTests()
        {
            using var contexto = _baseDatos.CrearContexto();
            var software = new Laboratorio { Nombre = "Laboratorio de Software", Ubicacion = "Edificio A", Capacidad = 30, Estado = "Habilitado" };
            var redes = new Laboratorio { Nombre = "Laboratorio de Redes", Ubicacion = "Edificio B", Capacidad = 25, Estado = "Habilitado" };
            contexto.Laboratorios.AddRange(software, redes);
            contexto.SaveChanges();
            _laboratorio1 = software.Id;
            _laboratorio2 = redes.Id;
        }

        public void Dispose()
        {
            _baseDatos.Dispose();
        }

        private static Disponibilidad CrearHorario(int laboratorioId, int dia, int horaInicio, int horaFin, string estado)
        {
            return new Disponibilidad
            {
                LaboratorioId = laboratorioId,
                Fecha = new DateTime(2026, 10, dia),
                HoraInicio = TimeSpan.FromHours(horaInicio),
                HoraFin = TimeSpan.FromHours(horaFin),
                Estado = estado
            };
        }

        [Fact]
        public async Task ObtenerPorLaboratorioAsync_VariosHorarios_FiltraPorLaboratorioYOrdenaPorFechaYHora()
        {
            // Arrange
            using (var contexto = _baseDatos.CrearContexto())
            {
                contexto.Disponibilidades.AddRange(
                    CrearHorario(_laboratorio1, 14, 13, 15, "No disponible"),
                    CrearHorario(_laboratorio1, 13, 13, 15, "No disponible"),
                    CrearHorario(_laboratorio2, 13, 8, 12, "Disponible"),
                    CrearHorario(_laboratorio1, 13, 8, 12, "Disponible"));
                await contexto.SaveChangesAsync();
            }
            using var contextoConsulta = _baseDatos.CrearContexto();
            var repositorio = new DisponibilidadRepository(contextoConsulta);

            // Act
            var horarios = await repositorio.ObtenerPorLaboratorioAsync(_laboratorio1);

            // Assert
            Assert.All(horarios, h => Assert.Equal(_laboratorio1, h.LaboratorioId));
            Assert.Equal(
                [
                    (new DateTime(2026, 10, 13), TimeSpan.FromHours(8), "Disponible"),
                    (new DateTime(2026, 10, 13), TimeSpan.FromHours(13), "No disponible"),
                    (new DateTime(2026, 10, 14), TimeSpan.FromHours(13), "No disponible")
                ],
                horarios.Select(h => (h.Fecha, h.HoraInicio, h.Estado)));
        }

        [Fact]
        public async Task ObtenerPorLaboratorioAsync_LaboratorioSinHorarios_DevuelveListaVacia()
        {
            // Arrange
            using var contexto = _baseDatos.CrearContexto();
            var repositorio = new DisponibilidadRepository(contexto);

            // Act
            var horarios = await repositorio.ObtenerPorLaboratorioAsync(_laboratorio2);

            // Assert
            Assert.Empty(horarios);
        }
    }
}
