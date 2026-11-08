using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaReservas.Application.Abstractions.Persistence;
using SistemaReservas.Application.Abstractions.Security;
using SistemaReservas.Infrastructure;
using SistemaReservas.Infrastructure.Data;
using SistemaReservas.Infrastructure.Repositories;
using SistemaReservas.Infrastructure.Security;

namespace SistemaReservas.Tests.Infrastructure
{
    public class DependencyInjectionTests
    {
        [Fact]
        public void AddInfrastructure_RegistraPersistenciaYHashingConScopeSinConexion()
        {
            var services = new ServiceCollection();
            var resultado = services.AddInfrastructure(
                "Server=localhost;Database=ReservasComposicionTests;Trusted_Connection=True;TrustServerCertificate=True;");
            Assert.Same(services, resultado);

            using var provider = services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateScopes = true,
                    ValidateOnBuild = true
                });
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SistemaReservasDbContext>();

            Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
            Assert.Equal(
                new[]
                {
                    "20261005163907_InitialCreate",
                    "20261017025514_AgregarReservasSprint3"
                },
                context.Database.GetMigrations());

            var usuarioRepository = scope.ServiceProvider.GetRequiredService<IUsuarioRepository>();
            Assert.IsType<UsuarioRepository>(usuarioRepository);
            Assert.IsType<LaboratorioRepository>(
                scope.ServiceProvider.GetRequiredService<ILaboratorioRepository>());
            Assert.IsType<DisponibilidadRepository>(
                scope.ServiceProvider.GetRequiredService<IDisponibilidadRepository>());
            Assert.IsType<ReservaRepository>(
                scope.ServiceProvider.GetRequiredService<IReservaRepository>());
            Assert.IsType<PasswordHashService>(
                scope.ServiceProvider.GetRequiredService<IPasswordHashService>());
            Assert.Same(
                usuarioRepository,
                scope.ServiceProvider.GetRequiredService<IUsuarioRepository>());

            using var segundoScope = provider.CreateScope();
            Assert.NotSame(
                usuarioRepository,
                segundoScope.ServiceProvider.GetRequiredService<IUsuarioRepository>());
        }
    }
}
