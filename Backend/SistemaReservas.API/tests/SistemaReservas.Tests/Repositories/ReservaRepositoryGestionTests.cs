using SistemaReservas.Domain.Entities;
using SistemaReservas.Infrastructure.Implementation;
using SistemaReservas.Tests.Infraestructura;

namespace SistemaReservas.Tests.Repositories
{
    // Gestión de Reservas: la consulta del Administrador trae las reservas de todos los usuarios.
    public sealed class ReservaRepositoryGestionTests : IDisposable
    {
        private readonly BaseDatosEnMemoria _baseDatos = new();

        public void Dispose()
        {
            _baseDatos.Dispose();
        }

        [Fact]
        public async Task ObtenerTodasAsync_TraeReservasDeTodosLosUsuariosConLaboratorioYUsuario_Ordenadas()
        {
            // Arrange
            using (var contexto = _baseDatos.CrearContexto())
            {
                var ana = new Usuario { Nombre = "Ana", Username = "ana", PasswordHash = "hash", Rol = "Usuario" };
                var luis = new Usuario { Nombre = "Luis", Username = "luis", PasswordHash = "hash", Rol = "Usuario" };
                var software = new Laboratorio { Nombre = "Software", Ubicacion = "Edificio A", Capacidad = 30, Estado = "Habilitado" };
                var redes = new Laboratorio { Nombre = "Redes", Ubicacion = "Edificio B", Capacidad = 20, Estado = "Habilitado" };
                contexto.Usuarios.AddRange(ana, luis);
                contexto.Laboratorios.AddRange(software, redes);
                contexto.SaveChanges();

                // Se insertan fuera de orden para comprobar el orden por fecha y hora.
                contexto.Reservas.AddRange(
                    NuevaReserva(redes.Id, luis.Id, new DateTime(2026, 10, 21), 8, "Cancelada"),
                    NuevaReserva(software.Id, ana.Id, new DateTime(2026, 10, 20), 13, "Activa"),
                    NuevaReserva(software.Id, luis.Id, new DateTime(2026, 10, 20), 9, "Activa"));
                contexto.SaveChanges();
            }

            // Act
            using var contextoConsulta = _baseDatos.CrearContexto();
            var reservas = await new ReservaRepository(contextoConsulta).ObtenerTodasAsync();

            // Assert
            Assert.Equal(3, reservas.Count);
            Assert.Equal(
                new[] { "Luis|Software|Activa", "Ana|Software|Activa", "Luis|Redes|Cancelada" },
                reservas.Select(r => $"{r.Usuario.Nombre}|{r.Laboratorio.Nombre}|{r.Estado}"));
        }

        [Fact]
        public async Task ObtenerTodasAsync_SinReservas_DevuelveListaVacia()
        {
            // Act
            using var contexto = _baseDatos.CrearContexto();
            var reservas = await new ReservaRepository(contexto).ObtenerTodasAsync();

            // Assert
            Assert.Empty(reservas);
        }

        private static Reserva NuevaReserva(int laboratorioId, int usuarioId, DateTime fecha, int hora, string estado) =>
            new()
            {
                LaboratorioId = laboratorioId,
                UsuarioId = usuarioId,
                Fecha = fecha,
                HoraInicio = new TimeSpan(hora, 0, 0),
                HoraFin = new TimeSpan(hora + 1, 0, 0),
                Estado = estado
            };
    }
}
