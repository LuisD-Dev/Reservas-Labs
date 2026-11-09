using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Infrastructure.Data;
using SistemaReservas.Infrastructure.Data.Seed;
using SistemaReservas.Infrastructure.Implementation;
using SistemaReservas.Infrastructure.Interfaces;
using SistemaReservas.Tests.Infraestructura;

namespace SistemaReservas.Tests.Infrastructure
{
    public class SeederTests
    {
        private readonly FakeTimeProvider _reloj = new(new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero));
        private readonly IPasswordHashService _hash = new PasswordHashService();

        [Fact]
        public async Task Orquestador_EjecutaUsuariosLaboratoriosYDisponibilidadesEnOrden()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            await using var context = new ContextoConRegistro(options);
            await context.Database.EnsureCreatedAsync();
            var hash = new HashConRegistro(context);

            await EfDatabaseSeeder.SeedAsync(context, hash, _reloj);

            Assert.Equal(new[] { "usuario", "usuario", "usuarios+laboratorios", "disponibilidades" }, context.Eventos);
            Assert.Equal(2, await context.Usuarios.CountAsync());
            Assert.Equal(2, await context.Laboratorios.CountAsync());
            var horarios = await context.Disponibilidades.ToListAsync();
            Assert.Equal(8, horarios.Count);
            Assert.All(horarios, h => Assert.True(h.LaboratorioId > 0));
            var hoy = _reloj.GetLocalNow().Date;
            Assert.All(horarios, h => Assert.Contains(h.Fecha, new[] { hoy.AddDays(1), hoy.AddDays(2) }));
            Assert.Equal(4, horarios.Count(h => h.HoraInicio == TimeSpan.FromHours(8) && h.HoraFin == TimeSpan.FromHours(12) && h.Estado == "Disponible"));
            Assert.Equal(4, horarios.Count(h => h.HoraInicio == TimeSpan.FromHours(13) && h.HoraFin == TimeSpan.FromHours(15) && h.Estado == "No disponible"));
        }

        [Fact]
        public async Task Orquestador_EsIdempotenteYConservaReservasYDatosExistentes()
        {
            using var database = new BaseDatosEnMemoria();
            await using var context = database.CrearContexto();
            await EfDatabaseSeeder.SeedAsync(context, _hash, _reloj);
            var admin = await context.Usuarios.SingleAsync(u => u.Username == "admin");
            var laboratorio = await context.Laboratorios.FirstAsync();
            var hashOriginal = admin.PasswordHash;
            admin.Nombre = "Nombre existente";
            var reserva = new Reserva
            {
                UsuarioId = admin.Id, LaboratorioId = laboratorio.Id, Fecha = _reloj.GetLocalNow().Date.AddDays(1),
                HoraInicio = TimeSpan.FromHours(9), HoraFin = TimeSpan.FromHours(10), Estado = "Activa"
            };
            context.Reservas.Add(reserva);
            await context.SaveChangesAsync();
            var reservaId = reserva.Id;

            await EfDatabaseSeeder.SeedAsync(context, _hash, _reloj);
            context.ChangeTracker.Clear();

            Assert.Equal(2, await context.Usuarios.CountAsync());
            Assert.Equal(2, await context.Laboratorios.CountAsync());
            Assert.Equal(8, await context.Disponibilidades.CountAsync());
            var conservada = await context.Reservas.SingleAsync();
            Assert.Equal(reservaId, conservada.Id);
            Assert.Equal("Activa", conservada.Estado);
            Assert.Equal(TimeSpan.FromHours(9), conservada.HoraInicio);
            Assert.Equal(TimeSpan.FromHours(10), conservada.HoraFin);
            Assert.Equal(admin.Id, conservada.UsuarioId);
            Assert.Equal(laboratorio.Id, conservada.LaboratorioId);
            var adminConservado = await context.Usuarios.SingleAsync(u => u.Username == "admin");
            Assert.Equal(hashOriginal, adminConservado.PasswordHash);
            Assert.Equal("Nombre existente", adminConservado.Nombre);
            Assert.True(_hash.VerifyPassword(adminConservado, "1234"));
        }

        [Fact]
        public async Task Usuarios_ConservaCredencialesRolesYNombresIniciales()
        {
            using var database = new BaseDatosEnMemoria();
            await using var context = database.CrearContexto();
            await EfDatabaseSeeder.SeedAsync(context, _hash, _reloj);
            var admin = await context.Usuarios.SingleAsync(u => u.Username == "admin");
            var usuario = await context.Usuarios.SingleAsync(u => u.Username == "usuario");
            Assert.Equal("Administrador", admin.Nombre);
            Assert.Equal("Administrador", admin.Rol);
            Assert.Equal("Usuario de prueba", usuario.Nombre);
            Assert.Equal("Usuario", usuario.Rol);
            Assert.All(new[] { admin, usuario }, u =>
            {
                Assert.Equal(0, u.IntentosFallidos);
                Assert.NotEqual("1234", u.PasswordHash);
                Assert.True(_hash.VerifyPassword(u, "1234"));
            });
        }

        private sealed class ContextoConRegistro(DbContextOptions<AppDbContext> options) : AppDbContextSqlite(options)
        {
            public List<string> Eventos { get; } = [];

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                var pendientes = ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList();
                if (pendientes.Any(e => e.Entity is Disponibilidad))
                {
                    Assert.DoesNotContain(pendientes, e => e.Entity is Usuario or Laboratorio);
                    Assert.Equal(2, Laboratorios.Count());
                    Eventos.Add("disponibilidades");
                }
                else
                {
                    Assert.Equal(2, pendientes.Count(e => e.Entity is Usuario));
                    Assert.Equal(2, pendientes.Count(e => e.Entity is Laboratorio));
                    Eventos.Add("usuarios+laboratorios");
                }
                return base.SaveChangesAsync(cancellationToken);
            }
        }

        private sealed class HashConRegistro(ContextoConRegistro context) : IPasswordHashService
        {
            private readonly PasswordHashService _real = new();
            public string HashPassword(string password)
            {
                Assert.Empty(context.ChangeTracker.Entries<Laboratorio>());
                Assert.Empty(context.ChangeTracker.Entries<Disponibilidad>());
                context.Eventos.Add("usuario");
                return _real.HashPassword(password);
            }
            public bool VerifyPassword(Usuario usuario, string password) => _real.VerifyPassword(usuario, password);
        }
    }
}
