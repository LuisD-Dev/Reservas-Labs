using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using SistemaReservas.Application.DTOs;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Infrastructure.Repositories;
using SistemaReservas.Application.Services;
using SistemaReservas.Tests.Infraestructura;
using Xunit;

namespace SistemaReservas.Tests.Services
{
    // HU8 - AB#104: comprueba el recorrido completo con la base de datos y el servicio
    // reales (sin simulaciones): reservar, cancelar y volver a reservar el mismo horario.
    // Cada operación usa un contexto nuevo, como si fuera una petición distinta a la API.
    public sealed class ReservaServiceLiberaHorarioTests : IDisposable
    {
        private static readonly DateOnly Dia = new(2026, 10, 20);

        private readonly FakeTimeProvider _reloj = new(
            new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));

        private readonly BaseDatosEnMemoria _baseDatos = new();

        private readonly int _usuarioAId;
        private readonly int _usuarioBId;
        private readonly int _laboratorioId;

        public ReservaServiceLiberaHorarioTests()
        {
            using var contexto = _baseDatos.CrearContexto();

            var usuarioA = new Usuario
            {
                Nombre = "Usuario A",
                Username = "usuario.a",
                PasswordHash = "hash",
                Rol = "Usuario"
            };

            var usuarioB = new Usuario
            {
                Nombre = "Usuario B",
                Username = "usuario.b",
                PasswordHash = "hash",
                Rol = "Usuario"
            };

            var laboratorio = new Laboratorio
            {
                Nombre = "Laboratorio de Software",
                Ubicacion = "Edificio A",
                Capacidad = 30,
                Estado = "Habilitado"
            };

            contexto.Usuarios.AddRange(usuarioA, usuarioB);
            contexto.Laboratorios.Add(laboratorio);
            contexto.SaveChanges();

            _usuarioAId = usuarioA.Id;
            _usuarioBId = usuarioB.Id;
            _laboratorioId = laboratorio.Id;
        }

        public void Dispose()
        {
            _baseDatos.Dispose();
        }

        private async Task<ResultadoCrearReserva> ReservarAsync(
            int usuarioId,
            int horaInicio,
            int horaFin)
        {
            using var contexto = _baseDatos.CrearContexto();

            var servicio = new ReservaService(
                new ReservaRepository(contexto),
                _reloj);

            return await servicio.CrearAsync(
                usuarioId,
                new CrearReservaRequest(
                    _laboratorioId,
                    Dia,
                    new TimeSpan(horaInicio, 0, 0),
                    new TimeSpan(horaFin, 0, 0)));
        }

        private async Task<ResultadoCancelarReserva> CancelarAsync(
            int reservaId,
            int usuarioId)
        {
            using var contexto = _baseDatos.CrearContexto();

            var servicio = new ReservaService(
                new ReservaRepository(contexto),
                _reloj);

            return await servicio.CancelarAsync(reservaId, usuarioId);
        }

        private async Task<List<Reserva>> LeerReservasAsync()
        {
            using var contexto = _baseDatos.CrearContexto();

            return await contexto.Reservas
                .AsNoTracking()
                .OrderBy(reserva => reserva.Id)
                .ToListAsync();
        }

        [Fact]
        public async Task CancelarReserva_LiberaElHorario_ParaReservarDeNuevoElMismoRango()
        {
            // Arrange: A reserva de 9 a 11 y B no puede reservar ese mismo horario.
            var reservaDeA = await ReservarAsync(_usuarioAId, 9, 11);
            Assert.True(reservaDeA.Exitoso);

            var intentoAntesDeCancelar = await ReservarAsync(_usuarioBId, 9, 11);
            Assert.False(intentoAntesDeCancelar.Exitoso);
            Assert.Equal(
                ReservaService.MensajeConflicto,
                intentoAntesDeCancelar.Errores["conflicto"]);

            // Act: A cancela su reserva y B vuelve a intentarlo.
            var cancelacion = await CancelarAsync(reservaDeA.Reserva!.Id, _usuarioAId);
            var intentoDespuesDeCancelar = await ReservarAsync(_usuarioBId, 9, 11);

            // Assert
            Assert.True(cancelacion.Exitoso);
            Assert.True(intentoDespuesDeCancelar.Exitoso);
            Assert.Empty(intentoDespuesDeCancelar.Errores);

            var reservas = await LeerReservasAsync();
            Assert.Equal(2, reservas.Count);
            Assert.Equal("Cancelada", reservas[0].Estado);
            Assert.Equal(_usuarioAId, reservas[0].UsuarioId);
            Assert.Equal("Activa", reservas[1].Estado);
            Assert.Equal(_usuarioBId, reservas[1].UsuarioId);
        }

        [Fact]
        public async Task CancelarReserva_LiberaElHorario_ParaUnRangoQueSoloSeTraslapabaEnParte()
        {
            // Arrange: A reserva de 9 a 11 y la cancela.
            var reservaDeA = await ReservarAsync(_usuarioAId, 9, 11);
            Assert.True(reservaDeA.Exitoso);

            var parcialAntes = await ReservarAsync(_usuarioBId, 10, 12);
            Assert.False(parcialAntes.Exitoso);

            var cancelacion = await CancelarAsync(reservaDeA.Reserva!.Id, _usuarioAId);
            Assert.True(cancelacion.Exitoso);

            // Act: B reserva de 10 a 12, que antes chocaba solo en parte.
            var parcialDespues = await ReservarAsync(_usuarioBId, 10, 12);

            // Assert
            Assert.True(parcialDespues.Exitoso);
        }

        [Fact]
        public async Task CancelarReserva_NoLiberaOtrasReservasActivasDelMismoDia()
        {
            // Arrange: A reserva de 9 a 11 y B de 13 a 15.
            var reservaDeA = await ReservarAsync(_usuarioAId, 9, 11);
            var reservaDeB = await ReservarAsync(_usuarioBId, 13, 15);
            Assert.True(reservaDeA.Exitoso);
            Assert.True(reservaDeB.Exitoso);

            // Act: A cancela solo la suya.
            var cancelacion = await CancelarAsync(reservaDeA.Reserva!.Id, _usuarioAId);
            var sobreLaDeB = await ReservarAsync(_usuarioAId, 14, 16);
            var sobreLaCancelada = await ReservarAsync(_usuarioAId, 9, 11);

            // Assert: el horario de B sigue ocupado y el de A quedó libre.
            Assert.True(cancelacion.Exitoso);
            Assert.False(sobreLaDeB.Exitoso);
            Assert.Equal(
                ReservaService.MensajeConflicto,
                sobreLaDeB.Errores["conflicto"]);
            Assert.True(sobreLaCancelada.Exitoso);
        }
    }
}
