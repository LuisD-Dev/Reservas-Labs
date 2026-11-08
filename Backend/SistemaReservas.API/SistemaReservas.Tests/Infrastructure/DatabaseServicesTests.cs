using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Infrastructure.Data;
using SistemaReservas.Infrastructure.Security;
using SistemaReservas.Tests.Infraestructura;

namespace SistemaReservas.Tests.Infrastructure
{
    public class DatabaseServicesTests
    {
        private static readonly DateTimeOffset Inicio = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task InitializeAsync_EjecutaSeederConUsuariosLaboratoriosYHorarios()
        {
            using var db = new BaseDatosEnMemoria();
            using var context = db.CrearContexto();
            var hashing = new PasswordHashService();
            var reloj = new FakeTimeProvider(Inicio);
            var initializer = new EfDatabaseInitializer(context, hashing, reloj);

            await initializer.InitializeAsync();

            var usuarios = await context.Usuarios.OrderBy(u => u.Username).ToListAsync();
            Assert.Equal(new[] { "admin", "usuario" }, usuarios.Select(u => u.Username));
            Assert.Equal(new[] { "Administrador", "Usuario" }, usuarios.Select(u => u.Rol));
            Assert.All(usuarios, usuario => Assert.True(hashing.VerifyPassword(usuario, "1234")));
            Assert.Equal(2, await context.Laboratorios.CountAsync());
            Assert.Equal(8, await context.Disponibilidades.CountAsync());
            Assert.All(await context.Disponibilidades.ToListAsync(), horario =>
                Assert.Contains(horario.Fecha.Date, new[]
                {
                    reloj.GetLocalNow().Date.AddDays(1), reloj.GetLocalNow().Date.AddDays(2)
                }));
            await initializer.InitializeAsync();
            Assert.Equal(2, await context.Usuarios.CountAsync());
            Assert.Equal(8, await context.Disponibilidades.CountAsync());
        }

        [Fact]
        public async Task InitializeAsync_ConservaReservaExistente()
        {
            using var db = new BaseDatosEnMemoria();
            using var context = db.CrearContexto();
            var usuario = new Usuario { Nombre = "Existente", Username = "existente", PasswordHash = "hash", Rol = "Usuario" };
            var laboratorio = new Laboratorio { Nombre = "Existente", Ubicacion = "A", Capacidad = 20, Estado = "Habilitado" };
            context.Usuarios.Add(usuario);
            context.Laboratorios.Add(laboratorio);
            await context.SaveChangesAsync();
            var reserva = new Reserva
            {
                UsuarioId = usuario.Id, LaboratorioId = laboratorio.Id, Fecha = Inicio.Date.AddDays(3),
                HoraInicio = TimeSpan.FromHours(9), HoraFin = TimeSpan.FromHours(10), Estado = "Activa"
            };
            context.Reservas.Add(reserva);
            await context.SaveChangesAsync();

            await new EfDatabaseInitializer(context, new PasswordHashService(), new FakeTimeProvider(Inicio)).InitializeAsync();

            using var consulta = db.CrearContexto();
            var conservada = Assert.Single(await consulta.Reservas.ToListAsync());
            Assert.Equal(reserva.Id, conservada.Id);
            Assert.Equal(usuario.Id, conservada.UsuarioId);
            Assert.Equal(laboratorio.Id, conservada.LaboratorioId);
            Assert.Equal(reserva.Fecha, conservada.Fecha);
            Assert.Equal(reserva.HoraInicio, conservada.HoraInicio);
            Assert.Equal(reserva.HoraFin, conservada.HoraFin);
            Assert.Equal("Activa", conservada.Estado);
        }

        [Fact]
        public async Task CanConnectAsync_ConSqliteAbierto_DevuelveTrue()
        {
            using var db = new BaseDatosEnMemoria();
            using var context = db.CrearContexto();

            Assert.True(await new EfDatabaseHealthCheck(context).CanConnectAsync());
        }
    }
}
