using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using SistemaReservas.API.Controllers;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Domain.Rules;
using SistemaReservas.Infrastructure.Interfaces;
using SistemaReservas.Application.Implementation;

namespace SistemaReservas.Tests.Controllers
{
    public class ReservasControllerCancelacionTests
    {
        private const int Propietario = 5;
        private const int OtroUsuario = 27;
        private const int ReservaId = 7;

        private static ClaimsPrincipal Usuario(string id, string rol)
        {
            var identidad = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, id),
                    new Claim(ClaimTypes.Name, "prueba"),
                    new Claim(ClaimTypes.Role, rol)
                },
                "PruebaAuth");

            return new ClaimsPrincipal(identidad);
        }

        private static ReservasController CrearControlador(
            IReservaRepository repositorio,
            ClaimsPrincipal usuario)
        {
            var reloj = new FakeTimeProvider(
                new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero));

            var servicio = SistemaReservas.Tests.Fakes.ReservaServiceFactory.Crear(repositorio, reloj, new MapsterMapper.Mapper(SistemaReservas.Application.Mapper.MapsterConfig.RegisterMaps()));

            return new ReservasController(servicio)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = usuario }
                }
            };
        }

        private static Reserva NuevaReserva(string estado) =>
            new()
            {
                Id = ReservaId,
                LaboratorioId = 3,
                UsuarioId = Propietario,
                Fecha = new DateTime(2026, 12, 1),
                HoraInicio = new TimeSpan(9, 0, 0),
                HoraFin = new TimeSpan(11, 0, 0),
                Estado = estado
            };

        [Fact]
        public async Task CancelarReserva_PropietarioCancelaLaSuya_DevuelveOk()
        {
            // Arrange
            var reserva = NuevaReserva("Activa");
            var repositorio = Substitute.For<IReservaRepository>();
            repositorio.ObtenerPorIdAsync(ReservaId).Returns(reserva);
            var controlador = CrearControlador(
                repositorio,
                Usuario(Propietario.ToString(), "Usuario"));

            // Act
            var resultado = await controlador.CancelarReserva(ReservaId);

            // Assert
            Assert.IsType<OkObjectResult>(resultado);
            Assert.Equal("Cancelada", reserva.Estado);
            await repositorio.Received(1).GuardarCambiosAsync();
        }

        [Fact]
        public async Task CancelarReserva_UsuarioSobreReservaAjena_Devuelve403YNoCancela()
        {
            // Arrange
            var reserva = NuevaReserva("Activa");
            var repositorio = Substitute.For<IReservaRepository>();
            repositorio.ObtenerPorIdAsync(ReservaId).Returns(reserva);
            var controlador = CrearControlador(
                repositorio,
                Usuario(OtroUsuario.ToString(), "Usuario"));

            // Act
            var resultado = await controlador.CancelarReserva(ReservaId);

            // Assert
            var objeto = Assert.IsType<ObjectResult>(resultado);
            Assert.Equal(StatusCodes.Status403Forbidden, objeto.StatusCode);
            Assert.Equal("Activa", reserva.Estado);
            await repositorio.DidNotReceive().GuardarCambiosAsync();
        }

        [Fact]
        public async Task CancelarReserva_AdministradorSobreReservaAjena_DevuelveOk()
        {
            // Arrange
            var reserva = NuevaReserva("Activa");
            var repositorio = Substitute.For<IReservaRepository>();
            repositorio.ObtenerPorIdAsync(ReservaId).Returns(reserva);
            var controlador = CrearControlador(
                repositorio,
                Usuario(OtroUsuario.ToString(), ReglasRoles.Administrador));

            // Act
            var resultado = await controlador.CancelarReserva(ReservaId);

            // Assert
            Assert.IsType<OkObjectResult>(resultado);
            Assert.Equal("Cancelada", reserva.Estado);
        }

        [Fact]
        public async Task CancelarReserva_ReservaInexistente_Devuelve404()
        {
            // Arrange
            var repositorio = Substitute.For<IReservaRepository>();
            repositorio.ObtenerPorIdAsync(ReservaId).Returns((Reserva?)null);
            var controlador = CrearControlador(
                repositorio,
                Usuario(Propietario.ToString(), "Usuario"));

            // Act
            var resultado = await controlador.CancelarReserva(ReservaId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(resultado);
        }

        [Fact]
        public async Task CancelarReserva_ReservaYaCancelada_Devuelve409()
        {
            // Arrange
            var reserva = NuevaReserva("Cancelada");
            var repositorio = Substitute.For<IReservaRepository>();
            repositorio.ObtenerPorIdAsync(ReservaId).Returns(reserva);
            var controlador = CrearControlador(
                repositorio,
                Usuario(Propietario.ToString(), "Usuario"));

            // Act
            var resultado = await controlador.CancelarReserva(ReservaId);

            // Assert
            Assert.IsType<ConflictObjectResult>(resultado);
            await repositorio.DidNotReceive().GuardarCambiosAsync();
        }

        [Fact]
        public async Task CancelarReserva_SinUsuarioIdentificable_Devuelve401()
        {
            // Arrange
            var repositorio = Substitute.For<IReservaRepository>();
            var controlador = CrearControlador(
                repositorio,
                Usuario("no-es-un-numero", "Usuario"));

            // Act
            var resultado = await controlador.CancelarReserva(ReservaId);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(resultado);
            await repositorio.DidNotReceive().ObtenerPorIdAsync(Arg.Any<int>());
        }

        [Fact]
        public void CancelarReserva_ExponeRutaPostIdCancelar()
        {
            // Arrange
            var metodo = typeof(ReservasController)
                .GetMethod(nameof(ReservasController.CancelarReserva))!;

            // Act
            var atributo = metodo
                .GetCustomAttributes(typeof(HttpPostAttribute), true)
                .Cast<HttpPostAttribute>()
                .Single();

            // Assert
            Assert.Equal("{id:int}/cancelar", atributo.Template);
        }
    }
}