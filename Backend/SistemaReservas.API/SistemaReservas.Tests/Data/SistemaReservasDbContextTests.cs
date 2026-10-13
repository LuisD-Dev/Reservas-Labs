using Microsoft.EntityFrameworkCore;
using SistemaReservas.API.Models;
using SistemaReservas.Tests.Infraestructura;

namespace SistemaReservas.Tests.Data
{
    // Taller V - Pruebas del modelo de SistemaReservasDbContext (Data/Configurations) sobre
    // SQLite en memoria: los check constraints y el índice único rechazan datos inválidos.
    // Patrón AAA: la base de datos vacía se crea en el campo _baseDatos.
    public sealed class SistemaReservasDbContextTests : IDisposable
    {
        private readonly BaseDatosEnMemoria _baseDatos = new();

        public void Dispose()
        {
            _baseDatos.Dispose();
        }

        private static Usuario CrearUsuario(string username, string rol)
        {
            return new Usuario { Nombre = "Usuario de prueba", Username = username, PasswordHash = "hash", Rol = rol };
        }

        [Fact]
        public async Task SaveChangesAsync_UsuarioConRolInvalido_LanzaExcepcion()
        {
            // Arrange
            using var contexto = _baseDatos.CrearContexto();
            contexto.Usuarios.Add(CrearUsuario("invitado", "Invitado"));

            // Act & Assert
            var excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

            // Assert
            Assert.Contains("CK_Usuarios_Rol", excepcion.InnerException?.Message);
        }

        [Fact]
        public async Task SaveChangesAsync_UsernameDuplicado_LanzaExcepcion()
        {
            // Arrange
            using (var contexto = _baseDatos.CrearContexto())
            {
                contexto.Usuarios.Add(CrearUsuario("usuario", "Usuario"));
                await contexto.SaveChangesAsync();
            }
            using var contextoDuplicado = _baseDatos.CrearContexto();
            contextoDuplicado.Usuarios.Add(CrearUsuario("usuario", "Administrador"));

            // Act & Assert
            var excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contextoDuplicado.SaveChangesAsync());

            // Assert
            Assert.Contains("UNIQUE", excepcion.InnerException?.Message);
            Assert.Contains("Usuarios.Username", excepcion.InnerException?.Message);
        }

        [Fact]
        public async Task SaveChangesAsync_LaboratorioConEstadoInvalido_LanzaExcepcion()
        {
            // Arrange
            using var contexto = _baseDatos.CrearContexto();
            contexto.Laboratorios.Add(new Laboratorio
            {
                Nombre = "Laboratorio de Software",
                Ubicacion = "Edificio A",
                Capacidad = 30,
                Estado = "En mantenimiento"
            });

            // Act & Assert
            var excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

            // Assert
            Assert.Contains("CK_Laboratorios_Estado", excepcion.InnerException?.Message);
        }

        [Fact]
        public async Task SaveChangesAsync_DisponibilidadConEstadoInvalido_LanzaExcepcion()
        {
            // Arrange
            using var contexto = _baseDatos.CrearContexto();
            var laboratorio = new Laboratorio
            {
                Nombre = "Laboratorio de Software",
                Ubicacion = "Edificio A",
                Capacidad = 30,
                Estado = "Habilitado"
            };
            contexto.Laboratorios.Add(laboratorio);
            await contexto.SaveChangesAsync();
            contexto.Disponibilidades.Add(new Disponibilidad
            {
                LaboratorioId = laboratorio.Id,
                Fecha = new DateTime(2026, 10, 13),
                HoraInicio = TimeSpan.FromHours(8),
                HoraFin = TimeSpan.FromHours(12),
                Estado = "Reservado"
            });

            // Act & Assert
            var excepcion = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

            // Assert
            Assert.Contains("CK_Disponibilidades_Estado", excepcion.InnerException?.Message);
        }
    }
}
