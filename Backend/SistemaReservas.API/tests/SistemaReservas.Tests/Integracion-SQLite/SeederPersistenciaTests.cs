using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Infrastructure.Implementation;
using SistemaReservas.Tests.Infraestructura;

namespace SistemaReservas.Tests.Integracion
{
    // HU10 - AB#117: el inicializador de datos (EfDatabaseSeeder, que la API ejecuta
    // al iniciar mediante EfDatabaseInitializer) no borra, modifica ni duplica las
    // reservas ya guardadas. Cada "inicio" usa un contexto y una conexión nuevos
    // sobre el mismo archivo SQLite, como ocurre al reiniciar la aplicación.
    // Complementa SeederTests (Luis), que lo verifica con una sola conexión en memoria.
    public sealed class SeederPersistenciaTests : IDisposable
    {
        private readonly BaseDatosEnArchivo _baseDatos = new();

        private readonly FakeTimeProvider _reloj =
            new(new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero));

        public void Dispose()
        {
            _baseDatos.Dispose();
        }

        // Simula un inicio de la API: contexto nuevo y el mismo inicializador de Program.cs.
        private async Task IniciarAplicacionAsync()
        {
            await using var contexto = _baseDatos.CrearContexto();
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

            await using var contexto = _baseDatos.CrearContexto();
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
            await using var contexto = _baseDatos.CrearContexto();

            return await contexto.Reservas
                .AsNoTracking()
                .OrderBy(r => r.Id)
                .ToListAsync();
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
            await using var contexto = _baseDatos.CrearContexto();
            Assert.Equal(2, await contexto.Reservas.CountAsync());
            Assert.Equal(2, await contexto.Usuarios.CountAsync());
            Assert.Equal(2, await contexto.Laboratorios.CountAsync());
            Assert.Equal(8, await contexto.Disponibilidades.CountAsync());
        }

        // Si la API se reinicia días después, el seeder agrega horarios para las nuevas
        // fechas, pero las reservas registradas siguen iguales.
        [Fact]
        public async Task InitializeAsync_ReinicioDiasDespues_AgregaHorariosSinTocarReservas()
        {
            // Arrange
            var originales = await PrepararReservasAsync();
            _reloj.Advance(TimeSpan.FromDays(3));

            // Act
            await IniciarAplicacionAsync();

            // Assert
            var despues = await LeerReservasAsync();
            Assert.Equal(
                originales.Select(r => (r.Id, r.Estado, r.Fecha, r.HoraInicio)),
                despues.Select(r => (r.Id, r.Estado, r.Fecha, r.HoraInicio)));

            await using var contexto = _baseDatos.CrearContexto();
            Assert.Equal(16, await contexto.Disponibilidades.CountAsync());
        }
    }
}
