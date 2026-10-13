using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SistemaReservas.API.DTOs;
using SistemaReservas.API.Models;
using SistemaReservas.API.Repositories;
using SistemaReservas.API.Services;
using SistemaReservas.Tests.Fakes;

namespace SistemaReservas.Tests.Services
{
    // NFR2 - Pruebas unitarias de AuthService (HU1) sin base de datos:
    // el repositorio es un doble en memoria y la hora la controla FakeTimeProvider.
    // Patrón AAA: el Arrange común (usuario "usuario" con su contraseña y el reloj
    // en Inicio) se prepara en el constructor; cada prueba agrega lo que necesita.
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
            // Act
            var resultado = await LoginAsync(password, username);

            // Assert
            Assert.Equal(LoginEstado.CredencialesInvalidas, resultado.Estado);
            Assert.Empty(_repositorio.Llamadas);
        }

        [Fact]
        public async Task LoginAsync_UsuarioInexistente_CredencialesInvalidas()
        {
            // Act
            var resultado = await LoginAsync(PasswordCorrecta, "no-existe");

            // Assert
            Assert.Equal(LoginEstado.CredencialesInvalidas, resultado.Estado);
            Assert.Equal(["ObtenerPorUsernameAsync"], _repositorio.Llamadas);
        }

        [Fact]
        public async Task LoginAsync_PasswordCorrecta_ExitosoConDatosDelUsuario()
        {
            // Act
            var resultado = await LoginAsync(PasswordCorrecta);

            // Assert
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
            // Act
            var resultado = await LoginAsync(PasswordIncorrecta);

            // Assert
            Assert.Equal(LoginEstado.CredencialesInvalidas, resultado.Estado);
            Assert.Equal(1, _repositorio.Obtener(Username).IntentosFallidos);
            Assert.Equal(["ObtenerPorUsernameAsync", "RegistrarIntentoFallidoAsync"], _repositorio.Llamadas);
        }

        [Fact]
        public async Task LoginAsync_CuatroFallos_TodaviaNoBloquea()
        {
            // Act
            LoginResult resultado = null!;
            for (var i = 0; i < 4; i++)
            {
                resultado = await LoginAsync(PasswordIncorrecta);
            }

            // Assert
            Assert.Equal(LoginEstado.CredencialesInvalidas, resultado.Estado);
            Assert.Equal(4, _repositorio.Obtener(Username).IntentosFallidos);
            Assert.Null(_repositorio.Obtener(Username).BloqueadoHasta);
        }

        [Fact]
        public async Task LoginAsync_QuintoFallo_BloqueaCincoMinutos()
        {
            // Arrange
            for (var i = 0; i < 4; i++)
            {
                await LoginAsync(PasswordIncorrecta);
            }
            _repositorio.Llamadas.Clear();

            // Act
            var resultado = await LoginAsync(PasswordIncorrecta);

            // Assert
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
            // Arrange
            await BloquearUsuarioAsync();
            _reloj.Advance(TimeSpan.FromMinutes(4) + TimeSpan.FromSeconds(59));
            _repositorio.Llamadas.Clear();

            // Act
            var resultado = await LoginAsync(PasswordCorrecta);

            // Assert
            Assert.Equal(LoginEstado.Bloqueado, resultado.Estado);
            Assert.Equal(Inicio.UtcDateTime.AddMinutes(5), resultado.BloqueadoHasta);
            // Bloqueado: se rechaza sin revisar la contraseña ni sumar intentos.
            Assert.Equal(["ObtenerPorUsernameAsync"], _repositorio.Llamadas);
        }

        [Fact]
        public async Task LoginAsync_AlVencerElBloqueo_PermiteEntrarYLimpiaElBloqueo()
        {
            // Arrange
            await BloquearUsuarioAsync();
            _reloj.Advance(TimeSpan.FromMinutes(5));
            _repositorio.Llamadas.Clear();

            // Act
            var resultado = await LoginAsync(PasswordCorrecta);

            // Assert
            Assert.Equal(LoginEstado.Exitoso, resultado.Estado);
            Assert.Null(_repositorio.Obtener(Username).BloqueadoHasta);
            Assert.Equal(["ObtenerPorUsernameAsync", "ReiniciarIntentosAsync"], _repositorio.Llamadas);
        }

        [Fact]
        public async Task LoginAsync_ExitoTrasFallos_ReiniciaElContador()
        {
            // Arrange
            await LoginAsync(PasswordIncorrecta);
            await LoginAsync(PasswordIncorrecta);
            _repositorio.Llamadas.Clear();

            // Act
            var resultado = await LoginAsync(PasswordCorrecta);

            // Assert
            Assert.Equal(LoginEstado.Exitoso, resultado.Estado);
            Assert.Equal(0, _repositorio.Obtener(Username).IntentosFallidos);
            Assert.Equal(["ObtenerPorUsernameAsync", "ReiniciarIntentosAsync"], _repositorio.Llamadas);
        }

        [Fact]
        public void HashPassword_NoGuardaLaPasswordEnTextoPlano()
        {
            // Act
            var hash = _authService.HashPassword(PasswordCorrecta);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(hash));
            Assert.DoesNotContain(PasswordCorrecta, hash);
            // Cada hash lleva su propia sal.
            Assert.NotEqual(hash, _authService.HashPassword(PasswordCorrecta));
        }

        // ---- Excepciones (NSubstitute) ----
        // AuthService no captura excepciones: si el repositorio falla (BD caída),
        // la excepción sale tal cual hacia el controlador.

        private Usuario CrearUsuario(int intentosFallidos = 0)
        {
            return new Usuario
            {
                Id = 2,
                Nombre = "Usuario de prueba",
                Username = Username,
                PasswordHash = _authService.HashPassword(PasswordCorrecta),
                Rol = "Usuario",
                IntentosFallidos = intentosFallidos
            };
        }

        [Fact]
        public async Task LoginAsync_RepositorioNoDisponible_LanzaExcepcion()
        {
            // Arrange
            var excepcion = new InvalidOperationException("No se pudo conectar a la base de datos.");
            var repositorio = Substitute.For<IUsuarioRepository>();
            repositorio.ObtenerPorUsernameAsync(Username).ThrowsAsync(excepcion);
            var authService = new AuthService(repositorio, _reloj);

            // Act & Assert
            var lanzada = await Assert.ThrowsAsync<InvalidOperationException>(
                () => authService.LoginAsync(new LoginRequest(Username, PasswordCorrecta)));

            // Assert
            Assert.Same(excepcion, lanzada);
            await repositorio.DidNotReceive().RegistrarIntentoFallidoAsync(Arg.Any<int>());
            await repositorio.DidNotReceive().BloquearAsync(Arg.Any<int>(), Arg.Any<DateTime>());
            await repositorio.DidNotReceive().ReiniciarIntentosAsync(Arg.Any<int>());
        }

        [Fact]
        public async Task LoginAsync_FallaAlRegistrarIntento_LanzaExcepcion()
        {
            // Arrange
            var excepcion = new InvalidOperationException("No se pudo conectar a la base de datos.");
            var repositorio = Substitute.For<IUsuarioRepository>();
            repositorio.ObtenerPorUsernameAsync(Username).Returns(CrearUsuario());
            repositorio.RegistrarIntentoFallidoAsync(2).ThrowsAsync(excepcion);
            var authService = new AuthService(repositorio, _reloj);

            // Act & Assert
            var lanzada = await Assert.ThrowsAsync<InvalidOperationException>(
                () => authService.LoginAsync(new LoginRequest(Username, PasswordIncorrecta)));

            // Assert
            Assert.Same(excepcion, lanzada);
            await repositorio.Received(1).RegistrarIntentoFallidoAsync(2);
            await repositorio.DidNotReceive().BloquearAsync(Arg.Any<int>(), Arg.Any<DateTime>());
        }

        [Fact]
        public async Task LoginAsync_FallaAlBloquear_LanzaExcepcion()
        {
            // Arrange
            var excepcion = new InvalidOperationException("No se pudo conectar a la base de datos.");
            var repositorio = Substitute.For<IUsuarioRepository>();
            repositorio.ObtenerPorUsernameAsync(Username).Returns(CrearUsuario(intentosFallidos: 4));
            repositorio.RegistrarIntentoFallidoAsync(2).Returns(5);
            repositorio.BloquearAsync(2, Arg.Any<DateTime>()).ThrowsAsync(excepcion);
            var authService = new AuthService(repositorio, _reloj);

            // Act & Assert
            var lanzada = await Assert.ThrowsAsync<InvalidOperationException>(
                () => authService.LoginAsync(new LoginRequest(Username, PasswordIncorrecta)));

            // Assert
            Assert.Same(excepcion, lanzada);
            await repositorio.Received(1).BloquearAsync(2, Inicio.UtcDateTime.AddMinutes(5));
        }

        [Fact]
        public async Task LoginAsync_UsuarioInexistente_NoRegistraIntentoFallido()
        {
            // Arrange
            var repositorio = Substitute.For<IUsuarioRepository>();
            repositorio.ObtenerPorUsernameAsync("no-existe").Returns((Usuario?)null);
            var authService = new AuthService(repositorio, _reloj);

            // Act
            var resultado = await authService.LoginAsync(new LoginRequest("no-existe", PasswordCorrecta));

            // Assert
            Assert.Equal(LoginEstado.CredencialesInvalidas, resultado.Estado);
            await repositorio.Received(1).ObtenerPorUsernameAsync("no-existe");
            await repositorio.DidNotReceive().RegistrarIntentoFallidoAsync(Arg.Any<int>());
            await repositorio.DidNotReceive().BloquearAsync(Arg.Any<int>(), Arg.Any<DateTime>());
            await repositorio.DidNotReceive().ReiniciarIntentosAsync(Arg.Any<int>());
        }
    }
}
