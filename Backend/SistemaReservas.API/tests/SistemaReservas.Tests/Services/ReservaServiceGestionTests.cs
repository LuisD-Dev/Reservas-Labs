using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using SistemaReservas.Domain.DTO;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Domain.Rules;
using SistemaReservas.Infrastructure.Interfaces;
using SistemaReservas.Tests.Fakes;

namespace SistemaReservas.Tests.Services
{
    // Gestión de Reservas: consulta de todas las reservas y registro a nombre de otro usuario.
    public class ReservaServiceGestionTests
    {
        private static readonly DateTime Dia = new(2026, 10, 20);

        private readonly FakeTimeProvider _reloj = new(
            new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));

        private readonly MapsterMapper.IMapper _mapper =
            new MapsterMapper.Mapper(SistemaReservas.Application.Mapper.MapsterConfig.RegisterMaps());

        private readonly IReservaRepository _repositorio = Substitute.For<IReservaRepository>();
        private readonly FakeUsuarioRepository _usuarios = new();

        public ReservaServiceGestionTests()
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
        }

        private SistemaReservas.Application.Implementation.ReservaService Servicio() =>
            ReservaServiceFactory.Crear(_repositorio, _reloj, _mapper, _usuarios);

        private static CrearReservaParaUsuarioRequest Solicitud(string? username) =>
            new(username, 3, new DateOnly(2026, 10, 20), new TimeSpan(9, 0, 0), new TimeSpan(11, 0, 0));

        [Fact]
        public async Task ObtenerTodasAsync_DevuelveLasReservasDeTodosLosUsuarios()
        {
            // Arrange
            _repositorio.ObtenerTodasAsync().Returns(new List<Reserva>
            {
                NuevaReserva(1, 27, "Ana Responsable", "ana", "Software", "Activa"),
                NuevaReserva(2, 31, "Luis Docente", "luis", "Redes", "Cancelada")
            });

            // Act
            var resultado = await Servicio().ObtenerTodasAsync();

            // Assert
            Assert.Equal(2, resultado.Count);
            Assert.Equal(
                new ReservaGestionResponse(1, 3, "Software", "Ana Responsable", "ana",
                    new DateOnly(2026, 10, 20), new TimeSpan(9, 0, 0), new TimeSpan(11, 0, 0), "Activa"),
                resultado[0]);
            Assert.Equal("Luis Docente", resultado[1].NombreUsuario);
            Assert.Equal("Cancelada", resultado[1].Estado);
        }

        [Fact]
        public async Task ObtenerTodasAsync_SinReservas_DevuelveListaVacia()
        {
            // Arrange
            _repositorio.ObtenerTodasAsync().Returns(new List<Reserva>());

            // Act
            var resultado = await Servicio().ObtenerTodasAsync();

            // Assert
            Assert.Empty(resultado);
        }

        [Fact]
        public async Task CrearParaUsuarioAsync_UsuarioExistente_GuardaLaReservaConSuId()
        {
            // Act
            var resultado = await Servicio().CrearParaUsuarioAsync(Solicitud("ana"));

            // Assert
            Assert.True(resultado.Exitoso);
            Assert.Equal("Activa", resultado.Reserva!.Estado);
            await _repositorio.Received(1).CrearAsync(Arg.Is<Reserva>(reserva =>
                reserva.UsuarioId == 27 &&
                reserva.LaboratorioId == 3 &&
                reserva.Fecha == Dia &&
                reserva.Estado == "Activa"));
        }

        [Fact]
        public async Task CrearParaUsuarioAsync_ConEspaciosAlrededorDelUsuario_LoEncuentra()
        {
            // Act
            var resultado = await Servicio().CrearParaUsuarioAsync(Solicitud("  ana "));

            // Assert
            Assert.True(resultado.Exitoso);
            await _repositorio.Received(1).CrearAsync(Arg.Is<Reserva>(reserva => reserva.UsuarioId == 27));
        }

        [Fact]
        public async Task CrearParaUsuarioAsync_UsuarioInexistente_NoGuardaReserva()
        {
            // Act
            var resultado = await Servicio().CrearParaUsuarioAsync(Solicitud("nadie"));

            // Assert
            Assert.False(resultado.Exitoso);
            Assert.Equal("No existe un usuario con ese nombre de usuario.", resultado.Errores["username"]);
            await _repositorio.DidNotReceive().CrearAsync(Arg.Any<Reserva>());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CrearParaUsuarioAsync_SinUsuario_NoConsultaNiGuarda(string? username)
        {
            // Act
            var resultado = await Servicio().CrearParaUsuarioAsync(Solicitud(username));

            // Assert
            Assert.False(resultado.Exitoso);
            Assert.Equal("Indica el usuario responsable de la reserva.", resultado.Errores["username"]);
            Assert.Empty(_usuarios.Llamadas);
            await _repositorio.DidNotReceive().CrearAsync(Arg.Any<Reserva>());
        }

        [Fact]
        public async Task CrearParaUsuarioAsync_FechaPasada_AplicaLasMismasValidaciones()
        {
            // Arrange
            var solicitud = Solicitud("ana") with { Fecha = new DateOnly(2026, 10, 16) };

            // Act
            var resultado = await Servicio().CrearParaUsuarioAsync(solicitud);

            // Assert
            Assert.False(resultado.Exitoso);
            Assert.Equal(ReglasReserva.MensajeFechaPasada, resultado.Errores["fecha"]);
            await _repositorio.DidNotReceive().CrearAsync(Arg.Any<Reserva>());
        }

        [Fact]
        public async Task CrearParaUsuarioAsync_HorarioTraslapado_NoGuardaReserva()
        {
            // Arrange
            _repositorio
                .ObtenerActivasPorLaboratorioYFechaAsync(3, Dia)
                .Returns(new List<Reserva>
                {
                    new()
                    {
                        LaboratorioId = 3, UsuarioId = 5, Fecha = Dia,
                        HoraInicio = new TimeSpan(10, 0, 0), HoraFin = new TimeSpan(12, 0, 0), Estado = "Activa"
                    }
                });

            // Act
            var resultado = await Servicio().CrearParaUsuarioAsync(Solicitud("ana"));

            // Assert
            Assert.False(resultado.Exitoso);
            Assert.Equal(ReglasTraslape.MensajeConflicto, resultado.Errores["conflicto"]);
            await _repositorio.DidNotReceive().CrearAsync(Arg.Any<Reserva>());
        }

        private static Reserva NuevaReserva(
            int id, int usuarioId, string nombre, string username, string laboratorio, string estado) =>
            new()
            {
                Id = id,
                LaboratorioId = 3,
                UsuarioId = usuarioId,
                Fecha = Dia,
                HoraInicio = new TimeSpan(9, 0, 0),
                HoraFin = new TimeSpan(11, 0, 0),
                Estado = estado,
                Laboratorio = new Laboratorio { Id = 3, Nombre = laboratorio },
                Usuario = new Usuario { Id = usuarioId, Nombre = nombre, Username = username, PasswordHash = "hash-privado" }
            };
    }
}
