using NSubstitute;
using Microsoft.Extensions.Time.Testing;
using SistemaReservas.API.DTOs;
using SistemaReservas.API.Models;
using SistemaReservas.API.Reglas;
using SistemaReservas.API.Repositories;
using SistemaReservas.API.Services;

namespace SistemaReservas.Tests.Services
{
    public class ReservaServiceTests
    {
        private readonly FakeTimeProvider _reloj = new(
            new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));

        [Fact]
        public async Task CrearAsync_FechaPasada_NoGuardaReserva()
        {
            // Arrange
            var repositorio = Substitute.For<IReservaRepository>();
            var servicio = new ReservaService(repositorio, _reloj);
            var request = new CrearReservaRequest(
                3,
                new DateOnly(2026, 10, 16),
                new TimeSpan(9, 0, 0),
                new TimeSpan(11, 0, 0));

            // Act
            var resultado = await servicio.CrearAsync(27, request);

            // Assert
            Assert.False(resultado.Exitoso);
            Assert.Null(resultado.Reserva);
            Assert.Equal(
                "La fecha no puede ser anterior a hoy.",
                resultado.Errores["fecha"]);
            await repositorio
                .DidNotReceive()
                .CrearAsync(Arg.Any<Reserva>());
        }

        [Fact]
        public async Task CrearAsync_DatosValidos_UsaUsuarioAutenticado()
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            repositorio
                .CrearAsync(Arg.Any<Reserva>())
                .Returns(llamada =>
                {
                    var reserva =
                        llamada.Arg<Reserva>();

                    reserva.Id = 10;

                    return reserva;
                });

            var servicio =
                new ReservaService(repositorio, _reloj);

            var request = new CrearReservaRequest(
                3,
                new DateOnly(2026, 10, 20),
                new TimeSpan(9, 0, 0),
                new TimeSpan(11, 0, 0));

            const int usuarioAutenticadoId = 27;

            // Act
            var resultado =
                await servicio.CrearAsync(
                    usuarioAutenticadoId,
                    request);

            // Assert
            Assert.True(resultado.Exitoso);
            Assert.NotNull(resultado.Reserva);
            Assert.Equal(10, resultado.Reserva.Id);
            Assert.Equal(
                usuarioAutenticadoId,
                resultado.Reserva.UsuarioId);

            await repositorio
                .Received(1)
                .CrearAsync(
                    Arg.Is<Reserva>(reserva =>
                        reserva.UsuarioId ==
                            usuarioAutenticadoId &&
                        reserva.LaboratorioId == 3 &&
                        reserva.Fecha ==
                            new DateTime(2026, 10, 20) &&
                        reserva.HoraInicio ==
                            new TimeSpan(9, 0, 0) &&
                        reserva.HoraFin ==
                            new TimeSpan(11, 0, 0) &&
                        reserva.Estado == "Activa"));
        }

        [Theory]
        [InlineData(9, 9)]
        [InlineData(10, 9)]
        public async Task CrearAsync_HorarioInvalido_NoGuardaReserva(
            int horaInicio,
            int horaFin)
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            var servicio =
                new ReservaService(repositorio, _reloj);

            var request = new CrearReservaRequest(
                3,
                new DateOnly(2026, 10, 20),
                new TimeSpan(horaInicio, 0, 0),
                new TimeSpan(horaFin, 0, 0));

            // Act
            var resultado =
                await servicio.CrearAsync(27, request);

            // Assert
            Assert.False(resultado.Exitoso);
            Assert.Null(resultado.Reserva);
            Assert.Equal(
                ReglasReserva.MensajeHoraFinInvalida,
                resultado.Errores["horaFin"]);

            await repositorio
                .DidNotReceive()
                .CrearAsync(Arg.Any<Reserva>());
        }

        [Fact]
        public async Task CrearAsync_DatosIncompletos_NoGuardaReserva()
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            var servicio =
                new ReservaService(repositorio, _reloj);

            var request = new CrearReservaRequest(
                null,
                null,
                null,
                null);

            // Act
            var resultado =
                await servicio.CrearAsync(
                    27,
                    request);

            // Assert
            Assert.False(resultado.Exitoso);
            Assert.Null(resultado.Reserva);
            Assert.Equal(4, resultado.Errores.Count);

            await repositorio
                .DidNotReceive()
                .CrearAsync(Arg.Any<Reserva>());
        }
    }
}
