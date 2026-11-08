using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SistemaReservas.API;

namespace SistemaReservas.Tests.Presentation
{
    public class DependencyInjectionTests
    {
        private const string Clave = "clave-jwt-solo-para-pruebas-presentacion-0123456789";

        private static IConfiguration Configuracion()
        {
            return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = Clave,
                ["Jwt:Issuer"] = "Pruebas.Presentation",
                ["Jwt:Audience"] = "Pruebas.Frontend"
            }).Build();
        }

        [Fact]
        public void AddPresentation_DevuelveColeccionYRegistraTimeProviderSingleton()
        {
            var services = new ServiceCollection();

            Assert.Same(services, services.AddPresentation(Configuracion()));

            var registro = Assert.Single(services, s => s.ServiceType == typeof(TimeProvider));
            Assert.Equal(ServiceLifetime.Singleton, registro.Lifetime);
            using var provider = services.BuildServiceProvider();
            using var primerScope = provider.CreateScope();
            using var segundoScope = provider.CreateScope();
            Assert.Same(TimeProvider.System, primerScope.ServiceProvider.GetRequiredService<TimeProvider>());
            Assert.Same(primerScope.ServiceProvider.GetRequiredService<TimeProvider>(),
                segundoScope.ServiceProvider.GetRequiredService<TimeProvider>());
        }

        [Fact]
        public async Task AddPresentation_ConservaEsquemaBearerYValidacionJwt()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddPresentation(Configuracion());
            using var provider = services.BuildServiceProvider();

            var esquemas = provider.GetRequiredService<IAuthenticationSchemeProvider>();
            Assert.Equal(JwtBearerDefaults.AuthenticationScheme,
                (await esquemas.GetDefaultAuthenticateSchemeAsync())!.Name);
            Assert.Equal(JwtBearerDefaults.AuthenticationScheme,
                (await esquemas.GetDefaultChallengeSchemeAsync())!.Name);
            var parametros = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters;
            Assert.True(parametros.ValidateIssuer);
            Assert.True(parametros.ValidateAudience);
            Assert.True(parametros.ValidateLifetime);
            Assert.True(parametros.ValidateIssuerSigningKey);
            Assert.Equal("Pruebas.Presentation", parametros.ValidIssuer);
            Assert.Equal("Pruebas.Frontend", parametros.ValidAudience);
            Assert.Equal(Encoding.UTF8.GetBytes(Clave), Assert.IsType<SymmetricSecurityKey>(parametros.IssuerSigningKey).Key);
            Assert.Equal(TimeSpan.FromSeconds(30), parametros.ClockSkew);
            Assert.Equal(ClaimTypes.Name, parametros.NameClaimType);
            Assert.Equal(ClaimTypes.Role, parametros.RoleClaimType);
        }

        [Fact]
        public void AddPresentation_ConservaPoliticaCorsDeFrontend()
        {
            var services = new ServiceCollection();
            services.AddPresentation(Configuracion());
            using var provider = services.BuildServiceProvider();

            var politica = provider.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy("PermitirFrontend");

            Assert.NotNull(politica);
            Assert.Equal(new[] { "http://localhost:5173" }, politica.Origins);
            Assert.True(politica.AllowAnyHeader);
            Assert.True(politica.AllowAnyMethod);
            Assert.False(politica.AllowAnyOrigin);
            Assert.False(politica.SupportsCredentials);
        }

        [Fact]
        public void AddPresentation_SinClaveConservaExcepcionDeConfiguracion()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();

            var error = Assert.Throws<InvalidOperationException>(() => services.AddPresentation(configuration));

            Assert.Equal("Falta la configuración Jwt:Key.", error.Message);
        }
    }
}
