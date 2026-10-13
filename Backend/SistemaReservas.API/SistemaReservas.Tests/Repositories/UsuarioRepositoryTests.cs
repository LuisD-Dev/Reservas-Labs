using Microsoft.EntityFrameworkCore;
using SistemaReservas.API.Models;
using SistemaReservas.API.Repositories;
using SistemaReservas.Tests.Infraestructura;

namespace SistemaReservas.Tests.Repositories
{
    // Taller V - Pruebas de UsuarioRepository (HU1) con EF Core sobre SQLite en memoria:
    // se ejecutan las consultas y los UPDATE reales (ExecuteUpdateAsync), sin SQL Server.
    // Lo guardado se verifica con un contexto nuevo, leyendo de la base de datos.
    // Patrón AAA: el Arrange común (base de datos con el usuario "usuario") está en el constructor.
    // SQLite no conserva DateTime.Kind: las fechas se comparan por valor.
    public sealed class UsuarioRepositoryTests : IDisposable
    {
        private const int IdInexistente = 9999;

        private readonly BaseDatosEnMemoria _baseDatos = new();
        private readonly int _usuarioId;

        public UsuarioRepositoryTests()
        {
            using var contexto = _baseDatos.CrearContexto();
            var usuario = new Usuario
            {
                Nombre = "Usuario de prueba",
                Username = "usuario",
                PasswordHash = "hash",
                Rol = "Usuario"
            };
            contexto.Usuarios.Add(usuario);
            contexto.SaveChanges();
            _usuarioId = usuario.Id;
        }

        public void Dispose()
        {
            _baseDatos.Dispose();
        }

        private Usuario LeerUsuario()
        {
            using var contexto = _baseDatos.CrearContexto();
            return contexto.Usuarios.AsNoTracking().Single(u => u.Id == _usuarioId);
        }

        [Fact]
        public async Task ObtenerPorUsernameAsync_UsuarioExistente_DevuelveUsuario()
        {
            // Arrange
            using var contexto = _baseDatos.CrearContexto();
            var repositorio = new UsuarioRepository(contexto);

            // Act
            var usuario = await repositorio.ObtenerPorUsernameAsync("usuario");

            // Assert
            Assert.NotNull(usuario);
            Assert.Equal(_usuarioId, usuario.Id);
            Assert.Equal("Usuario de prueba", usuario.Nombre);
            Assert.Equal("Usuario", usuario.Rol);
            Assert.Equal(0, usuario.IntentosFallidos);
            Assert.Null(usuario.BloqueadoHasta);
        }

        [Fact]
        public async Task ObtenerPorUsernameAsync_UsuarioInexistente_DevuelveNull()
        {
            // Arrange
            using var contexto = _baseDatos.CrearContexto();
            var repositorio = new UsuarioRepository(contexto);

            // Act
            var usuario = await repositorio.ObtenerPorUsernameAsync("no-existe");

            // Assert
            Assert.Null(usuario);
        }

        [Fact]
        public async Task RegistrarIntentoFallidoAsync_UsuarioExistente_DevuelveNuevoValor()
        {
            // Arrange
            using var contexto = _baseDatos.CrearContexto();
            var repositorio = new UsuarioRepository(contexto);

            // Act
            var primero = await repositorio.RegistrarIntentoFallidoAsync(_usuarioId);
            var segundo = await repositorio.RegistrarIntentoFallidoAsync(_usuarioId);

            // Assert
            Assert.Equal(1, primero);
            Assert.Equal(2, segundo);
            Assert.Equal(2, LeerUsuario().IntentosFallidos);
        }

        [Fact]
        public async Task ReiniciarIntentosAsync_UsuarioBloqueado_LimpiaContadorYBloqueo()
        {
            // Arrange
            using var contexto = _baseDatos.CrearContexto();
            var repositorio = new UsuarioRepository(contexto);
            await repositorio.RegistrarIntentoFallidoAsync(_usuarioId);
            await repositorio.BloquearAsync(_usuarioId, new DateTime(2026, 10, 5, 12, 5, 0));
            await repositorio.RegistrarIntentoFallidoAsync(_usuarioId);

            // Act
            await repositorio.ReiniciarIntentosAsync(_usuarioId);

            // Assert
            var usuario = LeerUsuario();
            Assert.Equal(0, usuario.IntentosFallidos);
            Assert.Null(usuario.BloqueadoHasta);
        }

        [Fact]
        public async Task BloquearAsync_UsuarioExistente_GuardaFechaYReiniciaContador()
        {
            // Arrange
            using var contexto = _baseDatos.CrearContexto();
            var repositorio = new UsuarioRepository(contexto);
            await repositorio.RegistrarIntentoFallidoAsync(_usuarioId);
            var bloqueadoHasta = new DateTime(2026, 10, 5, 12, 5, 0, DateTimeKind.Utc);

            // Act
            await repositorio.BloquearAsync(_usuarioId, bloqueadoHasta);

            // Assert
            var usuario = LeerUsuario();
            Assert.Equal(0, usuario.IntentosFallidos);
            Assert.NotNull(usuario.BloqueadoHasta);
            Assert.Equal(bloqueadoHasta.Ticks, usuario.BloqueadoHasta.Value.Ticks);
        }

        // ---- Excepciones ----

        [Fact]
        public async Task RegistrarIntentoFallidoAsync_UsuarioInexistente_LanzaExcepcion()
        {
            // Arrange
            using var contexto = _baseDatos.CrearContexto();
            var repositorio = new UsuarioRepository(contexto);

            // Act & Assert
            var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repositorio.RegistrarIntentoFallidoAsync(IdInexistente));

            // Assert
            Assert.Equal($"Usuario {IdInexistente} no encontrado.", excepcion.Message);
        }

        [Fact]
        public async Task ReiniciarIntentosAsync_UsuarioInexistente_LanzaExcepcion()
        {
            // Arrange
            using var contexto = _baseDatos.CrearContexto();
            var repositorio = new UsuarioRepository(contexto);

            // Act & Assert
            var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repositorio.ReiniciarIntentosAsync(IdInexistente));

            // Assert
            Assert.Equal($"Usuario {IdInexistente} no encontrado.", excepcion.Message);
        }

        [Fact]
        public async Task BloquearAsync_UsuarioInexistente_LanzaExcepcion()
        {
            // Arrange
            using var contexto = _baseDatos.CrearContexto();
            var repositorio = new UsuarioRepository(contexto);

            // Act & Assert
            var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repositorio.BloquearAsync(IdInexistente, new DateTime(2026, 10, 5, 12, 5, 0)));

            // Assert
            Assert.Equal($"Usuario {IdInexistente} no encontrado.", excepcion.Message);
        }
    }
}
