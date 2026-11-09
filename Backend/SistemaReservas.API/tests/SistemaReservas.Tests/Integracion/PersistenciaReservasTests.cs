using MapsterMapper;
using Microsoft.Extensions.Time.Testing;
using SistemaReservas.Application.Mapper;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Infrastructure.Implementation;
using SistemaReservas.Tests.Fakes;
using SistemaReservas.Tests.Infraestructura;

namespace SistemaReservas.Tests.Integracion
{
    // HU10 - AB#116: pruebas de integración de persistencia de reservas.
    // Cada prueba guarda con un contexto, lo cierra y recupera los datos con otro
    // contexto y otra conexión al mismo archivo SQLite.
    public sealed class PersistenciaReservasTests : IDisposable
    {
        private readonly BaseDatosEnArchivo _baseDatos = new();

        private readonly IMapper _mapper =
            new Mapper(MapsterConfig.RegisterMaps());

        private readonly FakeTimeProvider _reloj =
            new(new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero));

        private readonly int _usuarioId;
        private readonly int _otroUsuarioId;
        private readonly int _laboratorioId;

        public PersistenciaReservasTests()
        {
            using var contexto = _baseDatos.CrearContexto();

            var usuario = new Usuario
            {
                Nombre = "Usuario de prueba",
                Username = "usuario",
                PasswordHash = "hash",
                Rol = "Usuario"
            };

            var otroUsuario = new Usuario
            {
                Nombre = "Otro usuario",
                Username = "otro",
                PasswordHash = "hash",
                Rol = "Usuario"
            };

            var laboratorio = new Laboratorio
            {
                Nombre = "Laboratorio de Software",
                Ubicacion = "Edificio A",
                Capacidad = 30,
                Estado = "Habilitado"
            };

            contexto.Usuarios.AddRange(usuario, otroUsuario);
            contexto.Laboratorios.Add(laboratorio);
            contexto.SaveChanges();

            _usuarioId = usuario.Id;
            _otroUsuarioId = otroUsuario.Id;
            _laboratorioId = laboratorio.Id;
        }

        public void Dispose()
        {
            _baseDatos.Dispose();
        }

        private Reserva NuevaReserva(int usuarioId, int horaInicio, int horaFin)
        {
            return new Reserva
            {
                LaboratorioId = _laboratorioId,
                UsuarioId = usuarioId,
                Fecha = new DateTime(2026, 11, 12),
                HoraInicio = new TimeSpan(horaInicio, 0, 0),
                HoraFin = new TimeSpan(horaFin, 0, 0),
                Estado = "Activa"
            };
        }

        // Guarda con un contexto y devuelve el id; el contexto y su conexión se cierran al salir.
        private async Task<int> GuardarAsync(Reserva reserva)
        {
            await using var contexto = _baseDatos.CrearContexto();
            var repositorio = new ReservaRepository(contexto);

            var guardada = await repositorio.CrearAsync(reserva);

            return guardada.Id;
        }

        [Fact]
        public async Task CrearAsync_ContextoCerrado_OtroContextoRecuperaTodosLosDatos()
        {
            // Arrange
            var reservaId = await GuardarAsync(NuevaReserva(_usuarioId, 9, 11));

            // Act
            await using var contextoNuevo = _baseDatos.CrearContexto();
            var reservas = await new ReservaRepository(contextoNuevo)
                .ObtenerPorUsuarioAsync(_usuarioId);

            // Assert
            Assert.True(File.Exists(_baseDatos.RutaArchivo));

            var reserva = Assert.Single(reservas);
            Assert.Equal(reservaId, reserva.Id);
            Assert.Equal(_usuarioId, reserva.UsuarioId);
            Assert.Equal(_laboratorioId, reserva.LaboratorioId);
            Assert.Equal("Laboratorio de Software", reserva.Laboratorio.Nombre);
            Assert.Equal(new DateTime(2026, 11, 12), reserva.Fecha);
            Assert.Equal(new TimeSpan(9, 0, 0), reserva.HoraInicio);
            Assert.Equal(new TimeSpan(11, 0, 0), reserva.HoraFin);
            Assert.Equal("Activa", reserva.Estado);
        }

        [Fact]
        public async Task ObtenerPorIdAsync_ContextoCerrado_RecuperaLaMismaReserva()
        {
            // Arrange
            var reservaId = await GuardarAsync(NuevaReserva(_usuarioId, 13, 15));

            // Act
            await using var contextoNuevo = _baseDatos.CrearContexto();
            var reserva = await new ReservaRepository(contextoNuevo)
                .ObtenerPorIdAsync(reservaId);

            // Assert
            Assert.NotNull(reserva);
            Assert.Equal(_usuarioId, reserva.UsuarioId);
            Assert.Equal(_laboratorioId, reserva.LaboratorioId);
            Assert.Equal(new TimeSpan(13, 0, 0), reserva.HoraInicio);
            Assert.Equal(new TimeSpan(15, 0, 0), reserva.HoraFin);
        }

        [Fact]
        public async Task GuardarCambiosAsync_ReservaCancelada_ConservaElEstadoTrasReconectar()
        {
            // Arrange
            var reservaId = await GuardarAsync(NuevaReserva(_usuarioId, 9, 11));

            await using (var contextoCambio = _baseDatos.CrearContexto())
            {
                var repositorio = new ReservaRepository(contextoCambio);
                var reserva = await repositorio.ObtenerPorIdAsync(reservaId);
                reserva!.Estado = "Cancelada";
                await repositorio.GuardarCambiosAsync();
            }

            // Act
            await using var contextoNuevo = _baseDatos.CrearContexto();
            var recuperada = await new ReservaRepository(contextoNuevo)
                .ObtenerPorIdAsync(reservaId);

            // Assert
            Assert.Equal("Cancelada", recuperada!.Estado);
        }

        [Fact]
        public async Task ObtenerPorUsuarioAsync_VariosUsuarios_CadaUnoRecuperaSoloLasSuyas()
        {
            // Arrange
            await GuardarAsync(NuevaReserva(_usuarioId, 7, 8));
            await GuardarAsync(NuevaReserva(_otroUsuarioId, 9, 10));
            await GuardarAsync(NuevaReserva(_usuarioId, 11, 12));

            // Act
            await using var contextoNuevo = _baseDatos.CrearContexto();
            var repositorio = new ReservaRepository(contextoNuevo);
            var delUsuario = await repositorio.ObtenerPorUsuarioAsync(_usuarioId);
            var delOtro = await repositorio.ObtenerPorUsuarioAsync(_otroUsuarioId);

            // Assert
            Assert.Equal(2, delUsuario.Count);
            Assert.All(delUsuario, r => Assert.Equal(_usuarioId, r.UsuarioId));
            Assert.Equal(new TimeSpan(9, 0, 0), Assert.Single(delOtro).HoraInicio);
        }

        // Equivale a consultar Mis Reservas después de reiniciar la aplicación.
        [Fact]
        public async Task ObtenerMisReservasAsync_ServicioConContextoNuevo_DevuelveLaReservaGuardada()
        {
            // Arrange
            var reservaId = await GuardarAsync(NuevaReserva(_usuarioId, 9, 11));

            await using var contextoNuevo = _baseDatos.CrearContexto();
            var servicio = ReservaServiceFactory.Crear(contextoNuevo, _reloj, _mapper);

            // Act
            var misReservas = await servicio.ObtenerMisReservasAsync(_usuarioId);

            // Assert
            var reserva = Assert.Single(misReservas);
            Assert.Equal(reservaId, reserva.Id);
            Assert.Equal(_laboratorioId, reserva.LaboratorioId);
            Assert.Equal("Laboratorio de Software", reserva.NombreLaboratorio);
            Assert.Equal(new DateOnly(2026, 11, 12), reserva.Fecha);
            Assert.Equal(new TimeSpan(9, 0, 0), reserva.HoraInicio);
            Assert.Equal(new TimeSpan(11, 0, 0), reserva.HoraFin);
            Assert.Equal("Activa", reserva.Estado);
        }
    }
}
