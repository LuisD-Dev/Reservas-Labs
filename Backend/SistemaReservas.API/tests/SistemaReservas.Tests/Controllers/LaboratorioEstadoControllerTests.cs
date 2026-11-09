using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MapsterMapper;
using NSubstitute;
using SistemaReservas.API.Controllers;
using SistemaReservas.Application.Implementation;
using SistemaReservas.Application.Mapper;
using SistemaReservas.Domain.DTO;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Domain.Rules;
using SistemaReservas.Infrastructure.Interfaces;

namespace SistemaReservas.Tests.Controllers
{
    public class LaboratorioEstadoControllerTests
    {
        private readonly ILaboratorioRepository _repository = Substitute.For<ILaboratorioRepository>();
        private LaboratoriosController Controller() => new(new LaboratorioService(_repository, new Mapper(MapsterConfig.RegisterMaps())));

        [Fact]
        public void Accion_UsaPatchRutaYExigeAdministradorSinAutorizarUsuarioNormal()
        {
            var tipo = typeof(LaboratoriosController);
            var accion = tipo.GetMethod(nameof(LaboratoriosController.ActualizarEstado))!;
            Assert.Equal("api/[controller]", tipo.GetCustomAttribute<RouteAttribute>()!.Template);
            Assert.Equal("{id}/estado", Assert.Single(accion.GetCustomAttributes<HttpPatchAttribute>()).Template);
            Assert.Single(tipo.GetCustomAttributes<AuthorizeAttribute>());
            var autorizacion = Assert.Single(accion.GetCustomAttributes<AuthorizeAttribute>());
            Assert.Equal(ReglasRoles.Administrador, autorizacion.Roles);
            Assert.DoesNotContain(ReglasRoles.Usuario, autorizacion.Roles!.Split(','));
            Assert.Empty(tipo.GetCustomAttributes<AllowAnonymousAttribute>());
            Assert.Empty(accion.GetCustomAttributes<AllowAnonymousAttribute>());
        }

        [Fact]
        public async Task Actualizar_Devuelve200ConDtoPublicoSinDatosInternos()
        {
            _repository.ObtenerPorIdAsync(1).Returns(new Laboratorio { Id = 1, Nombre = "Computo", Ubicacion = "A", Capacidad = 30, Estado = "Habilitado" });
            var respuesta = Assert.IsType<OkObjectResult>(await Controller().ActualizarEstado(1, new("Fuera de servicio")));
            Assert.Equal(200, respuesta.StatusCode);
            using var json = JsonDocument.Parse(JsonSerializer.Serialize(respuesta.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            Assert.Equal("Estado del laboratorio actualizado correctamente.", json.RootElement.GetProperty("mensaje").GetString());
            var laboratorio = json.RootElement.GetProperty("laboratorio");
            Assert.Equal(new[] { "capacidad", "estado", "id", "nombre", "ubicacion" }, laboratorio.EnumerateObject().Select(p => p.Name).Order());
            Assert.Equal(1, laboratorio.GetProperty("id").GetInt32());
            Assert.Equal("Fuera de servicio", laboratorio.GetProperty("estado").GetString());
            await _repository.Received(1).ObtenerPorIdAsync(1);
        }

        [Theory]
        [InlineData(0, "Habilitado")]
        [InlineData(-1, "Habilitado")]
        [InlineData(1, "")]
        [InlineData(1, null)]
        [InlineData(1, "Otro")]
        public async Task Actualizar_DatosInvalidos_Devuelve400(int id, string? estado)
        {
            var respuesta = Assert.IsType<BadRequestObjectResult>(await Controller().ActualizarEstado(id, new(estado)));
            Assert.Equal(400, respuesta.StatusCode);
            await _repository.DidNotReceive().GuardarCambiosAsync();
        }

        [Fact]
        public async Task Actualizar_CuerpoNulo_Devuelve400()
        {
            Assert.IsType<BadRequestObjectResult>(await Controller().ActualizarEstado(1, null));
            await _repository.DidNotReceive().ObtenerPorIdAsync(Arg.Any<int>());
        }

        [Fact]
        public async Task Actualizar_Inexistente_Devuelve404()
        {
            var respuesta = Assert.IsType<NotFoundObjectResult>(await Controller().ActualizarEstado(123, new("Habilitado")));
            Assert.Equal(404, respuesta.StatusCode);
            await _repository.DidNotReceive().GuardarCambiosAsync();
        }
    }
}
