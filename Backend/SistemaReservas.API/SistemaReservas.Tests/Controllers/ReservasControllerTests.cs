using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
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
            AutenticarUsuario(
                usuarioId.ToString(),
                nombre);
        }

        // HU4 - AB#79: permite simular claims con un identificador no numérico o vacío.
        private void AutenticarUsuario(
            string identificador,
            string nombre)
        {
            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    identificador),

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

        // Convierte el cuerpo de la respuesta a JSON con las mismas opciones web
        // (camelCase) que usa ASP.NET Core, para revisar los nombres exactos de los campos.
        private static JsonElement Cuerpo(IActionResult respuesta)
        {
            var objeto =
                Assert.IsAssignableFrom<ObjectResult>(respuesta);

            var opciones =
                new JsonSerializerOptions(JsonSerializerDefaults.Web);

            return JsonDocument.Parse(
                JsonSerializer.Serialize(objeto.Value, opciones)).RootElement;
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

        // HU4 - AB#78: la respuesta 201 devuelve el detalle completo de la reserva.
        [Fact]
        public async Task CrearReserva_DatosValidos_Devuelve201ConDetalleCompleto()
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

            var cuerpo = Cuerpo(respuesta);

            Assert.Equal(
                "Reserva registrada correctamente.",
                cuerpo.GetProperty("mensaje").GetString());

            var reservaCreada =
                cuerpo.GetProperty("reserva");

            Assert.Equal(15, reservaCreada.GetProperty("id").GetInt32());
            Assert.Equal(3, reservaCreada.GetProperty("laboratorioId").GetInt32());
            Assert.Equal("2026-10-20", reservaCreada.GetProperty("fecha").GetString());
            Assert.Equal("09:00:00", reservaCreada.GetProperty("horaInicio").GetString());
            Assert.Equal("11:00:00", reservaCreada.GetProperty("horaFin").GetString());
            Assert.Equal("Activa", reservaCreada.GetProperty("estado").GetString());
        }

        // HU4 - AB#79: el responsable es el nombre del usuario del token.
        [Fact]
        public async Task CrearReserva_UsuarioAutenticado_ResponsableEsNombreDelToken()
        {
            // Arrange
            AutenticarUsuario(
                41,
                "Carlos Token");

            _repositorio
                .CrearAsync(Arg.Any<Reserva>())
                .Returns(llamada => llamada.Arg<Reserva>());

            // Act
            var respuesta =
                await _controller.CrearReserva(
                    CrearRequestValido());

            // Assert
            Assert.Equal(
                "Carlos Token",
                Cuerpo(respuesta)
                    .GetProperty("reserva")
                    .GetProperty("responsable")
                    .GetString());

            await _repositorio
                .Received(1)
                .CrearAsync(
                    Arg.Is<Reserva>(reserva =>
                        reserva.UsuarioId == 41));
        }

        // HU4 - AB#79: un claim vacío, no numérico, cero o negativo no identifica
        // al usuario; se responde 401 sin llegar al servicio ni al repositorio.
        [Theory]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("0")]
        [InlineData("-5")]
        public async Task CrearReserva_IdentificadorInvalido_Devuelve401SinConsultarRepositorio(
            string identificador)
        {
            // Arrange
            AutenticarUsuario(
                identificador,
                "Ana Responsable");

            // Act
            var respuesta =
                await _controller.CrearReserva(
                    CrearRequestValido());

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(
                respuesta);

            Assert.Equal(
                "No fue posible identificar al usuario autenticado.",
                Cuerpo(respuesta).GetProperty("mensaje").GetString());

            Assert.Empty(
                _repositorio.ReceivedCalls());
        }

        // HU4 - AB#79: el cuerpo de la petición no permite enviar el usuario;
        // el identificador solo puede salir del token.
        [Fact]
        public void CrearReservaRequest_NoIncluyeUsuarioId()
        {
            // Act
            var propiedad =
                typeof(CrearReservaRequest)
                    .GetProperty("UsuarioId");

            // Assert
            Assert.Null(propiedad);
        }

        // HU4 - AB#80: el 400 informa un mensaje general y el error de cada campo.
        [Fact]
        public async Task CrearReserva_DatosIncompletos_Devuelve400ConMensajeYErroresPorCampo()
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

            var cuerpo = Cuerpo(respuesta);

            Assert.Equal(
                "Revisa los datos de la reserva.",
                cuerpo.GetProperty("mensaje").GetString());

            var errores =
                cuerpo.GetProperty("errores");

            Assert.Equal(
                "Selecciona un laboratorio.",
                errores.GetProperty("laboratorioId").GetString());

            Assert.Equal(
                "Ingresa la fecha de la reserva.",
                errores.GetProperty("fecha").GetString());

            Assert.Equal(
                "Ingresa la hora inicial.",
                errores.GetProperty("horaInicio").GetString());

            Assert.Equal(
                "Ingresa la hora final.",
                errores.GetProperty("horaFin").GetString());
        }

        // HU4 - AB#78: el controlador no captura excepciones. Si el repositorio
        // falla (por ejemplo, la BD rechaza un laboratorio inexistente por la FK),
        // la excepción sale de CrearReserva y hoy la API responde 500.
        [Fact]
        public async Task CrearReserva_RepositorioFalla_PropagaExcepcion()
        {
            // Arrange
            AutenticarUsuario(
                27,
                "Ana Responsable");

            var excepcion =
                new InvalidOperationException(
                    "No se pudo guardar la reserva.");

            _repositorio
                .CrearAsync(Arg.Any<Reserva>())
                .ThrowsAsync(excepcion);

            // Act
            var lanzada =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _controller.CrearReserva(
                        CrearRequestValido()));

            // Assert
            Assert.Same(excepcion, lanzada);
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
