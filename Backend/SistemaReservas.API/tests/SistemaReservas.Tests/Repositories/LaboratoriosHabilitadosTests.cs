using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaReservas.API.Controllers;
using SistemaReservas.Application.Implementation;
using SistemaReservas.Application.Mapper;
using SistemaReservas.Domain.DTO;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Domain.Rules;
using SistemaReservas.Infrastructure.Implementation;
using SistemaReservas.Tests.Infraestructura;

namespace SistemaReservas.Tests.Repositories
{
    public class LaboratoriosHabilitadosTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ConsultaEfYEndpoint_ConservanOrdenYFiltranSoloCuandoSeSolicita(bool soloHabilitados)
        {
            using var db = new BaseDatosEnMemoria();
            using (var context = db.CrearContexto())
            {
                context.Laboratorios.AddRange(
                    new Laboratorio { Nombre = "Zeta", Ubicacion = "A", Capacidad = 10, Estado = ReglasEstadoLaboratorio.Habilitado },
                    new Laboratorio { Nombre = "Beta", Ubicacion = "B", Capacidad = 20, Estado = ReglasEstadoLaboratorio.FueraDeServicio },
                    new Laboratorio { Nombre = "Alfa", Ubicacion = "C", Capacidad = 30, Estado = ReglasEstadoLaboratorio.Habilitado });
                await context.SaveChangesAsync();
            }
            using var consulta = db.CrearContexto();
            var repositorio = new LaboratorioRepository(consulta);
            var esperados = soloHabilitados ? new[] { "Alfa", "Zeta" } : new[] { "Alfa", "Beta", "Zeta" };
            var entidades = await repositorio.ObtenerTodosAsync(soloHabilitados);
            Assert.Equal(esperados, entidades.Select(l => l.Nombre));
            Assert.Empty(consulta.ChangeTracker.Entries());
            var controller = new LaboratoriosController(new LaboratorioService(repositorio, new Mapper(MapsterConfig.RegisterMaps())));
            var respuesta = Assert.IsType<OkObjectResult>(await controller.ObtenerLaboratorios(soloHabilitados));
            Assert.Equal(esperados, Assert.IsAssignableFrom<IReadOnlyList<LaboratorioResponse>>(respuesta.Value).Select(l => l.Nombre));
            Assert.Equal(3, (await repositorio.ObtenerTodosAsync()).Count);
        }

        [Fact]
        public async Task DisponibilidadGet_FiltraEstadoActualDelLaboratorioSinEliminarHorarios()
        {
            using var db = new BaseDatosEnMemoria();
            int id;
            using (var context = db.CrearContexto())
            {
                var laboratorio = new Laboratorio { Nombre = "Redes", Ubicacion = "A", Capacidad = 10, Estado = ReglasEstadoLaboratorio.FueraDeServicio };
                context.Laboratorios.Add(laboratorio);
                await context.SaveChangesAsync();
                id = laboratorio.Id;
                context.Disponibilidades.Add(new Disponibilidad { LaboratorioId = id, Fecha = new DateTime(2030, 1, 2),
                    HoraInicio = TimeSpan.FromHours(8), HoraFin = TimeSpan.FromHours(12), Estado = "Disponible" });
                await context.SaveChangesAsync();
            }
            using (var consulta = db.CrearContexto())
            {
                var servicio = new DisponibilidadService(new DisponibilidadRepository(consulta), new Mapper(MapsterConfig.RegisterMaps()));
                var controller = new DisponibilidadController(servicio,
                    NSubstitute.Substitute.For<SistemaReservas.Application.Interfaces.IVerificacionDisponibilidadService>());
                var respuesta = Assert.IsType<OkObjectResult>(await controller.ObtenerDisponibilidad(id));
                Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<DisponibilidadResponse>>(respuesta.Value));
                Assert.Equal(1, await consulta.Disponibilidades.CountAsync());
                (await consulta.Laboratorios.SingleAsync()).Estado = ReglasEstadoLaboratorio.Habilitado;
                await consulta.SaveChangesAsync();
            }
            using var reactivado = db.CrearContexto();
            Assert.Single(await new DisponibilidadRepository(reactivado).ObtenerPorLaboratorioAsync(id));
        }
    }
}
