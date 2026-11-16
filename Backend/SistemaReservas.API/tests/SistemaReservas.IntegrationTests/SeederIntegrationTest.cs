using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Infrastructure.Implementation;
using SistemaReservas.IntegrationTests.Contenedores;

namespace SistemaReservas.IntegrationTests
{
    // HU10 - AB#117: el inicializador de datos que la API ejecuta al iniciar
    // (EfDatabaseInitializer) no borra, modifica ni duplica los datos guardados
    // en SQL Server real (Docker). Cada "inicio" usa un contexto y una conexión nuevos.
    public sealed class SeederIntegrationTest :
        IClassFixture<SqlServerContenedor>, IAsyncLifetime
    {
        private readonly SqlServerContenedor _sqlServer;

        private readonly FakeTimeProvider _reloj =
            new(new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero));

        public SeederIntegrationTest(SqlServerContenedor sqlServer)
        {
            _sqlServer = sqlServer;
        }

        // Antes de cada prueba: tablas vacías, como una base recién creada.
        public Task InitializeAsync() => _sqlServer.LimpiarAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        // Simula un inicio de la API: contexto nuevo y el mismo inicializador de Program.cs.
        private async Task IniciarAplicacionAsync()
        {
            await using var contexto = _sqlServer.CrearContexto();
            var inicializador = new EfDatabaseInitializer(
                contexto,
                new PasswordHashService(),
                _reloj);

            await inicializador.InitializeAsync();
        }

        // Primer inicio y registro de dos reservas (una activa y una cancelada).
        private async Task<List<Reserva>> PrepararReservasAsync()
        {
            await IniciarAplicacionAsync();

            await using var contexto = _sqlServer.CrearContexto();
            var usuario = await contexto.Usuarios.SingleAsync(u => u.Username == "usuario");
            var laboratorios = await contexto.Laboratorios.OrderBy(l => l.Id).ToListAsync();

            var reservas = new List<Reserva>
            {
                new()
                {
                    UsuarioId = usuario.Id,
                    LaboratorioId = laboratorios[0].Id,
                    Fecha = new DateTime(2026, 11, 11),
                    HoraInicio = new TimeSpan(9, 0, 0),
                    HoraFin = new TimeSpan(11, 0, 0),
                    Estado = "Activa"
                },
                new()
                {
                    UsuarioId = usuario.Id,
                    LaboratorioId = laboratorios[1].Id,
                    Fecha = new DateTime(2026, 11, 12),
                    HoraInicio = new TimeSpan(13, 0, 0),
                    HoraFin = new TimeSpan(14, 0, 0),
                    Estado = "Cancelada"
                }
            };

            contexto.Reservas.AddRange(reservas);
            await contexto.SaveChangesAsync();

            return reservas;
        }

        private async Task<List<Reserva>> LeerReservasAsync()
        {
            await using var contexto = _sqlServer.CrearContexto();

            return await contexto.Reservas
                .AsNoTracking()
                .OrderBy(r => r.Id)
                .ToListAsync();
        }

        // Las migraciones del proyecto se aplicaron completas sobre SQL Server.
        [Fact]
        public async Task MigrateAsync_BaseNueva_AplicaTodasLasMigraciones()
        {
            // Arrange
            await using var contexto = _sqlServer.CrearContexto();

            // Act
            var aplicadas = await contexto.Database.GetAppliedMigrationsAsync();
            var pendientes = await contexto.Database.GetPendingMigrationsAsync();

            // Assert
            Assert.Equal(contexto.Database.GetMigrations(), aplicadas);
            Assert.Empty(pendientes);
        }

        [Fact]
        public async Task InitializeAsync_BaseVacia_CreaUsuariosLaboratoriosYHorarios()
        {
            // Act
            await IniciarAplicacionAsync();

            // Assert
            await using var contexto = _sqlServer.CrearContexto();
            Assert.Equal(2, await contexto.Usuarios.CountAsync());
            Assert.Equal(2, await contexto.Laboratorios.CountAsync());
            Assert.Equal(8, await contexto.Disponibilidades.CountAsync());
            Assert.Equal(0, await contexto.Reservas.CountAsync());
        }

        [Fact]
        public async Task InitializeAsync_ReinicioConReservas_NoBorraNiModificaLasReservas()
        {
            // Arrange
            var originales = await PrepararReservasAsync();

            // Act
            await IniciarAplicacionAsync();

            // Assert
            var despues = await LeerReservasAsync();
            Assert.Equal(originales.Count, despues.Count);

            foreach (var (original, actual) in originales.Zip(despues))
            {
                Assert.Equal(original.Id, actual.Id);
                Assert.Equal(original.UsuarioId, actual.UsuarioId);
                Assert.Equal(original.LaboratorioId, actual.LaboratorioId);
                Assert.Equal(original.Fecha, actual.Fecha);
                Assert.Equal(original.HoraInicio, actual.HoraInicio);
                Assert.Equal(original.HoraFin, actual.HoraFin);
                Assert.Equal(original.Estado, actual.Estado);
            }
        }

        [Fact]
        public async Task InitializeAsync_VariosReinicios_NoDuplicaReservasNiDatosIniciales()
        {
            // Arrange
            await PrepararReservasAsync();

            // Act
            await IniciarAplicacionAsync();
            await IniciarAplicacionAsync();
            await IniciarAplicacionAsync();

            // Assert
            await using var contexto = _sqlServer.CrearContexto();
            Assert.Equal(2, await contexto.Reservas.CountAsync());
            Assert.Equal(2, await contexto.Usuarios.CountAsync());
            Assert.Equal(2, await contexto.Laboratorios.CountAsync());
            Assert.Equal(8, await contexto.Disponibilidades.CountAsync());
        }
    }
}
