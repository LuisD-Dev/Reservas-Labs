using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Microsoft.Extensions.Time.Testing;
using SistemaReservas.API.Controllers;
using SistemaReservas.API.DTOs;
using SistemaReservas.API.Models;
using SistemaReservas.API.Repositories;
using SistemaReservas.API.Services;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using SistemaReservas.API.Reglas;

namespace SistemaReservas.Tests.Controllers
{
    public class ReservasControllerTests
    {
        private readonly IReservaRepository
            _repositorio =
                Substitute.For<IReservaRepository>();

        private readonly ReservasController
            _controller;

        public ReservasControllerTests()
        {
            var servicio =
                new ReservaService(_repositorio, new FakeTimeProvider(
                    new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero)));

            _controller =
                new ReservasController(servicio);

            _controller.ControllerContext =
                new ControllerContext
                {
                    HttpContext =
                        new DefaultHttpContext()
                };
        }

        private void AutenticarUsuario(
            int usuarioId,
            string nombre)
        {
            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    usuarioId.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    nombre)
            };

            var identidad =
                new ClaimsIdentity(
                    claims,
                    "Prueba");

            _controller.ControllerContext
                .HttpContext.User =
                    new ClaimsPrincipal(identidad);
        }

        private static CrearReservaRequest
            CrearRequestValido()
        {
            return new CrearReservaRequest(
                3,
                new DateOnly(2026, 10, 20),
                new TimeSpan(9, 0, 0),
                new TimeSpan(11, 0, 0));
        }

        [Fact]
        public async Task CrearReserva_SinIdentificador_Devuelve401()
        {
            // Act
            var respuesta =
                await _controller.CrearReserva(
                    CrearRequestValido());

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(
                respuesta);

            await _repositorio
                .DidNotReceive()
                .CrearAsync(Arg.Any<Reserva>());
        }

        [Fact]
        public async Task CrearReserva_DatosValidos_UsaIdDelUsuarioYDevuelve201()
        {
            // Arrange
            AutenticarUsuario(
                27,
                "Ana Responsable");

            _repositorio
                .CrearAsync(Arg.Any<Reserva>())
                .Returns(llamada =>
                {
                    var reserva =
                        llamada.Arg<Reserva>();

                    reserva.Id = 15;

                    return reserva;
                });

            // Act
            var respuesta =
                await _controller.CrearReserva(
                    CrearRequestValido());

            // Assert
            var creado =
                Assert.IsType<ObjectResult>(
                    respuesta);

            Assert.Equal(
                StatusCodes.Status201Created,
                creado.StatusCode);

            await _repositorio
                .Received(1)
                .CrearAsync(
                    Arg.Is<Reserva>(reserva =>
                        reserva.UsuarioId == 27 &&
                        reserva.Id == 15));
        }

        [Fact]
        public async Task CrearReserva_DatosIncompletos_Devuelve400()
        {
            // Arrange
            AutenticarUsuario(
                27,
                "Ana Responsable");

            var request =
                new CrearReservaRequest(
                    null,
                    null,
                    null,
                    null);

            // Act
            var respuesta =
                await _controller.CrearReserva(
                    request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(
                respuesta);

            await _repositorio
                .DidNotReceive()
                .CrearAsync(Arg.Any<Reserva>());
        }
        [Fact]
        public void ReservasController_RequiereAutenticacion()
        {
            // Act
            var atributo =
                typeof(ReservasController)
                    .GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            Assert.NotNull(atributo);
        }

        [Fact]
        public void ProbarAccesoAdministrador_RequiereRolAdministrador()
        {
            // Arrange
            var metodo =
                typeof(ReservasController)
                    .GetMethod(
                        nameof(
                            ReservasController
                                .ProbarAccesoAdministrador));

            Assert.NotNull(metodo);

            // Act
            var atributo =
                metodo.GetCustomAttribute<
                    AuthorizeAttribute>();

            // Assert
            Assert.NotNull(atributo);

            Assert.Equal(
                ReglasRoles.Administrador,
                atributo.Roles);
        }
    }
}
