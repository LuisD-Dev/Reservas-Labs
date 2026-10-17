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
        [Fact]
        public async Task SaveChangesAsync_ReservaValida_AsociaUsuarioYLaboratorio()
        {
            // Arrange
            int usuarioId;
            int laboratorioId;

            using (var contexto = _baseDatos.CrearContexto())
            {
                var usuario = CrearUsuario(
                    "responsable",
                    "Usuario");

                var laboratorio = new Laboratorio
                {
                    Nombre = "Laboratorio de Software",
                    Ubicacion = "Edificio A",
                    Capacidad = 30,
                    Estado = "Habilitado"
                };

                contexto.Usuarios.Add(usuario);
                contexto.Laboratorios.Add(laboratorio);

                await contexto.SaveChangesAsync();

                usuarioId = usuario.Id;
                laboratorioId = laboratorio.Id;

                contexto.Reservas.Add(new Reserva
                {
                    LaboratorioId = laboratorioId,
                    UsuarioId = usuarioId,
                    Fecha = new DateTime(2026, 10, 20),
                    HoraInicio = new TimeSpan(9, 0, 0),
                    HoraFin = new TimeSpan(11, 0, 0)
                });

                await contexto.SaveChangesAsync();
            }

            using var contextoConsulta =
                _baseDatos.CrearContexto();

            // Act
            var reserva = await contextoConsulta.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Laboratorio)
                .SingleAsync();

            // Assert
            Assert.Equal(usuarioId, reserva.UsuarioId);
            Assert.Equal(laboratorioId, reserva.LaboratorioId);

            Assert.Equal(
                "responsable",
                reserva.Usuario.Username);

            Assert.Equal(
                "Laboratorio de Software",
                reserva.Laboratorio.Nombre);

            Assert.Equal("Activa", reserva.Estado);
        }
    }
}
