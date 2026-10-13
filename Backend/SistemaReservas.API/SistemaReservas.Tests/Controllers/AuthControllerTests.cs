using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.Tokens;
using SistemaReservas.API.Controllers;
using SistemaReservas.API.DTOs;
using SistemaReservas.API.Models;
using SistemaReservas.API.Services;
using SistemaReservas.Tests.Fakes;

namespace SistemaReservas.Tests.Controllers
{
    // Taller V - Pruebas unitarias de AuthController (HU1 - #22): códigos HTTP y JSON
    // de POST /api/auth/login. AuthService y TokenService son clases concretas, así que
    // se usan instancias reales con dependencias controladas: FakeUsuarioRepository,
    // configuración JWT en memoria (clave solo de prueba) y FakeTimeProvider.
    // Patrón AAA: el Arrange común (usuario "usuario", servicios y reloj en Inicio)
    // se prepara en el constructor; en el [Theory] los datos vienen de [InlineData].
    // El 400 por modelo inválido (falta un campo o el cuerpo) lo devuelve [ApiController]
    // antes de llegar a Login, por eso queda fuera de esta prueba unitaria.
    public class AuthControllerTests
    {
        private const string Username = "usuario";
        private const string PasswordCorrecta = "1234";
        private const string PasswordIncorrecta = "incorrecta";
        private const string MensajeCredenciales = "Usuario o contraseña incorrectos.";
        private const string MensajeBloqueo = "Demasiados intentos fallidos. Intente de nuevo en unos minutos.";
        private const string ClavePrueba = "clave-solo-para-pruebas-unitarias-0123456789";
        private static readonly DateTimeOffset Inicio = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

        private readonly FakeUsuarioRepository _repositorio = new();
        private readonly FakeTimeProvider _reloj = new(Inicio);
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            var configuracion = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = ClavePrueba,
                    ["Jwt:Issuer"] = "SistemaReservas.Pruebas",
                    ["Jwt:Audience"] = "SistemaReservas.Pruebas.Frontend",
                    ["Jwt:ExpiraHoras"] = "2"
                })
                .Build();

            var authService = new AuthService(_repositorio, _reloj);
            var tokenService = new TokenService(configuracion, _reloj);
            _controller = new AuthController(authService, tokenService, _reloj);

            _repositorio.Agregar(new Usuario
            {
                Id = 2,
                Nombre = "Usuario de prueba",
                Username = Username,
                PasswordHash = authService.HashPassword(PasswordCorrecta),
                Rol = "Usuario"
            });
        }

        private Task<IActionResult> LoginAsync(string password, string username = Username)
        {
            return _controller.Login(new LoginRequest(username, password));
        }

        // Convierte el cuerpo de la respuesta a JSON para revisar los nombres exactos de los campos.
        private static JsonElement Cuerpo(IActionResult respuesta)
        {
            var objeto = Assert.IsAssignableFrom<ObjectResult>(respuesta);
            return JsonDocument.Parse(JsonSerializer.Serialize(objeto.Value)).RootElement;
        }

        [Fact]
        public async Task Login_CredencialesCorrectas_Devuelve200ConToken()
        {
            // Act
            var respuesta = await LoginAsync(PasswordCorrecta);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(respuesta);
            Assert.Equal(200, ok.StatusCode);
            var cuerpo = Cuerpo(respuesta);
            Assert.Equal(2, cuerpo.GetProperty("usuarioId").GetInt32());
            Assert.Equal("Usuario de prueba", cuerpo.GetProperty("nombre").GetString());
            Assert.Equal("Usuario", cuerpo.GetProperty("rol").GetString());
            Assert.Equal(Inicio.UtcDateTime.AddHours(2), cuerpo.GetProperty("expiraUtc").GetDateTime());
            // El token está firmado con la clave configurada (la vigencia no se valida: el reloj es fijo).
            var token = cuerpo.GetProperty("token").GetString();
            Assert.False(string.IsNullOrWhiteSpace(token));
            new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ClavePrueba))
            }, out _);
        }

        [Fact]
        public async Task Login_PasswordIncorrecta_Devuelve401ConMensajeGenerico()
        {
            // Act
            var respuesta = await LoginAsync(PasswordIncorrecta);

            // Assert
            var noAutorizado = Assert.IsType<UnauthorizedObjectResult>(respuesta);
            Assert.Equal(401, noAutorizado.StatusCode);
            Assert.Equal(MensajeCredenciales, Cuerpo(respuesta).GetProperty("mensaje").GetString());
        }

        [Fact]
        public async Task Login_UsuarioInexistente_Devuelve401ConMensajeGenerico()
        {
            // Act
            var respuesta = await LoginAsync(PasswordCorrecta, "no-existe");

            // Assert
            // Mismo mensaje que con la contraseña incorrecta: no revela cuál dato falló.
            var noAutorizado = Assert.IsType<UnauthorizedObjectResult>(respuesta);
            Assert.Equal(401, noAutorizado.StatusCode);
            Assert.Equal(MensajeCredenciales, Cuerpo(respuesta).GetProperty("mensaje").GetString());
        }

        [Fact]
        public async Task Login_CuentaBloqueada_Devuelve423ConSegundosRestantes()
        {
            // Arrange
            for (var i = 0; i < 4; i++)
            {
                await LoginAsync(PasswordIncorrecta);
            }
            var bloqueadoHasta = Inicio.UtcDateTime.AddMinutes(5);

            // Act
            var quintoFallo = await LoginAsync(PasswordIncorrecta);
            _reloj.Advance(TimeSpan.FromSeconds(60));
            var aLos60Segundos = await LoginAsync(PasswordCorrecta);

            // Assert
            // 5.º intento fallido: ya responde 423 con los 5 minutos completos.
            var bloqueo = Assert.IsType<ObjectResult>(quintoFallo);
            Assert.Equal(423, bloqueo.StatusCode);
            var cuerpo = Cuerpo(quintoFallo);
            Assert.Equal(MensajeBloqueo, cuerpo.GetProperty("mensaje").GetString());
            Assert.Equal(bloqueadoHasta, cuerpo.GetProperty("bloqueadoHasta").GetDateTime());
            Assert.Equal(300, cuerpo.GetProperty("segundosRestantes").GetInt32());

            // A los 60 s sigue bloqueado aun con la contraseña correcta.
            bloqueo = Assert.IsType<ObjectResult>(aLos60Segundos);
            Assert.Equal(423, bloqueo.StatusCode);
            cuerpo = Cuerpo(aLos60Segundos);
            Assert.Equal(MensajeBloqueo, cuerpo.GetProperty("mensaje").GetString());
            Assert.Equal(bloqueadoHasta, cuerpo.GetProperty("bloqueadoHasta").GetDateTime());
            Assert.Equal(DateTimeKind.Utc, cuerpo.GetProperty("bloqueadoHasta").GetDateTime().Kind);
            Assert.Equal(240, cuerpo.GetProperty("segundosRestantes").GetInt32());
        }

        [Fact]
        public async Task Login_BloqueoVencido_Devuelve200()
        {
            // Arrange
            for (var i = 0; i < 5; i++)
            {
                await LoginAsync(PasswordIncorrecta);
            }
            _reloj.Advance(TimeSpan.FromMinutes(5));

            // Act
            var respuesta = await LoginAsync(PasswordCorrecta);

            // Assert
            Assert.IsType<OkObjectResult>(respuesta);
            Assert.Equal(2, Cuerpo(respuesta).GetProperty("usuarioId").GetInt32());
        }

        [Theory]
        [InlineData("", PasswordCorrecta)]
        [InlineData("   ", PasswordCorrecta)]
        [InlineData(Username, "")]
        public async Task Login_DatosVacios_Devuelve401ConMensajeGenerico(string username, string password)
        {
            // Act
            var respuesta = await LoginAsync(password, username);

            // Assert
            // Con campos vacíos (pero presentes) el contrato actual responde 401, no 400.
            var noAutorizado = Assert.IsType<UnauthorizedObjectResult>(respuesta);
            Assert.Equal(401, noAutorizado.StatusCode);
            Assert.Equal(MensajeCredenciales, Cuerpo(respuesta).GetProperty("mensaje").GetString());
            Assert.Empty(_repositorio.Llamadas);
        }
    }
}
