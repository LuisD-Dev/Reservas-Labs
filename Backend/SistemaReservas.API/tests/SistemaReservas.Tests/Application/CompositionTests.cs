using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaReservas.Application;
using SistemaReservas.Application.Interfaces;
using SistemaReservas.Infrastructure;
using SistemaReservas.Infrastructure.Data;
using SistemaReservas.Infrastructure.Interfaces;

namespace SistemaReservas.Tests.Application
{
    public class CompositionTests
    {
        [Fact]
        public void Composicion_ResuelveServiciosYMapperConLasVidasUtilesActuales()
        {
            var services = new ServiceCollection();
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "clave-solo-pruebas-composicion-0123456789",
                    ["Jwt:Issuer"] = "Pruebas",
                    ["Jwt:Audience"] = "Pruebas.Frontend"
                }).Build());
            services.AddApplication();
            services.AddInfrastructure("Server=localhost;Database=NoConectar;Trusted_Connection=True;TrustServerCertificate=True");
            using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
            using var first = provider.CreateScope();
            using var second = provider.CreateScope();
            foreach (var contrato in new[] { typeof(IAuthService), typeof(ILaboratorioService), typeof(IDisponibilidadService),
                typeof(IReservaService), typeof(IVerificacionDisponibilidadService), typeof(IMapper), typeof(AppDbContext) })
            {
                var instancia = first.ServiceProvider.GetRequiredService(contrato);
                Assert.Same(instancia, first.ServiceProvider.GetRequiredService(contrato));
                Assert.NotSame(instancia, second.ServiceProvider.GetRequiredService(contrato));
            }
            Assert.Same(first.ServiceProvider.GetRequiredService<TypeAdapterConfig>(), second.ServiceProvider.GetRequiredService<TypeAdapterConfig>());
            Assert.Same(first.ServiceProvider.GetRequiredService<ITokenService>(), second.ServiceProvider.GetRequiredService<ITokenService>());
        }
    }
}
