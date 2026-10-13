using SistemaReservas.API.Models;
using SistemaReservas.API.Repositories;
using SistemaReservas.Tests.Infraestructura;

namespace SistemaReservas.Tests.Repositories
{
    // Taller V - Pruebas de LaboratorioRepository (HU2) con EF Core sobre SQLite en memoria.
    // Patrón AAA: la base de datos vacía se crea en el campo _baseDatos.
    public sealed class LaboratorioRepositoryTests : IDisposable
    {
        private readonly BaseDatosEnMemoria _baseDatos = new();

        public void Dispose()
        {
            _baseDatos.Dispose();
        }

        [Fact]
        public async Task ObtenerTodosAsync_VariosLaboratorios_DevuelveOrdenadosPorNombre()
        {
            // Arrange
            using (var contexto = _baseDatos.CrearContexto())
            {
                contexto.Laboratorios.AddRange(
                    new Laboratorio { Nombre = "Laboratorio de Software", Ubicacion = "Edificio A", Capacidad = 30, Estado = "Habilitado" },
                    new Laboratorio { Nombre = "Laboratorio de Redes", Ubicacion = "Edificio B", Capacidad = 25, Estado = "Fuera de servicio" },
                    new Laboratorio { Nombre = "Laboratorio de Bases de Datos", Ubicacion = "Edificio C", Capacidad = 20, Estado = "Habilitado" });
                await contexto.SaveChangesAsync();
            }
            using var contextoConsulta = _baseDatos.CrearContexto();
            var repositorio = new LaboratorioRepository(contextoConsulta);

            // Act
            var laboratorios = await repositorio.ObtenerTodosAsync();

            // Assert
            Assert.Equal(
                ["Laboratorio de Bases de Datos", "Laboratorio de Redes", "Laboratorio de Software"],
                laboratorios.Select(l => l.Nombre));
            var redes = laboratorios[1];
            Assert.Equal("Edificio B", redes.Ubicacion);
            Assert.Equal(25, redes.Capacidad);
            Assert.Equal("Fuera de servicio", redes.Estado);
        }
    }
}
