using NSubstitute;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using SistemaReservas.API.DTOs;
using SistemaReservas.API.Models;
using SistemaReservas.API.Repositories;
using SistemaReservas.API.Services;
using SistemaReservas.Tests.Infraestructura;

namespace SistemaReservas.Tests.Services
{
    public class ReservaServiceConflictosTests
    {
        private readonly FakeTimeProvider _reloj = new(
            new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));

        private static readonly DateTime Dia =
            new(2026, 10, 20);

        private static IReadOnlyList<Reserva> ReservasExistentes() =>
            new List<Reserva>
            {
                new()
                {
                    LaboratorioId = 3,
                    UsuarioId = 5,
                    Fecha = Dia,
                    HoraInicio = new TimeSpan(9, 0, 0),
                    HoraFin = new TimeSpan(11, 0, 0),
                    Estado = "Activa"
                }
            };

        private static CrearReservaRequest Solicitud(int inicio, int fin) =>
            new(3, new DateOnly(2026, 10, 20),
                new TimeSpan(inicio, 0, 0),
                new TimeSpan(fin, 0, 0));

        private static CrearReservaRequest Solicitud(
            int horaInicio, int minutoInicio, int horaFin, int minutoFin) =>
            new(3, DateOnly.FromDateTime(Dia),
                new TimeSpan(horaInicio, minutoInicio, 0),
                new TimeSpan(horaFin, minutoFin, 0));

        [Theory]
        [InlineData(8, 0, 9, 1)]
        [InlineData(10, 59, 12, 0)]
        public async Task CrearAsync_TraslapeParcialDeUnMinuto_NoGuardaReserva(
            int horaInicio,
            int minutoInicio,
            int horaFin,
            int minutoFin)
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            repositorio
                .ObtenerActivasPorLaboratorioYFechaAsync(3, Dia)
                .Returns(ReservasExistentes());

            var servicio =
                new ReservaService(repositorio, _reloj);

            // Act
            var resultado = await servicio.CrearAsync(
                27,
                Solicitud(horaInicio, minutoInicio, horaFin, minutoFin));

            // Assert
            Assert.False(resultado.Exitoso);
            Assert.Null(resultado.Reserva);
            Assert.Equal(
                ReservaService.MensajeConflicto,
                resultado.Errores["conflicto"]);

            await repositorio
                .DidNotReceive()
                .CrearAsync(Arg.Any<Reserva>());
        }

        [Theory]
        [InlineData(10, 12)]  // empieza dentro de la existente
        [InlineData(8, 10)]   // termina dentro de la existente
        [InlineData(10, 11)]  // contenida en la existente
        [InlineData(9, 11)]   // idéntica
        public async Task CrearAsync_ReservaTraslapada_NoGuardaReserva(
            int inicio,
            int fin)
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            repositorio
                .ObtenerActivasPorLaboratorioYFechaAsync(3, Dia)
                .Returns(ReservasExistentes());

            var servicio =
                new ReservaService(repositorio, _reloj);

            // Act
            var resultado =
                await servicio.CrearAsync(27, Solicitud(inicio, fin));

            // Assert
            Assert.False(resultado.Exitoso);
            Assert.Null(resultado.Reserva);
            Assert.Equal(
                ReservaService.MensajeConflicto,
                resultado.Errores["conflicto"]);

            await repositorio
                .DidNotReceive()
                .CrearAsync(Arg.Any<Reserva>());
        }

        [Fact]
        public async Task CrearAsync_ReservaConsecutiva_GuardaReserva()
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            repositorio
                .ObtenerActivasPorLaboratorioYFechaAsync(3, Dia)
                .Returns(ReservasExistentes());

            repositorio
                .CrearAsync(Arg.Any<Reserva>())
                .Returns(llamada => llamada.Arg<Reserva>());

            var servicio =
                new ReservaService(repositorio, _reloj);

            // Act
            var resultado =
                await servicio.CrearAsync(27, Solicitud(11, 12));

            // Assert
            Assert.True(resultado.Exitoso);

            await repositorio
                .Received(1)
                .CrearAsync(Arg.Any<Reserva>());
        }

        [Fact]
        public async Task CrearAsync_SinReservasPrevias_GuardaReservaYConsultaLaboratorioYFecha()
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            repositorio
                .ObtenerActivasPorLaboratorioYFechaAsync(3, Dia)
                .Returns(new List<Reserva>());

            repositorio
                .CrearAsync(Arg.Any<Reserva>())
                .Returns(llamada => llamada.Arg<Reserva>());

            var servicio =
                new ReservaService(repositorio, _reloj);

            // Act
            var resultado =
                await servicio.CrearAsync(27, Solicitud(9, 11));

            // Assert
            Assert.True(resultado.Exitoso);

            await repositorio
                .Received(1)
                .ObtenerActivasPorLaboratorioYFechaAsync(3, Dia);
        }

        [Fact]
        public async Task CrearAsync_DatosInvalidos_NoConsultaConflictos()
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            var servicio =
                new ReservaService(repositorio, _reloj);

            var request = new CrearReservaRequest(null, null, null, null);

            // Act
            var resultado =
                await servicio.CrearAsync(27, request);

            // Assert
            Assert.False(resultado.Exitoso);

            await repositorio
                .DidNotReceive()
                .ObtenerActivasPorLaboratorioYFechaAsync(
                    Arg.Any<int>(),
                    Arg.Any<DateTime>());
        }

        // HU6 - AB#86: el conflicto se informa con un único error y el mensaje exacto.
        [Fact]
        public async Task CrearAsync_Conflicto_DevuelveSoloErrorConflictoConMensajeExacto()
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            repositorio
                .ObtenerActivasPorLaboratorioYFechaAsync(3, Dia)
                .Returns(ReservasExistentes());

            var servicio =
                new ReservaService(repositorio, _reloj);

            // Act
            var resultado =
                await servicio.CrearAsync(27, Solicitud(10, 12));

            // Assert
            Assert.False(resultado.Exitoso);
            Assert.Null(resultado.Reserva);

            var error =
                Assert.Single(resultado.Errores);

            Assert.Equal("conflicto", error.Key);

            Assert.Equal(
                "El laboratorio ya tiene una reserva que se traslapa con ese horario.",
                error.Value);

            await repositorio
                .DidNotReceive()
                .CrearAsync(Arg.Any<Reserva>());
        }

        // HU6 - AB#86: se revisan todas las reservas del día, no solo la primera.
        [Fact]
        public async Task CrearAsync_ConflictoConSegundaReservaDelDia_NoGuardaReserva()
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            repositorio
                .ObtenerActivasPorLaboratorioYFechaAsync(3, Dia)
                .Returns(new List<Reserva>
                {
                    new()
                    {
                        LaboratorioId = 3,
                        UsuarioId = 5,
                        Fecha = Dia,
                        HoraInicio = new TimeSpan(8, 0, 0),
                        HoraFin = new TimeSpan(9, 0, 0),
                        Estado = "Activa"
                    },
                    new()
                    {
                        LaboratorioId = 3,
                        UsuarioId = 6,
                        Fecha = Dia,
                        HoraInicio = new TimeSpan(13, 0, 0),
                        HoraFin = new TimeSpan(15, 0, 0),
                        Estado = "Activa"
                    }
                });

            var servicio =
                new ReservaService(repositorio, _reloj);

            // Act
            var resultado =
                await servicio.CrearAsync(27, Solicitud(14, 16));

            // Assert
            Assert.False(resultado.Exitoso);

            Assert.Equal(
                ReservaService.MensajeConflicto,
                resultado.Errores["conflicto"]);

            await repositorio
                .DidNotReceive()
                .CrearAsync(Arg.Any<Reserva>());
        }

        // HU6 - AB#87: una reserva que termina justo cuando empieza la existente
        // o que está separada de ella no genera conflicto.
        [Theory]
        [InlineData(8, 9)]   // consecutiva antes de la existente (09:00 a 11:00)
        [InlineData(12, 13)] // separada de la existente
        public async Task CrearAsync_ReservaSinTraslape_GuardaReserva(
            int inicio,
            int fin)
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            repositorio
                .ObtenerActivasPorLaboratorioYFechaAsync(3, Dia)
                .Returns(ReservasExistentes());

            repositorio
                .CrearAsync(Arg.Any<Reserva>())
                .Returns(llamada => llamada.Arg<Reserva>());

            var servicio =
                new ReservaService(repositorio, _reloj);

            // Act
            var resultado =
                await servicio.CrearAsync(27, Solicitud(inicio, fin));

            // Assert
            Assert.True(resultado.Exitoso);

            await repositorio
                .Received(1)
                .CrearAsync(Arg.Any<Reserva>());
        }

        // HU6 - AB#88: crea en SQLite en memoria un usuario y dos laboratorios, y una
        // reserva existente de 09:00 a 11:00 en el primer laboratorio.
        private static async Task<(int UsuarioId, int LaboratorioA, int LaboratorioB)>
            PrepararBaseDatosAsync(
                BaseDatosEnMemoria baseDatos,
                string estadoExistente,
                int diasDesplazamiento)
        {
            using var contexto =
                baseDatos.CrearContexto();

            var usuario = new Usuario
            {
                Nombre = "Usuario responsable",
                Username = "responsable",
                PasswordHash = "hash",
                Rol = "Usuario"
            };

            var laboratorioA = new Laboratorio
            {
                Nombre = "Laboratorio de Software",
                Ubicacion = "Edificio A",
                Capacidad = 30,
                Estado = "Habilitado"
            };

            var laboratorioB = new Laboratorio
            {
                Nombre = "Laboratorio de Redes",
                Ubicacion = "Edificio B",
                Capacidad = 20,
                Estado = "Habilitado"
            };

            contexto.Usuarios.Add(usuario);
            contexto.Laboratorios.AddRange(laboratorioA, laboratorioB);
            await contexto.SaveChangesAsync();

            contexto.Reservas.Add(new Reserva
            {
                LaboratorioId = laboratorioA.Id,
                UsuarioId = usuario.Id,
                Fecha = Dia.AddDays(diasDesplazamiento),
                HoraInicio = new TimeSpan(9, 0, 0),
                HoraFin = new TimeSpan(11, 0, 0),
                Estado = estadoExistente
            });

            await contexto.SaveChangesAsync();

            return (usuario.Id, laboratorioA.Id, laboratorioB.Id);
        }

        // HU6 - AB#88: con el repositorio real, solo una reserva activa del mismo
        // laboratorio y fecha bloquea el horario; las demás no generan conflicto.
        [Theory]
        [InlineData(true, "Activa", 0)]     // otro laboratorio, mismo horario
        [InlineData(false, "Cancelada", 0)] // reserva cancelada en el mismo laboratorio
        [InlineData(false, "Activa", 1)]    // mismo laboratorio, otra fecha
        public async Task CrearAsync_ConRepositorioSqlite_ReservaQueNoAplica_GuardaReserva(
            bool otroLaboratorio,
            string estadoExistente,
            int diasDesplazamiento)
        {
            // Arrange
            using var baseDatos = new BaseDatosEnMemoria();

            var (usuarioId, laboratorioA, laboratorioB) =
                await PrepararBaseDatosAsync(
                    baseDatos,
                    estadoExistente,
                    diasDesplazamiento);

            var laboratorioSolicitado =
                otroLaboratorio ? laboratorioB : laboratorioA;

            using var contexto =
                baseDatos.CrearContexto();

            var servicio = new ReservaService(
                new ReservaRepository(contexto),
                _reloj);

            var request = new CrearReservaRequest(
                laboratorioSolicitado,
                DateOnly.FromDateTime(Dia),
                new TimeSpan(9, 0, 0),
                new TimeSpan(11, 0, 0));

            // Act
            var resultado =
                await servicio.CrearAsync(usuarioId, request);

            // Assert
            Assert.True(resultado.Exitoso);
            Assert.NotNull(resultado.Reserva);
            Assert.True(resultado.Reserva.Id > 0);

            using var contextoConsulta =
                baseDatos.CrearContexto();

            Assert.Equal(
                2,
                await contextoConsulta.Reservas.CountAsync());
        }

        // HU6 - AB#88: control con el repositorio real; una reserva activa del mismo
        // laboratorio y fecha sí genera conflicto y no se guarda la nueva.
        [Fact]
        public async Task CrearAsync_ConRepositorioSqlite_ReservaActivaMismoLaboratorio_RechazaConflicto()
        {
            // Arrange
            using var baseDatos = new BaseDatosEnMemoria();

            var (usuarioId, laboratorioA, _) =
                await PrepararBaseDatosAsync(
                    baseDatos,
                    "Activa",
                    0);

            using var contexto =
                baseDatos.CrearContexto();

            var servicio = new ReservaService(
                new ReservaRepository(contexto),
                _reloj);

            var request = new CrearReservaRequest(
                laboratorioA,
                DateOnly.FromDateTime(Dia),
                new TimeSpan(10, 0, 0),
                new TimeSpan(12, 0, 0));

            // Act
            var resultado =
                await servicio.CrearAsync(usuarioId, request);

            // Assert
            Assert.False(resultado.Exitoso);

            Assert.Equal(
                ReservaService.MensajeConflicto,
                resultado.Errores["conflicto"]);

            using var contextoConsulta =
                baseDatos.CrearContexto();

            Assert.Equal(
                1,
                await contextoConsulta.Reservas.CountAsync());
        }
    }
}

