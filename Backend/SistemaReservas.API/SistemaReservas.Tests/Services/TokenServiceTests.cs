using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.Tokens;
using SistemaReservas.API.Services;

namespace SistemaReservas.Tests.Services
{
    // NFR2 - Pruebas unitarias de TokenService (NFR1 - #43): claims, firma y expiración.
    // Usa una clave solo de prueba (no la de appsettings) y una hora fija con FakeTimeProvider.
    public class TokenServiceTests
    {
        private const string ClavePrueba = "clave-solo-para-pruebas-unitarias-0123456789";
        private const string Issuer = "SistemaReservas.Pruebas";
        private const string Audience = "SistemaReservas.Pruebas.Frontend";
        private static readonly DateTimeOffset Inicio = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

        private readonly FakeTimeProvider _reloj = new(Inicio);

        private TokenService CrearServicio(string? expiraHoras = "2", string? clave = ClavePrueba)
        {
            var configuracion = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = clave,
                    ["Jwt:Issuer"] = Issuer,
                    ["Jwt:Audience"] = Audience,
                    ["Jwt:ExpiraHoras"] = expiraHoras
                })
                .Build();

            return new TokenService(configuracion, _reloj);
        }

        private static TokenValidationParameters ParametrosValidacion(string clave)
        {
            return new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
                // La hora del token es fija (FakeTimeProvider), por eso no se valida la vigencia.
                ValidateLifetime = false
            };
        }

        private static string ValorClaim(JwtSecurityToken jwt, string tipo)
        {
            return jwt.Claims.Single(c => c.Type == tipo).Value;
        }

        [Fact]
        public void GenerarToken_IncluyeLosClaimsDelUsuario()
        {
            var (token, _) = CrearServicio().GenerarToken(7, "Administrador", "Administrador");

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.Equal("7", ValorClaim(jwt, JwtRegisteredClaimNames.Sub));
            Assert.Equal("7", ValorClaim(jwt, ClaimTypes.NameIdentifier));
            Assert.Equal("Administrador", ValorClaim(jwt, ClaimTypes.Name));
            Assert.Equal("Administrador", ValorClaim(jwt, ClaimTypes.Role));
            Assert.False(string.IsNullOrWhiteSpace(ValorClaim(jwt, JwtRegisteredClaimNames.Jti)));
        }

        [Fact]
        public void GenerarToken_UsaIssuerYAudienceDeLaConfiguracion()
        {
            var (token, _) = CrearServicio().GenerarToken(7, "Administrador", "Administrador");

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.Equal(Issuer, jwt.Issuer);
            Assert.Equal(Audience, Assert.Single(jwt.Audiences));
        }

        [Fact]
        public void GenerarToken_ExpiraSegunExpiraHoras()
        {
            var (token, expiraUtc) = CrearServicio(expiraHoras: "2").GenerarToken(7, "Administrador", "Administrador");

            var esperado = Inicio.UtcDateTime.AddHours(2);
            Assert.Equal(esperado, expiraUtc);
            Assert.Equal(DateTimeKind.Utc, expiraUtc.Kind);
            // El claim "exp" del token coincide con la expiración devuelta.
            Assert.Equal(esperado, new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("no-es-numero")]
        public void GenerarToken_SinExpiraHorasValido_ExpiraEnOchoHoras(string? expiraHoras)
        {
            var (_, expiraUtc) = CrearServicio(expiraHoras).GenerarToken(7, "Administrador", "Administrador");

            Assert.Equal(Inicio.UtcDateTime.AddHours(8), expiraUtc);
        }

        [Fact]
        public void GenerarToken_SinClave_LanzaInvalidOperationException()
        {
            var servicio = CrearServicio(clave: null);

            Assert.Throws<InvalidOperationException>(() => servicio.GenerarToken(7, "Administrador", "Administrador"));
        }

        [Fact]
        public void GenerarToken_FirmaValidaConLaMismaClave()
        {
            var (token, _) = CrearServicio().GenerarToken(7, "Administrador", "Administrador");

            var principal = new JwtSecurityTokenHandler()
                .ValidateToken(token, ParametrosValidacion(ClavePrueba), out var tokenValidado);

            Assert.Equal(SecurityAlgorithms.HmacSha256, ((JwtSecurityToken)tokenValidado).Header.Alg);
            Assert.True(principal.IsInRole("Administrador"));
        }

        [Fact]
        public void GenerarToken_ConOtraClave_FirmaNoValida()
        {
            var (token, _) = CrearServicio().GenerarToken(7, "Administrador", "Administrador");
            var otraClave = "otra-clave-distinta-para-pruebas-9876543210";

            Assert.ThrowsAny<SecurityTokenException>(() =>
                new JwtSecurityTokenHandler().ValidateToken(token, ParametrosValidacion(otraClave), out _));
        }

        [Fact]
        public void GenerarToken_CadaTokenTieneJtiDistinto()
        {
            var servicio = CrearServicio();

            var (primero, _) = servicio.GenerarToken(7, "Administrador", "Administrador");
            var (segundo, _) = servicio.GenerarToken(7, "Administrador", "Administrador");

            var handler = new JwtSecurityTokenHandler();
            Assert.NotEqual(handler.ReadJwtToken(primero).Id, handler.ReadJwtToken(segundo).Id);
        }
    }
}
