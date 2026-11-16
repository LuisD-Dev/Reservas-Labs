using Microsoft.EntityFrameworkCore;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Infrastructure.Implementation;
using SistemaReservas.IntegrationTests.Contenedores;

namespace SistemaReservas.IntegrationTests
{
    // HU10 - AB#116: persistencia de reservas en SQL Server real (Docker).
    // Cada prueba guarda con un contexto, lo cierra y recupera los datos con otro
    // contexto y otra conexión, como ocurre al reiniciar la aplicación.
    public sealed class ReservaRepositoryIntegrationTest :
        IClassFixture<SqlServerContenedor>, IAsyncLifetime
    {
        private readonly SqlServerContenedor _sqlServer;

        private int _usuarioId;
        private int _laboratorioId;
        private int _otroLaboratorioId;

        public ReservaRepositoryIntegrationTest(SqlServerContenedor sqlServer)
        {
            _sqlServer = sqlServer;
        }

        // Antes de cada prueba: tablas vacías y datos base (un usuario y dos laboratorios).
        public async Task InitializeAsync()
        {
            await _sqlServer.LimpiarAsync();

            await using var contexto = _sqlServer.CrearContexto();

            var usuario = new Usuario
            {
                Nombre = "Usuario de prueba",
                Username = "usuario",
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

            var otroLaboratorio = new Laboratorio
            {
                Nombre = "Laboratorio de Redes",
                Ubicacion = "Edificio B",
                Capacidad = 25,
                Estado = "Habilitado"
            };

            contexto.Usuarios.Add(usuario);
            contexto.Laboratorios.AddRange(laboratorio, otroLaboratorio);
            await contexto.SaveChangesAsync();

            _usuarioId = usuario.Id;
            _laboratorioId = laboratorio.Id;
            _otroLaboratorioId = otroLaboratorio.Id;
        }

        public Task DisposeAsync() => Task.CompletedTask;

        private Reserva NuevaReserva(
            int laboratorioId,
            DateTime fecha,
            int horaInicio,
            int horaFin,
            string estado = "Activa")
        {
            return new Reserva
            {
                LaboratorioId = laboratorioId,
                UsuarioId = _usuarioId,
                Fecha = fecha,
                HoraInicio = new TimeSpan(horaInicio, 0, 0),
                HoraFin = new TimeSpan(horaFin, 0, 0),
                Estado = estado
            };
        }

        // Guarda con un contexto y devuelve el id; el contexto y su conexión se cierran al salir.
        private async Task<int> GuardarAsync(Reserva reserva)
        {
            await using var contexto = _sqlServer.CrearContexto();
            var repositorio = new ReservaRepository(contexto);

            var guardada = await repositorio.CrearAsync(reserva);

            return guardada.Id;
        }

        [Fact]
        public async Task CrearAsync_ContextoCerrado_OtroContextoRecuperaTodosLosDatos()
        {
            // Arrange
            var reservaId = await GuardarAsync(
                NuevaReserva(_laboratorioId, new DateTime(2026, 11, 12), 9, 11));

            // Act
            await using var contextoNuevo = _sqlServer.CrearContexto();
            var reservas = await new ReservaRepository(contextoNuevo)
                .ObtenerPorUsuarioAsync(_usuarioId);

            // Assert
            var reserva = Assert.Single(reservas);
            Assert.Equal(reservaId, reserva.Id);
            Assert.Equal(_usuarioId, reserva.UsuarioId);
            Assert.Equal(_laboratorioId, reserva.LaboratorioId);
            Assert.Equal("Laboratorio de Software", reserva.Laboratorio.Nombre);
            Assert.Equal(new DateTime(2026, 11, 12), reserva.Fecha);
            Assert.Equal(new TimeSpan(9, 0, 0), reserva.HoraInicio);
            Assert.Equal(new TimeSpan(11, 0, 0), reserva.HoraFin);
            Assert.Equal("Activa", reserva.Estado);
        }

        [Fact]
        public async Task GuardarCambiosAsync_ReservaCancelada_ConservaElEstadoTrasReconectar()
        {
            // Arrange
            var reservaId = await GuardarAsync(
                NuevaReserva(_laboratorioId, new DateTime(2026, 11, 12), 9, 11));

            await using (var contextoCambio = _sqlServer.CrearContexto())
            {
                var repositorio = new ReservaRepository(contextoCambio);
                var reserva = await repositorio.ObtenerPorIdAsync(reservaId);
                reserva!.Estado = "Cancelada";
                await repositorio.GuardarCambiosAsync();
            }

            // Act
            await using var contextoNuevo = _sqlServer.CrearContexto();
            var recuperada = await new ReservaRepository(contextoNuevo)
                .ObtenerPorIdAsync(reservaId);

            // Assert
            Assert.NotNull(recuperada);
            Assert.Equal("Cancelada", recuperada.Estado);
        }

        // HU6: la consulta de conflictos filtra por laboratorio, fecha y estado en SQL Server.
        [Fact]
        public async Task ObtenerActivasPorLaboratorioYFechaAsync_VariasReservas_SoloDevuelveLasActivasDelLaboratorioYFecha()
        {
            // Arrange
            var fecha = new DateTime(2026, 11, 12);
            var esperadaId = await GuardarAsync(NuevaReserva(_laboratorioId, fecha, 9, 11));
            await GuardarAsync(NuevaReserva(_laboratorioId, fecha, 13, 14, "Cancelada"));
            await GuardarAsync(NuevaReserva(_otroLaboratorioId, fecha, 9, 11));
            await GuardarAsync(NuevaReserva(_laboratorioId, fecha.AddDays(1), 9, 11));

            // Act
            await using var contextoNuevo = _sqlServer.CrearContexto();
            var activas = await new ReservaRepository(contextoNuevo)
                .ObtenerActivasPorLaboratorioYFechaAsync(_laboratorioId, fecha);

            // Assert
            var activa = Assert.Single(activas);
            Assert.Equal(esperadaId, activa.Id);
        }

        // Documenta el hallazgo del Sprint 3: con un laboratorio inexistente la llave
        // foránea de SQL Server rechaza la reserva y la excepción llega a la API (500).
        [Fact]
        public async Task CrearAsync_LaboratorioInexistente_LanzaDbUpdateException()
        {
            // Arrange
            var reserva = NuevaReserva(999999, new DateTime(2026, 11, 12), 9, 11);

            // Act + Assert
            await Assert.ThrowsAsync<DbUpdateException>(() => GuardarAsync(reserva));
        }

        // La restricción CK_Reservas_Estado solo permite 'Activa' y 'Cancelada'.
        [Fact]
        public async Task CrearAsync_EstadoInvalido_LanzaDbUpdateException()
        {
            // Arrange
            var reserva = NuevaReserva(_laboratorioId, new DateTime(2026, 11, 12), 9, 11, "Pendiente");

            // Act + Assert
            await Assert.ThrowsAsync<DbUpdateException>(() => GuardarAsync(reserva));
        }
    }
}
