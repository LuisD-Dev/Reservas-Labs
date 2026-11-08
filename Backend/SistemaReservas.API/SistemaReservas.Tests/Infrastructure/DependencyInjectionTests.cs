using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "clave-de-prueba-composicion-0123456789",
                    ["Jwt:Issuer"] = "Pruebas",
                    ["Jwt:Audience"] = "Pruebas.Frontend"
                }).Build());
            services.AddSingleton(TimeProvider.System);
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

            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
            Assert.IsType<JwtTokenService>(tokenService);
            Assert.Same(tokenService, segundoScope.ServiceProvider.GetRequiredService<ITokenService>());
            var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
            Assert.IsType<EfDatabaseInitializer>(initializer);
            Assert.Same(initializer, scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>());
            Assert.NotSame(initializer, segundoScope.ServiceProvider.GetRequiredService<IDatabaseInitializer>());
            var healthCheck = scope.ServiceProvider.GetRequiredService<IDatabaseHealthCheck>();
            Assert.IsType<EfDatabaseHealthCheck>(healthCheck);
            Assert.Same(healthCheck, scope.ServiceProvider.GetRequiredService<IDatabaseHealthCheck>());
            Assert.NotSame(healthCheck, segundoScope.ServiceProvider.GetRequiredService<IDatabaseHealthCheck>());
            Assert.Equal(ServiceLifetime.Singleton,
                Assert.Single(services, s => s.ServiceType == typeof(ITokenService)).Lifetime);
            Assert.Equal(ServiceLifetime.Scoped,
                Assert.Single(services, s => s.ServiceType == typeof(IDatabaseInitializer)).Lifetime);
            Assert.Equal(ServiceLifetime.Scoped,
                Assert.Single(services, s => s.ServiceType == typeof(IDatabaseHealthCheck)).Lifetime);
        }
    }
}
