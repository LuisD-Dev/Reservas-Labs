using NSubstitute;
using Microsoft.Extensions.Time.Testing;
using SistemaReservas.API.DTOs;
using SistemaReservas.API.Models;
using SistemaReservas.API.Repositories;
using SistemaReservas.API.Services;

namespace SistemaReservas.Tests.Services
{
    public class ReservaServiceConflictosTests
    {
        private readonly FakeTimeProvider _reloj = new(
            new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));

        private static readonly DateTime Dia =
            new(2026, 10, 20);

        private static IReadOnlyList<Reserva> ReservasExistentes() =>
            new List<Reserva>
            {
                new()
                {
                    LaboratorioId = 3,
                    UsuarioId = 5,
                    Fecha = Dia,
                    HoraInicio = new TimeSpan(9, 0, 0),
                    HoraFin = new TimeSpan(11, 0, 0),
                    Estado = "Activa"
                }
            };

        private static CrearReservaRequest Solicitud(int inicio, int fin) =>
            new(3, new DateOnly(2026, 10, 20),
                new TimeSpan(inicio, 0, 0),
                new TimeSpan(fin, 0, 0));

        [Theory]
        [InlineData(10, 12)]  // empieza dentro de la existente
        [InlineData(8, 10)]   // termina dentro de la existente
        [InlineData(10, 11)]  // contenida en la existente
        [InlineData(9, 11)]   // idéntica
        public async Task CrearAsync_ReservaTraslapada_NoGuardaReserva(
            int inicio,
            int fin)
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            repositorio
                .ObtenerActivasPorLaboratorioYFechaAsync(3, Dia)
                .Returns(ReservasExistentes());

            var servicio =
                new ReservaService(repositorio);

            // Act
            var resultado =
                await servicio.CrearAsync(27, Solicitud(inicio, fin));

            // Assert
            Assert.False(resultado.Exitoso);
            Assert.Null(resultado.Reserva);
            Assert.Equal(
                ReservaService.MensajeConflicto,
                resultado.Errores["conflicto"]);

            await repositorio
                .DidNotReceive()
                .CrearAsync(Arg.Any<Reserva>());
        }

        [Fact]
        public async Task CrearAsync_ReservaConsecutiva_GuardaReserva()
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            repositorio
                .ObtenerActivasPorLaboratorioYFechaAsync(3, Dia)
                .Returns(ReservasExistentes());

            repositorio
                .CrearAsync(Arg.Any<Reserva>())
                .Returns(llamada => llamada.Arg<Reserva>());

            var servicio =
                new ReservaService(repositorio);

            // Act
            var resultado =
                await servicio.CrearAsync(27, Solicitud(11, 12));

            // Assert
            Assert.True(resultado.Exitoso);

            await repositorio
                .Received(1)
                .CrearAsync(Arg.Any<Reserva>());
        }

        [Fact]
        public async Task CrearAsync_SinReservasPrevias_GuardaReservaYConsultaLaboratorioYFecha()
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            repositorio
                .ObtenerActivasPorLaboratorioYFechaAsync(3, Dia)
                .Returns(new List<Reserva>());

            repositorio
                .CrearAsync(Arg.Any<Reserva>())
                .Returns(llamada => llamada.Arg<Reserva>());

            var servicio =
                new ReservaService(repositorio);

            // Act
            var resultado =
                await servicio.CrearAsync(27, Solicitud(9, 11));

            // Assert
            Assert.True(resultado.Exitoso);

            await repositorio
                .Received(1)
                .ObtenerActivasPorLaboratorioYFechaAsync(3, Dia);
        }

        [Fact]
        public async Task CrearAsync_DatosInvalidos_NoConsultaConflictos()
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            var servicio =
                new ReservaService(repositorio);

            var request = new CrearReservaRequest(null, null, null, null);

            // Act
            var resultado =
                await servicio.CrearAsync(27, request);

            // Assert
            Assert.False(resultado.Exitoso);

            await repositorio
                .DidNotReceive()
                .ObtenerActivasPorLaboratorioYFechaAsync(
                    Arg.Any<int>(),
                    Arg.Any<DateTime>());
        }
    }
}

