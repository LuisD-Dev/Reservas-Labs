using Microsoft.EntityFrameworkCore;
using SistemaReservas.API.Models;
using SistemaReservas.API.Repositories;
using SistemaReservas.Tests.Infraestructura;
using Xunit;

namespace SistemaReservas.Tests.Repositories
{
    public sealed class ReservaRepositoryTests : IDisposable
    {
        private readonly BaseDatosEnMemoria _baseDatos = new();

        private readonly int _usuarioId;
        private readonly int _laboratorioId;

        public ReservaRepositoryTests()
        {
            using var contexto = _baseDatos.CrearContexto();

            var usuario = new Usuario
            {
                Nombre = "Usuario responsable",
                Username = "responsable",
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

            contexto.Usuarios.Add(usuario);
            contexto.Laboratorios.Add(laboratorio);
            contexto.SaveChanges();

            _usuarioId = usuario.Id;
            _laboratorioId = laboratorio.Id;
        }

        public void Dispose()
        {
            _baseDatos.Dispose();
        }

        [Fact]
        public async Task ObtenerActivasPorLaboratorioYFechaAsync_FiltraLaboratorioFechaYEstado()
        {
            // Arrange
            var fechaObjetivo = new DateTime(2026, 10, 20);
            int reservaEsperadaId;

            using (var contexto = _baseDatos.CrearContexto())
            {
                var otroLaboratorio = new Laboratorio
                {
                    Nombre = "Laboratorio de Redes",
                    Ubicacion = "Edificio B",
                    Capacidad = 20,
                    Estado = "Habilitado"
                };

                contexto.Laboratorios.Add(otroLaboratorio);
                await contexto.SaveChangesAsync();

                var reservaEsperada = new Reserva
                {
                    LaboratorioId = _laboratorioId,
                    UsuarioId = _usuarioId,
                    Fecha = fechaObjetivo,
                    HoraInicio = new TimeSpan(8, 0, 0),
                    HoraFin = new TimeSpan(9, 0, 0),
                    Estado = "Activa"
                };

                contexto.Reservas.AddRange(
                    reservaEsperada,
                    new Reserva
                    {
                        LaboratorioId = _laboratorioId,
                        UsuarioId = _usuarioId,
                        Fecha = fechaObjetivo,
                        HoraInicio = new TimeSpan(10, 0, 0),
                        HoraFin = new TimeSpan(11, 0, 0),
                        Estado = "Cancelada"
                    },
                    new Reserva
                    {
                        LaboratorioId = otroLaboratorio.Id,
                        UsuarioId = _usuarioId,
                        Fecha = fechaObjetivo,
                        HoraInicio = new TimeSpan(12, 0, 0),
                        HoraFin = new TimeSpan(13, 0, 0),
                        Estado = "Activa"
                    },
                    new Reserva
                    {
                        LaboratorioId = _laboratorioId,
                        UsuarioId = _usuarioId,
                        Fecha = fechaObjetivo.AddDays(1),
                        HoraInicio = new TimeSpan(14, 0, 0),
                        HoraFin = new TimeSpan(15, 0, 0),
                        Estado = "Activa"
                    });

                await contexto.SaveChangesAsync();
                reservaEsperadaId = reservaEsperada.Id;
            }

            using var contextoConsulta = _baseDatos.CrearContexto();
            var repositorio = new ReservaRepository(contextoConsulta);

            // Act
            var resultado =
                await repositorio.ObtenerActivasPorLaboratorioYFechaAsync(
                    _laboratorioId,
                    fechaObjetivo.AddHours(15));

            // Assert
            var reserva = Assert.Single(resultado);

            Assert.Equal(reservaEsperadaId, reserva.Id);
            Assert.Equal(_laboratorioId, reserva.LaboratorioId);
            Assert.Equal(fechaObjetivo, reserva.Fecha);
            Assert.Equal("Activa", reserva.Estado);
            Assert.Equal(new TimeSpan(8, 0, 0), reserva.HoraInicio);
            Assert.Equal(new TimeSpan(9, 0, 0), reserva.HoraFin);
        }

        [Fact]
        public async Task CrearAsync_ReservaValida_GuardaUsuarioResponsable()
        {
            // Arrange
            using var contexto = _baseDatos.CrearContexto();
            var repositorio = new ReservaRepository(contexto);

            var reserva = new Reserva
            {
                LaboratorioId = _laboratorioId,
                UsuarioId = _usuarioId,
                Fecha = new DateTime(2026, 10, 20),
                HoraInicio = new TimeSpan(9, 0, 0),
                HoraFin = new TimeSpan(11, 0, 0)
            };

            // Act
            var resultado = await repositorio.CrearAsync(reserva);

            // Assert
            Assert.Same(reserva, resultado);
            Assert.True(resultado.Id > 0);
            Assert.Equal(_usuarioId, resultado.UsuarioId);

            using var contextoConsulta = _baseDatos.CrearContexto();

            var guardada = await contextoConsulta.Reservas
                .Include(r => r.Usuario)
                .SingleAsync();

            Assert.Equal(_usuarioId, guardada.UsuarioId);
            Assert.Equal("responsable", guardada.Usuario.Username);
            Assert.Equal("Activa", guardada.Estado);
        }

        // HU4 - AB#78: la BD rechaza una reserva de un laboratorio
        // inexistente por la FK; el repositorio propaga la excepción.
        [Fact]
        public async Task CrearAsync_LaboratorioInexistente_LanzaDbUpdateException()
        {
            // Arrange
            using var contexto = _baseDatos.CrearContexto();
            var repositorio = new ReservaRepository(contexto);

            var reserva = new Reserva
            {
                LaboratorioId = _laboratorioId + 999,
                UsuarioId = _usuarioId,
                Fecha = new DateTime(2026, 10, 20),
                HoraInicio = new TimeSpan(9, 0, 0),
                HoraFin = new TimeSpan(11, 0, 0),
                Estado = "Activa"
            };

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(
                () => repositorio.CrearAsync(reserva));

            using var contextoConsulta = _baseDatos.CrearContexto();

            Assert.Empty(contextoConsulta.Reservas);
        }


        // HU8 - AB#98: una reserva inexistente devuelve null.
        [Fact]
        public async Task ObtenerPorIdAsync_ReservaInexistente_DevuelveNull()
        {
            using var contexto = _baseDatos.CrearContexto();
            var repositorio = new ReservaRepository(contexto);

            var resultado =
                await repositorio.ObtenerPorIdAsync(int.MaxValue);

            Assert.Null(resultado);
        }

        // HU8 - AB#98: cancelar conserva la reserva y persiste su estado.
        [Fact]
        public async Task GuardarCambiosAsync_Cancelacion_ConservaReservaYPersisteEstado()
        {
            var fecha = new DateTime(2026, 11, 10);
            var horaInicio = new TimeSpan(9, 0, 0);
            var horaFin = new TimeSpan(10, 0, 0);
            int reservaId;

            // Crear una reserva y guardarla.
            using (var contexto = _baseDatos.CrearContexto())
            {
                var reserva = new Reserva
                {
                    UsuarioId = _usuarioId,
                    LaboratorioId = _laboratorioId,
                    Fecha = fecha,
                    HoraInicio = horaInicio,
                    HoraFin = horaFin,
                    Estado = "Activa"
                };

                contexto.Reservas.Add(reserva);
                await contexto.SaveChangesAsync();

                reservaId = reserva.Id;
            }

            // Recuperarla mediante el repositorio y cancelar.
            using (var contexto = _baseDatos.CrearContexto())
            {
                var repositorio = new ReservaRepository(contexto);

                var reserva =
                    await repositorio.ObtenerPorIdAsync(reservaId);

                Assert.NotNull(reserva);
                Assert.Equal(reservaId, reserva.Id);

                reserva.Estado = "Cancelada";

                await repositorio.GuardarCambiosAsync();
            }

            // Consultar desde otro contexto para comprobar la persistencia.
            using (var contexto = _baseDatos.CrearContexto())
            {
                var guardada = await contexto.Reservas
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        reserva => reserva.Id == reservaId);

                Assert.NotNull(guardada);
                Assert.Equal("Cancelada", guardada.Estado);
                Assert.Equal(_usuarioId, guardada.UsuarioId);
                Assert.Equal(_laboratorioId, guardada.LaboratorioId);
                Assert.Equal(fecha, guardada.Fecha);
                Assert.Equal(horaInicio, guardada.HoraInicio);
                Assert.Equal(horaFin, guardada.HoraFin);
            }
        }
        // HU7 - AB#96: crea un usuario adicional para probar el aislamiento entre usuarios.
        private int CrearOtroUsuario()
        {
            using var contexto =
                _baseDatos.CrearContexto();

            var otroUsuario = new Usuario
            {
                Nombre = "Otro usuario",
                Username = "otro",
                PasswordHash = "hash",
                Rol = "Usuario"
            };

            contexto.Usuarios.Add(otroUsuario);
            contexto.SaveChanges();

            return otroUsuario.Id;
        }

        private Reserva NuevaReserva(
            int usuarioId,
            DateTime fecha,
            int horaInicio,
            string estado = "Activa")
        {
            return new Reserva
            {
                LaboratorioId = _laboratorioId,
                UsuarioId = usuarioId,
                Fecha = fecha,
                HoraInicio = new TimeSpan(horaInicio, 0, 0),
                HoraFin = new TimeSpan(horaInicio + 1, 0, 0),
                Estado = estado
            };
        }

        // HU7 - AB#96: aislamiento por usuario, solo se devuelven las reservas propias.
        [Fact]
        public async Task ObtenerPorUsuarioAsync_VariosUsuarios_DevuelveSoloLasDelUsuario()
        {
            // Arrange
            var otroUsuarioId = CrearOtroUsuario();
            var fecha = new DateTime(2026, 11, 10);

            using (var contexto = _baseDatos.CrearContexto())
            {
                contexto.Reservas.AddRange(
                    NuevaReserva(_usuarioId, fecha, 8),
                    NuevaReserva(otroUsuarioId, fecha, 10),
                    NuevaReserva(_usuarioId, fecha, 12),
                    NuevaReserva(otroUsuarioId, fecha.AddDays(1), 14));

                await contexto.SaveChangesAsync();
            }

            using var contextoConsulta = _baseDatos.CrearContexto();
            var repositorio = new ReservaRepository(contextoConsulta);

            // Act
            var resultado = await repositorio.ObtenerPorUsuarioAsync(_usuarioId);

            // Assert
            Assert.Equal(2, resultado.Count);
            Assert.All(
                resultado,
                reserva => Assert.Equal(_usuarioId, reserva.UsuarioId));
        }

        // HU7 - AB#96: el otro usuario tampoco ve las reservas del primero.
        [Fact]
        public async Task ObtenerPorUsuarioAsync_OtroUsuario_NoVeReservasAjenas()
        {
            // Arrange
            var otroUsuarioId = CrearOtroUsuario();

            using (var contexto = _baseDatos.CrearContexto())
            {
                contexto.Reservas.Add(
                    NuevaReserva(_usuarioId, new DateTime(2026, 11, 10), 8));

                await contexto.SaveChangesAsync();
            }

            using var contextoConsulta = _baseDatos.CrearContexto();
            var repositorio = new ReservaRepository(contextoConsulta);

            // Act
            var resultado = await repositorio.ObtenerPorUsuarioAsync(otroUsuarioId);

            // Assert
            Assert.Empty(resultado);
        }

        // HU7 - AB#96: Mis Reservas muestra tanto las activas como las canceladas.
        [Fact]
        public async Task ObtenerPorUsuarioAsync_ActivasYCanceladas_DevuelveAmbosEstados()
        {
            // Arrange
            var fecha = new DateTime(2026, 11, 10);

            using (var contexto = _baseDatos.CrearContexto())
            {
                contexto.Reservas.AddRange(
                    NuevaReserva(_usuarioId, fecha, 8, "Activa"),
                    NuevaReserva(_usuarioId, fecha, 10, "Cancelada"));

                await contexto.SaveChangesAsync();
            }

            using var contextoConsulta = _baseDatos.CrearContexto();
            var repositorio = new ReservaRepository(contextoConsulta);

            // Act
            var resultado = await repositorio.ObtenerPorUsuarioAsync(_usuarioId);

            // Assert
            Assert.Equal(
                new[] { "Activa", "Cancelada" },
                resultado.Select(reserva => reserva.Estado));
        }

        // HU7 - AB#96: orden por fecha, luego hora de inicio y luego id.
        [Fact]
        public async Task ObtenerPorUsuarioAsync_VariasReservas_OrdenaPorFechaYHora()
        {
            // Arrange
            var fecha = new DateTime(2026, 11, 10);
            var tardeId = 0;
            var mananaId = 0;
            var diaSiguienteId = 0;
            var empateId = 0;

            using (var contexto = _baseDatos.CrearContexto())
            {
                var diaSiguiente = NuevaReserva(_usuarioId, fecha.AddDays(1), 7);
                var tarde = NuevaReserva(_usuarioId, fecha, 15);
                var manana = NuevaReserva(_usuarioId, fecha, 9);
                var empate = NuevaReserva(_usuarioId, fecha, 15, "Cancelada");

                contexto.Reservas.AddRange(diaSiguiente, tarde, manana, empate);
                await contexto.SaveChangesAsync();

                tardeId = tarde.Id;
                mananaId = manana.Id;
                diaSiguienteId = diaSiguiente.Id;
                empateId = empate.Id;
            }

            using var contextoConsulta = _baseDatos.CrearContexto();
            var repositorio = new ReservaRepository(contextoConsulta);

            // Act
            var resultado = await repositorio.ObtenerPorUsuarioAsync(_usuarioId);

            // Assert
            Assert.Equal(
                new[] { mananaId, tardeId, empateId, diaSiguienteId },
                resultado.Select(reserva => reserva.Id));
        }

        // HU7 - AB#96: incluye el laboratorio para mostrar su nombre.
        [Fact]
        public async Task ObtenerPorUsuarioAsync_ReservaExistente_IncluyeLaboratorio()
        {
            // Arrange
            using (var contexto = _baseDatos.CrearContexto())
            {
                contexto.Reservas.Add(
                    NuevaReserva(_usuarioId, new DateTime(2026, 11, 10), 8));

                await contexto.SaveChangesAsync();
            }

            using var contextoConsulta = _baseDatos.CrearContexto();
            var repositorio = new ReservaRepository(contextoConsulta);

            // Act
            var resultado = await repositorio.ObtenerPorUsuarioAsync(_usuarioId);

            // Assert
            var reserva = Assert.Single(resultado);
            Assert.NotNull(reserva.Laboratorio);
            Assert.Equal("Laboratorio de Software", reserva.Laboratorio.Nombre);
        }

        // HU7 - AB#96: usuario sin reservas recibe una lista vacía (no null).
        [Fact]
        public async Task ObtenerPorUsuarioAsync_UsuarioSinReservas_DevuelveListaVacia()
        {
            // Arrange
            using var contexto = _baseDatos.CrearContexto();
            var repositorio = new ReservaRepository(contexto);

            // Act
            var resultado = await repositorio.ObtenerPorUsuarioAsync(_usuarioId);

            // Assert
            Assert.NotNull(resultado);
            Assert.Empty(resultado);

        }
    }
}