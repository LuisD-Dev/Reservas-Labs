using Microsoft.EntityFrameworkCore;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Infrastructure.Implementation;
using SistemaReservas.Tests.Infraestructura;

namespace SistemaReservas.Tests.Repositories
{
    public class LaboratorioEstadoRepositoryTests
    {
        [Fact]
        public async Task ObtenerPorId_EncuentraLaboratorioYDevuelveNullSiNoExiste()
        {
            using var database = new BaseDatosEnMemoria();
            int id;
            using (var context = database.CrearContexto())
            {
                var laboratorio = new Laboratorio { Nombre = "Computo", Ubicacion = "A", Capacidad = 30, Estado = "Habilitado" };
                context.Laboratorios.Add(laboratorio);
                await context.SaveChangesAsync();
                id = laboratorio.Id;
            }
            using var consulta = database.CrearContexto();
            var repository = new LaboratorioRepository(consulta);
            var encontrado = await repository.ObtenerPorIdAsync(id);
            Assert.NotNull(encontrado);
            Assert.Equal(id, encontrado.Id);
            Assert.Equal("Computo", encontrado.Nombre);
            Assert.Null(await repository.ObtenerPorIdAsync(id + 1));
        }

        [Fact]
        public async Task GuardarCambios_PersisteSoloEstadoYRepetirNoCreaRegistros()
        {
            using var database = new BaseDatosEnMemoria();
            int id;
            using (var context = database.CrearContexto())
            {
                var laboratorio = new Laboratorio { Nombre = "Computo", Ubicacion = "A", Capacidad = 30, Estado = "Habilitado" };
                context.Laboratorios.Add(laboratorio);
                await context.SaveChangesAsync();
                id = laboratorio.Id;
            }
            for (var intento = 0; intento < 2; intento++)
            {
                using var context = database.CrearContexto();
                var repository = new LaboratorioRepository(context);
                var laboratorio = await repository.ObtenerPorIdAsync(id);
                laboratorio!.Estado = "Fuera de servicio";
                await repository.GuardarCambiosAsync();
            }
            using var verificacion = database.CrearContexto();
            var guardado = Assert.Single(await verificacion.Laboratorios.ToListAsync());
            Assert.Equal(id, guardado.Id);
            Assert.Equal("Fuera de servicio", guardado.Estado);
            Assert.Equal("Computo", guardado.Nombre);
            Assert.Equal("A", guardado.Ubicacion);
            Assert.Equal(30, guardado.Capacidad);
        }
    }
}
