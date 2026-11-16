using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using SistemaReservas.API.Controllers;
using SistemaReservas.Domain.DTO;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Domain.Rules;
using SistemaReservas.Infrastructure.Interfaces;
using SistemaReservas.Tests.Fakes;

namespace SistemaReservas.Tests.Controllers
{
    // Gestión de Reservas: endpoints exclusivos del Administrador.
    public class ReservasControllerGestionTests
    {
        private static readonly DateTime Dia = new(2026, 10, 20);

        private readonly IReservaRepository _repositorio = Substitute.For<IReservaRepository>();
        private readonly FakeUsuarioRepository _usuarios = new();
        private readonly ReservasController _controller;

        public ReservasControllerGestionTests()
        {
            _usuarios.Agregar(new Usuario { Id = 27, Nombre = "Ana Responsable", Username = "ana", Rol = ReglasRoles.Usuario });

            _repositorio
                .CrearAsync(Arg.Any<Reserva>())
                .Returns(llamada =>
                {
                    var reserva = llamada.Arg<Reserva>();
                    reserva.Id = 10;
                    return reserva;
                });

            var servicio = ReservaServiceFactory.Crear(
                _repositorio,
                new FakeTimeProvider(new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero)),
                new MapsterMapper.Mapper(SistemaReservas.Application.Mapper.MapsterConfig.RegisterMaps()),
                _usuarios);

            _controller = new ReservasController(servicio)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
        }

        private static JsonElement Cuerpo(IActionResult respuesta)
        {
            var objeto = Assert.IsAssignableFrom<ObjectResult>(respuesta);
            var opciones = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            return JsonDocument.Parse(JsonSerializer.Serialize(objeto.Value, opciones)).RootElement;
        }

        private static CrearReservaParaUsuarioRequest Solicitud(string? username) =>
            new(username, 3, new DateOnly(2026, 10, 20), new TimeSpan(9, 0, 0), new TimeSpan(11, 0, 0));

        [Theory]
        [InlineData(nameof(ReservasController.ObtenerTodasLasReservas))]
        [InlineData(nameof(ReservasController.CrearReservaParaUsuario))]
        public void AccionesDeGestion_SonExclusivasDelAdministrador(string accion)
        {
            var metodo = typeof(ReservasController).GetMethod(accion)!;

            var autorizacion = metodo.GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(autorizacion);
            Assert.Equal(ReglasRoles.Administrador, autorizacion!.Roles);
        }

        [Fact]
        public async Task ObtenerTodasLasReservas_Devuelve200ConLosNombresJsonDelContrato()
        {
            // Arrange
            _repositorio.ObtenerTodasAsync().Returns(new List<Reserva>
            {
                new()
                {
                    Id = 15, LaboratorioId = 3, UsuarioId = 27, Fecha = Dia,
                    HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(11, 0, 0), Estado = "Activa",
                    Laboratorio = new Laboratorio { Id = 3, Nombre = "Laboratorio de Software" },
                    Usuario = new Usuario { Id = 27, Nombre = "Ana Responsable", Username = "ana", PasswordHash = "hash-privado" }
                }
            });

            // Act
            var respuesta = await _controller.ObtenerTodasLasReservas();

            // Assert
            Assert.IsType<OkObjectResult>(respuesta);
            var reserva = Assert.Single(Cuerpo(respuesta).EnumerateArray());
            Assert.Equal(15, reserva.GetProperty("id").GetInt32());
            Assert.Equal(3, reserva.GetProperty("laboratorioId").GetInt32());
            Assert.Equal("Laboratorio de Software", reserva.GetProperty("nombreLaboratorio").GetString());
            Assert.Equal("Ana Responsable", reserva.GetProperty("nombreUsuario").GetString());
            Assert.Equal("ana", reserva.GetProperty("username").GetString());
            Assert.Equal("2026-10-20", reserva.GetProperty("fecha").GetString());
            Assert.Equal("09:00:00", reserva.GetProperty("horaInicio").GetString());
            Assert.Equal("11:00:00", reserva.GetProperty("horaFin").GetString());
            Assert.Equal("Activa", reserva.GetProperty("estado").GetString());
            Assert.False(reserva.TryGetProperty("passwordHash", out _));
            Assert.False(reserva.TryGetProperty("usuario", out _));
        }

        [Fact]
        public async Task CrearReservaParaUsuario_DatosValidos_Devuelve201ConElResponsable()
        {
            // Act
            var respuesta = await _controller.CrearReservaParaUsuario(Solicitud(" ana "));

            // Assert
            var creada = Assert.IsType<ObjectResult>(respuesta);
            Assert.Equal(StatusCodes.Status201Created, creada.StatusCode);

            var cuerpo = Cuerpo(respuesta);
            Assert.Equal("Reserva registrada correctamente.", cuerpo.GetProperty("mensaje").GetString());
            var reserva = cuerpo.GetProperty("reserva");
            Assert.Equal(10, reserva.GetProperty("id").GetInt32());
            Assert.Equal("ana", reserva.GetProperty("responsable").GetString());
            Assert.Equal("Activa", reserva.GetProperty("estado").GetString());
            await _repositorio.Received(1).CrearAsync(Arg.Is<Reserva>(r => r.UsuarioId == 27));
        }

        [Fact]
        public async Task CrearReservaParaUsuario_UsuarioInexistente_Devuelve400ConErrores()
        {
            // Act
            var respuesta = await _controller.CrearReservaParaUsuario(Solicitud("nadie"));

            // Assert
            Assert.IsType<BadRequestObjectResult>(respuesta);
            var errores = Cuerpo(respuesta).GetProperty("errores");
            Assert.Equal(
                "No existe un usuario con ese nombre de usuario.",
                errores.GetProperty("username").GetString());
            await _repositorio.DidNotReceive().CrearAsync(Arg.Any<Reserva>());
        }
    }
}
