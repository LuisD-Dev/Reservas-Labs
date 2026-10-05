using Microsoft.Extensions.Time.Testing;
using SistemaReservas.API.DTOs;
using SistemaReservas.API.Models;
using SistemaReservas.API.Services;
using SistemaReservas.Tests.Fakes;

namespace SistemaReservas.Tests.Services
{
    // NFR2 - Pruebas unitarias de AuthService (HU1) sin base de datos:
    // el repositorio es un doble en memoria y la hora la controla FakeTimeProvider.
    public class AuthServiceTests
    {
        private const string Username = "usuario";
        private const string PasswordCorrecta = "1234";
        private const string PasswordIncorrecta = "incorrecta";
        private static readonly DateTimeOffset Inicio = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

        private readonly FakeUsuarioRepository _repositorio = new();
        private readonly FakeTimeProvider _reloj = new(Inicio);
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _authService = new AuthService(_repositorio, _reloj);

            _repositorio.Agregar(new Usuario
            {
                Id = 2,
                Nombre = "Usuario de prueba",
                Username = Username,
                PasswordHash = _authService.HashPassword(PasswordCorrecta),
                Rol = "Usuario"
            });
        }

        private Task<LoginResult> LoginAsync(string password, string username = Username)
        {
            return _authService.LoginAsync(new LoginRequest(username, password));
        }

        private async Task BloquearUsuarioAsync()
        {
            for (var i = 0; i < 5; i++)
            {
                await LoginAsync(PasswordIncorrecta);
            }
        }

        [Theory]
        [InlineData("", PasswordCorrecta)]
        [InlineData("   ", PasswordCorrecta)]
        [InlineData(Username, "")]
        public async Task LoginAsync_DatosVacios_CredencialesInvalidasSinConsultarRepositorio(
            string username,
            string password)
        {
            var resultado = await LoginAsync(password, username);

            Assert.Equal(LoginEstado.CredencialesInvalidas, resultado.Estado);
            Assert.Empty(_repositorio.Llamadas);
        }

        [Fact]
        public async Task LoginAsync_UsuarioInexistente_CredencialesInvalidas()
        {
            var resultado = await LoginAsync(PasswordCorrecta, "no-existe");

            Assert.Equal(LoginEstado.CredencialesInvalidas, resultado.Estado);
            Assert.Equal(["ObtenerPorUsernameAsync"], _repositorio.Llamadas);
        }

        [Fact]
        public async Task LoginAsync_PasswordCorrecta_ExitosoConDatosDelUsuario()
        {
            var resultado = await LoginAsync(PasswordCorrecta);

            Assert.Equal(LoginEstado.Exitoso, resultado.Estado);
            Assert.Equal(2, resultado.UsuarioId);
            Assert.Equal("Usuario de prueba", resultado.Nombre);
            Assert.Equal("Usuario", resultado.Rol);
            // Sin intentos previos ni bloqueo no hace falta escribir en la base.
            Assert.Equal(["ObtenerPorUsernameAsync"], _repositorio.Llamadas);
        }

        [Fact]
        public async Task LoginAsync_PasswordIncorrecta_SumaIntentoYCredencialesInvalidas()
        {
            var resultado = await LoginAsync(PasswordIncorrecta);

            Assert.Equal(LoginEstado.CredencialesInvalidas, resultado.Estado);
            Assert.Equal(1, _repositorio.Obtener(Username).IntentosFallidos);
            Assert.Equal(["ObtenerPorUsernameAsync", "RegistrarIntentoFallidoAsync"], _repositorio.Llamadas);
        }

        [Fact]
        public async Task LoginAsync_CuatroFallos_TodaviaNoBloquea()
        {
            LoginResult resultado = null!;
            for (var i = 0; i < 4; i++)
            {
                resultado = await LoginAsync(PasswordIncorrecta);
            }

            Assert.Equal(LoginEstado.CredencialesInvalidas, resultado.Estado);
            Assert.Equal(4, _repositorio.Obtener(Username).IntentosFallidos);
            Assert.Null(_repositorio.Obtener(Username).BloqueadoHasta);
        }

        [Fact]
        public async Task LoginAsync_QuintoFallo_BloqueaCincoMinutos()
        {
            for (var i = 0; i < 4; i++)
            {
                await LoginAsync(PasswordIncorrecta);
            }
            _repositorio.Llamadas.Clear();

            var resultado = await LoginAsync(PasswordIncorrecta);

            var esperado = Inicio.UtcDateTime.AddMinutes(5);
            Assert.Equal(LoginEstado.Bloqueado, resultado.Estado);
            Assert.Equal(esperado, resultado.BloqueadoHasta);
            Assert.Equal(esperado, _repositorio.Obtener(Username).BloqueadoHasta);
            // Al bloquear se reinicia el contador para que tenga otros 5 intentos al vencer.
            Assert.Equal(0, _repositorio.Obtener(Username).IntentosFallidos);
            Assert.Equal(
                ["ObtenerPorUsernameAsync", "RegistrarIntentoFallidoAsync", "BloquearAsync"],
                _repositorio.Llamadas);
        }

        [Fact]
        public async Task LoginAsync_MientrasEstaBloqueado_RechazaAunConPasswordCorrecta()
        {
            await BloquearUsuarioAsync();
            _reloj.Advance(TimeSpan.FromMinutes(4) + TimeSpan.FromSeconds(59));
            _repositorio.Llamadas.Clear();

            var resultado = await LoginAsync(PasswordCorrecta);

            Assert.Equal(LoginEstado.Bloqueado, resultado.Estado);
            Assert.Equal(Inicio.UtcDateTime.AddMinutes(5), resultado.BloqueadoHasta);
            // Bloqueado: se rechaza sin revisar la contraseña ni sumar intentos.
            Assert.Equal(["ObtenerPorUsernameAsync"], _repositorio.Llamadas);
        }

        [Fact]
        public async Task LoginAsync_AlVencerElBloqueo_PermiteEntrarYLimpiaElBloqueo()
        {
            await BloquearUsuarioAsync();
            _reloj.Advance(TimeSpan.FromMinutes(5));
            _repositorio.Llamadas.Clear();

            var resultado = await LoginAsync(PasswordCorrecta);

            Assert.Equal(LoginEstado.Exitoso, resultado.Estado);
            Assert.Null(_repositorio.Obtener(Username).BloqueadoHasta);
            Assert.Equal(["ObtenerPorUsernameAsync", "ReiniciarIntentosAsync"], _repositorio.Llamadas);
        }

        [Fact]
        public async Task LoginAsync_ExitoTrasFallos_ReiniciaElContador()
        {
            await LoginAsync(PasswordIncorrecta);
            await LoginAsync(PasswordIncorrecta);
            _repositorio.Llamadas.Clear();

            var resultado = await LoginAsync(PasswordCorrecta);

            Assert.Equal(LoginEstado.Exitoso, resultado.Estado);
            Assert.Equal(0, _repositorio.Obtener(Username).IntentosFallidos);
            Assert.Equal(["ObtenerPorUsernameAsync", "ReiniciarIntentosAsync"], _repositorio.Llamadas);
        }

        [Fact]
        public void HashPassword_NoGuardaLaPasswordEnTextoPlano()
        {
            var hash = _authService.HashPassword(PasswordCorrecta);

            Assert.False(string.IsNullOrWhiteSpace(hash));
            Assert.DoesNotContain(PasswordCorrecta, hash);
            // Cada hash lleva su propia sal.
            Assert.NotEqual(hash, _authService.HashPassword(PasswordCorrecta));
        }
    }
}
