using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using SistemaReservas.Domain.DTO;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Infrastructure.Interfaces;
using SistemaReservas.Application.Implementation;

namespace SistemaReservas.Tests.Services
{
    public class ReservaServiceCancelacionAdminTests
    {
        private const int Propietario = 5;
        private const int OtroUsuario = 27;
        private const int ReservaId = 7;

        private readonly FakeTimeProvider _reloj = new(
            new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero));

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
        public async Task CancelarAsync_AdministradorCancelaReservaAjena_CancelaYGuarda()
        {
            // Arrange
            var reserva = NuevaReserva("Activa");
            var repositorio = Substitute.For<IReservaRepository>();
            repositorio.ObtenerPorIdAsync(ReservaId).Returns(reserva);
            var servicio = new ReservaService(repositorio, _reloj, new MapsterMapper.Mapper(SistemaReservas.Application.Mapper.MapsterConfig.RegisterMaps()));

            // Act
            var resultado =
                await servicio.CancelarAsync(
                    ReservaId,
                    OtroUsuario,
                    esAdministrador: true);

            // Assert
            Assert.Equal(EstadoResultadoCancelacion.Exitosa, resultado.Estado);
            Assert.Equal("Cancelada", reserva.Estado);
            await repositorio.Received(1).GuardarCambiosAsync();
        }

        [Fact]
        public async Task CancelarAsync_SinIndicarAdministrador_NoPermiteCancelarReservaAjena()
        {
            // Arrange
            var reserva = NuevaReserva("Activa");
            var repositorio = Substitute.For<IReservaRepository>();
            repositorio.ObtenerPorIdAsync(ReservaId).Returns(reserva);
            var servicio = new ReservaService(repositorio, _reloj, new MapsterMapper.Mapper(SistemaReservas.Application.Mapper.MapsterConfig.RegisterMaps()));

            // Act
            var resultado =
                await servicio.CancelarAsync(ReservaId, OtroUsuario);

            // Assert
            Assert.Equal(EstadoResultadoCancelacion.NoAutorizada, resultado.Estado);
            Assert.Equal("Activa", reserva.Estado);
            await repositorio.DidNotReceive().GuardarCambiosAsync();
        }

        [Fact]
        public async Task CancelarAsync_AdministradorSobreReservaYaCancelada_DevuelveYaCancelada()
        {
            // Arrange
            var reserva = NuevaReserva("Cancelada");
            var repositorio = Substitute.For<IReservaRepository>();
            repositorio.ObtenerPorIdAsync(ReservaId).Returns(reserva);
            var servicio = new ReservaService(repositorio, _reloj, new MapsterMapper.Mapper(SistemaReservas.Application.Mapper.MapsterConfig.RegisterMaps()));

            // Act
            var resultado =
                await servicio.CancelarAsync(
                    ReservaId,
                    OtroUsuario,
                    esAdministrador: true);

            // Assert
            Assert.Equal(EstadoResultadoCancelacion.YaCancelada, resultado.Estado);
            await repositorio.DidNotReceive().GuardarCambiosAsync();
        }

        [Fact]
        public async Task CancelarAsync_AdministradorSobreReservaInexistente_DevuelveNoEncontrada()
        {
            // Arrange
            var repositorio = Substitute.For<IReservaRepository>();
            repositorio.ObtenerPorIdAsync(ReservaId).Returns((Reserva?)null);
            var servicio = new ReservaService(repositorio, _reloj, new MapsterMapper.Mapper(SistemaReservas.Application.Mapper.MapsterConfig.RegisterMaps()));

            // Act
            var resultado =
                await servicio.CancelarAsync(
                    ReservaId,
                    OtroUsuario,
                    esAdministrador: true);

            // Assert
            Assert.Equal(EstadoResultadoCancelacion.NoEncontrada, resultado.Estado);
            await repositorio.DidNotReceive().GuardarCambiosAsync();
        }
    }
}