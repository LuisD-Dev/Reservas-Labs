using NSubstitute;
using SistemaReservas.Domain.DTO;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Infrastructure.Interfaces;
using SistemaReservas.Application.Implementation;
using Xunit;

namespace SistemaReservas.Tests.Services
{
    public class ReservaServiceCancelacionTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task CancelarAsync_UsuarioInvalido_NoConsultaNiGuarda(
            int usuarioId)
        {
            var repositorio = Substitute.For<IReservaRepository>();
            var servicio = new ReservaService(
                repositorio, TimeProvider.System, new MapsterMapper.Mapper(SistemaReservas.Application.Mapper.MapsterConfig.RegisterMaps()));

            var resultado = await servicio.CancelarAsync(10, usuarioId);

            Assert.Equal(
                EstadoResultadoCancelacion.UsuarioInvalido,
                resultado.Estado);

            Assert.False(resultado.Exitoso);
            Assert.Empty(repositorio.ReceivedCalls());
        }

        [Fact]
        public async Task CancelarAsync_ReservaInexistente_NoGuarda()
        {
            var repositorio = Substitute.For<IReservaRepository>();

            repositorio.ObtenerPorIdAsync(10)
                .Returns(Task.FromResult<Reserva?>(null));

            var servicio = new ReservaService(
                repositorio, TimeProvider.System, new MapsterMapper.Mapper(SistemaReservas.Application.Mapper.MapsterConfig.RegisterMaps()));

            var resultado = await servicio.CancelarAsync(10, 27);

            Assert.Equal(
                EstadoResultadoCancelacion.NoEncontrada,
                resultado.Estado);

            Assert.False(resultado.Exitoso);
            await repositorio.DidNotReceive().GuardarCambiosAsync();
        }

        [Theory]
        [InlineData("Activa")]
        [InlineData("Cancelada")]
        public async Task CancelarAsync_ReservaAjena_NoModificaNiGuarda(
            string estado)
        {
            var reserva = new Reserva
            {
                Id = 10,
                UsuarioId = 99,
                Estado = estado
            };

            var repositorio = Substitute.For<IReservaRepository>();

            repositorio.ObtenerPorIdAsync(10)
                .Returns(Task.FromResult<Reserva?>(reserva));

            var servicio = new ReservaService(
                repositorio, TimeProvider.System, new MapsterMapper.Mapper(SistemaReservas.Application.Mapper.MapsterConfig.RegisterMaps()));

            var resultado = await servicio.CancelarAsync(10, 27);

            Assert.Equal(
                EstadoResultadoCancelacion.NoAutorizada,
                resultado.Estado);

            Assert.False(resultado.Exitoso);
            Assert.Equal(estado, reserva.Estado);
            await repositorio.DidNotReceive().GuardarCambiosAsync();
        }

        [Theory]
        [InlineData(
            "Cancelada",
            EstadoResultadoCancelacion.YaCancelada)]
        [InlineData(
            "Desconocido",
            EstadoResultadoCancelacion.EstadoInvalido)]
        public async Task CancelarAsync_EstadoNoCancelable_NoGuarda(
            string estado,
            EstadoResultadoCancelacion resultadoEsperado)
        {
            var reserva = new Reserva
            {
                Id = 10,
                UsuarioId = 27,
                Estado = estado
            };

            var repositorio = Substitute.For<IReservaRepository>();

            repositorio.ObtenerPorIdAsync(10)
                .Returns(Task.FromResult<Reserva?>(reserva));

            var servicio = new ReservaService(
                repositorio, TimeProvider.System, new MapsterMapper.Mapper(SistemaReservas.Application.Mapper.MapsterConfig.RegisterMaps()));

            var resultado = await servicio.CancelarAsync(10, 27);

            Assert.Equal(resultadoEsperado, resultado.Estado);
            Assert.False(resultado.Exitoso);
            Assert.Equal(estado, reserva.Estado);
            await repositorio.DidNotReceive().GuardarCambiosAsync();
        }

        [Fact]
        public async Task CancelarAsync_PropietarioDeReservaActiva_CancelaYGuarda()
        {
            var reserva = new Reserva
            {
                Id = 10,
                UsuarioId = 27,
                LaboratorioId = 3,
                Fecha = new DateTime(2026, 11, 10),
                HoraInicio = new TimeSpan(9, 0, 0),
                HoraFin = new TimeSpan(10, 0, 0),
                Estado = "Activa"
            };

            var repositorio = Substitute.For<IReservaRepository>();

            repositorio.ObtenerPorIdAsync(10)
                .Returns(Task.FromResult<Reserva?>(reserva));

            string? estadoAlGuardar = null;

            repositorio.GuardarCambiosAsync().Returns(_ =>
            {
                estadoAlGuardar = reserva.Estado;
                return Task.CompletedTask;
            });

            var servicio = new ReservaService(
                repositorio, TimeProvider.System, new MapsterMapper.Mapper(SistemaReservas.Application.Mapper.MapsterConfig.RegisterMaps()));

            var resultado = await servicio.CancelarAsync(10, 27);

            Assert.True(resultado.Exitoso);
            Assert.Equal(
                EstadoResultadoCancelacion.Exitosa,
                resultado.Estado);

            Assert.Equal("Cancelada", estadoAlGuardar);
            Assert.Equal("Cancelada", reserva.Estado);
            Assert.Equal(27, reserva.UsuarioId);
            Assert.Equal(3, reserva.LaboratorioId);
            Assert.Equal(new DateTime(2026, 11, 10), reserva.Fecha);
            Assert.Equal(new TimeSpan(9, 0, 0), reserva.HoraInicio);
            Assert.Equal(new TimeSpan(10, 0, 0), reserva.HoraFin);

            await repositorio.Received(1).ObtenerPorIdAsync(10);
            await repositorio.Received(1).GuardarCambiosAsync();
        }
    }
}