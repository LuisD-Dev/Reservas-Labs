using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Microsoft.Extensions.Time.Testing;
using SistemaReservas.API.DTOs;
using SistemaReservas.API.Models;
using SistemaReservas.API.Reglas;
using SistemaReservas.API.Repositories;
using SistemaReservas.API.Services;

namespace SistemaReservas.Tests.Services
{
    public class ReservaServiceTests
    {
        private readonly FakeTimeProvider _reloj = new(
            new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));

        [Fact]
        public async Task CrearAsync_FechaPasada_NoGuardaReserva()
        {
            // Arrange
            var repositorio = Substitute.For<IReservaRepository>();
            var servicio = new ReservaService(repositorio, _reloj);
            var request = new CrearReservaRequest(
                3,
                new DateOnly(2026, 10, 16),
                new TimeSpan(9, 0, 0),
                new TimeSpan(11, 0, 0));

            // Act
            var resultado = await servicio.CrearAsync(27, request);

            // Assert
            Assert.False(resultado.Exitoso);
            Assert.Null(resultado.Reserva);
            Assert.Equal(
                "La fecha no puede ser anterior a hoy.",
                resultado.Errores["fecha"]);
            await repositorio
                .DidNotReceive()
                .CrearAsync(Arg.Any<Reserva>());
        }

        [Fact]
        public async Task CrearAsync_DatosValidos_UsaUsuarioAutenticado()
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            repositorio
                .CrearAsync(Arg.Any<Reserva>())
                .Returns(llamada =>
                {
                    var reserva =
                        llamada.Arg<Reserva>();

                    reserva.Id = 10;

                    return reserva;
                });

            var servicio =
                new ReservaService(repositorio, _reloj);

            var request = new CrearReservaRequest(
                3,
                new DateOnly(2026, 10, 20),
                new TimeSpan(9, 0, 0),
                new TimeSpan(11, 0, 0));

            const int usuarioAutenticadoId = 27;

            // Act
            var resultado =
                await servicio.CrearAsync(
                    usuarioAutenticadoId,
                    request);

            // Assert
            Assert.True(resultado.Exitoso);
            Assert.NotNull(resultado.Reserva);
            Assert.Equal(10, resultado.Reserva.Id);
            Assert.Equal(
                usuarioAutenticadoId,
                resultado.Reserva.UsuarioId);

            await repositorio
                .Received(1)
                .CrearAsync(
                    Arg.Is<Reserva>(reserva =>
                        reserva.UsuarioId ==
                            usuarioAutenticadoId &&
                        reserva.LaboratorioId == 3 &&
                        reserva.Fecha ==
                            new DateTime(2026, 10, 20) &&
                        reserva.HoraInicio ==
                            new TimeSpan(9, 0, 0) &&
                        reserva.HoraFin ==
                            new TimeSpan(11, 0, 0) &&
                        reserva.Estado == "Activa"));
        }

        [Theory]
        [InlineData(9, 9)]
        [InlineData(10, 9)]
        public async Task CrearAsync_HorarioInvalido_NoGuardaReserva(
            int horaInicio,
            int horaFin)
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            var servicio =
                new ReservaService(repositorio, _reloj);

            var request = new CrearReservaRequest(
                3,
                new DateOnly(2026, 10, 20),
                new TimeSpan(horaInicio, 0, 0),
                new TimeSpan(horaFin, 0, 0));

            // Act
            var resultado =
                await servicio.CrearAsync(27, request);

            // Assert
            Assert.False(resultado.Exitoso);
            Assert.Null(resultado.Reserva);
            Assert.Equal(
                ReglasReserva.MensajeHoraFinInvalida,
                resultado.Errores["horaFin"]);

            await repositorio
                .DidNotReceive()
                .CrearAsync(Arg.Any<Reserva>());
        }

        [Fact]
        public async Task CrearAsync_DatosIncompletos_NoGuardaReserva()
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            var servicio =
                new ReservaService(repositorio, _reloj);

            var request = new CrearReservaRequest(
                null,
                null,
                null,
                null);

            // Act
            var resultado =
                await servicio.CrearAsync(
                    27,
                    request);

            // Assert
            Assert.False(resultado.Exitoso);
            Assert.Null(resultado.Reserva);
            Assert.Equal(4, resultado.Errores.Count);

            await repositorio
                .DidNotReceive()
                .CrearAsync(Arg.Any<Reserva>());
        }

        // HU4 - AB#80: con varios campos faltantes se informa el error de cada uno
        // y no se consulta conflictos ni se guarda.
        [Fact]
        public async Task CrearAsync_VariosCamposFaltantes_DevuelveErrorDeCadaCampo()
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            var servicio =
                new ReservaService(repositorio, _reloj);

            var request = new CrearReservaRequest(
                3,
                null,
                null,
                new TimeSpan(11, 0, 0));

            // Act
            var resultado =
                await servicio.CrearAsync(27, request);

            // Assert
            Assert.False(resultado.Exitoso);
            Assert.Null(resultado.Reserva);
            Assert.Equal(2, resultado.Errores.Count);

            Assert.Equal(
                "Ingresa la fecha de la reserva.",
                resultado.Errores["fecha"]);

            Assert.Equal(
                "Ingresa la hora inicial.",
                resultado.Errores["horaInicio"]);

            Assert.Empty(
                repositorio.ReceivedCalls());
        }

        // HU4 - AB#80: laboratorio nulo, cero o negativo es un dato inválido.
        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(-3)]
        public async Task CrearAsync_LaboratorioInvalido_NoConsultaConflictosNiGuarda(
            int? laboratorioId)
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            var servicio =
                new ReservaService(repositorio, _reloj);

            var request = new CrearReservaRequest(
                laboratorioId,
                new DateOnly(2026, 10, 20),
                new TimeSpan(9, 0, 0),
                new TimeSpan(11, 0, 0));

            // Act
            var resultado =
                await servicio.CrearAsync(27, request);

            // Assert
            Assert.False(resultado.Exitoso);

            var error =
                Assert.Single(resultado.Errores);

            Assert.Equal("laboratorioId", error.Key);
            Assert.Equal("Selecciona un laboratorio.", error.Value);

            await repositorio
                .DidNotReceive()
                .ObtenerActivasPorLaboratorioYFechaAsync(
                    Arg.Any<int>(),
                    Arg.Any<DateTime>());

            await repositorio
                .DidNotReceive()
                .CrearAsync(Arg.Any<Reserva>());
        }

        // HU4 - AB#78: el servicio no captura excepciones del repositorio. Si la BD
        // rechaza la reserva (por ejemplo, laboratorio inexistente por la FK), la
        // excepción se propaga al controlador y hoy la API responde 500.
        [Fact]
        public async Task CrearAsync_RepositorioFalla_PropagaExcepcion()
        {
            // Arrange
            var excepcion =
                new InvalidOperationException(
                    "No se pudo guardar la reserva.");

            var repositorio =
                Substitute.For<IReservaRepository>();

            repositorio
                .ObtenerActivasPorLaboratorioYFechaAsync(
                    Arg.Any<int>(),
                    Arg.Any<DateTime>())
                .Returns(new List<Reserva>());

            repositorio
                .CrearAsync(Arg.Any<Reserva>())
                .ThrowsAsync(excepcion);

            var servicio =
                new ReservaService(repositorio, _reloj);

            var request = new CrearReservaRequest(
                3,
                new DateOnly(2026, 10, 20),
                new TimeSpan(9, 0, 0),
                new TimeSpan(11, 0, 0));

            // Act
            var lanzada =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => servicio.CrearAsync(27, request));

            // Assert
            Assert.Same(excepcion, lanzada);
        }

        [Theory]
        [InlineData(6, 8)]
        [InlineData(20, 22)]
        public async Task CrearAsync_FueraDelHorarioDeFuncionamiento_NoGuardaReserva(
            int horaInicio,
            int horaFin)
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            var servicio =
                new ReservaService(repositorio, _reloj);

            var request = new CrearReservaRequest(
                3,
                new DateOnly(2026, 10, 20),
                new TimeSpan(horaInicio, 0, 0),
                new TimeSpan(horaFin, 0, 0));

            // Act
            var resultado =
                await servicio.CrearAsync(27, request);

            // Assert
            Assert.False(resultado.Exitoso);
            Assert.Null(resultado.Reserva);
            Assert.Equal(
                ReglasReserva.MensajeFueraDeHorario,
                resultado.Errores["horaFin"]);

            await repositorio
                .DidNotReceive()
                .CrearAsync(Arg.Any<Reserva>());
        }

        [Fact]
        public async Task CrearAsync_DuracionMayorAlMaximo_NoGuardaReserva()
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            var servicio =
                new ReservaService(repositorio, _reloj);

            var request = new CrearReservaRequest(
                3,
                new DateOnly(2026, 10, 20),
                new TimeSpan(9, 0, 0),
                new TimeSpan(13, 1, 0));

            // Act
            var resultado =
                await servicio.CrearAsync(27, request);

            // Assert
            Assert.False(resultado.Exitoso);
            Assert.Null(resultado.Reserva);
            Assert.Equal(
                ReglasReserva.MensajeDuracionExcedida,
                resultado.Errores["horaFin"]);

            await repositorio
                .DidNotReceive()
                .CrearAsync(Arg.Any<Reserva>());
        }

        [Fact]
        public async Task CrearAsync_ReservaHastaElCierreConDuracionMaxima_GuardaReserva()
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            repositorio
                .CrearAsync(Arg.Any<Reserva>())
                .Returns(llamada => llamada.Arg<Reserva>());

            var servicio =
                new ReservaService(repositorio, _reloj);

            var request = new CrearReservaRequest(
                3,
                new DateOnly(2026, 10, 20),
                ReglasReserva.HoraCierre - ReglasReserva.DuracionMaxima,
                ReglasReserva.HoraCierre);

            // Act
            var resultado =
                await servicio.CrearAsync(27, request);

            // Assert
            Assert.True(resultado.Exitoso);
            Assert.NotNull(resultado.Reserva);

            await repositorio
                .Received(1)
                .CrearAsync(Arg.Any<Reserva>());
        }

        // Repositorio sin reservas previas que devuelve la reserva que recibe.
        private static IReservaRepository RepositorioQueGuarda()
        {
            var repositorio =
                Substitute.For<IReservaRepository>();

            repositorio
                .ObtenerActivasPorLaboratorioYFechaAsync(
                    Arg.Any<int>(),
                    Arg.Any<DateTime>())
                .Returns(new List<Reserva>());

            repositorio
                .CrearAsync(Arg.Any<Reserva>())
                .Returns(llamada => llamada.Arg<Reserva>());

            return repositorio;
        }

        // HU5 - AB#82: valores límite al minuto del horario de 07:00 a 21:00.
        [Theory]
        [InlineData(6, 59, 8, 0, false)]  // empieza un minuto antes de abrir
        [InlineData(7, 0, 8, 0, true)]    // empieza justo al abrir
        [InlineData(20, 0, 21, 0, true)]  // termina justo al cerrar
        [InlineData(20, 0, 21, 1, false)] // termina un minuto después de cerrar
        public async Task CrearAsync_LimitesDelHorario_DevuelveResultadoEsperado(
            int horaInicio,
            int minutoInicio,
            int horaFin,
            int minutoFin,
            bool esValida)
        {
            // Arrange
            var repositorio = RepositorioQueGuarda();

            var servicio =
                new ReservaService(repositorio, _reloj);

            var request = new CrearReservaRequest(
                3,
                new DateOnly(2026, 10, 20),
                new TimeSpan(horaInicio, minutoInicio, 0),
                new TimeSpan(horaFin, minutoFin, 0));

            // Act
            var resultado =
                await servicio.CrearAsync(27, request);

            // Assert
            Assert.Equal(esValida, resultado.Exitoso);

            if (esValida)
            {
                await repositorio
                    .Received(1)
                    .CrearAsync(Arg.Any<Reserva>());
            }
            else
            {
                Assert.Equal(
                    ReglasReserva.MensajeFueraDeHorario,
                    resultado.Errores["horaFin"]);

                await repositorio
                    .DidNotReceive()
                    .CrearAsync(Arg.Any<Reserva>());
            }
        }

        // HU5 - AB#82: si la hora final es menor que la inicial no se agrega el
        // mensaje de horario aunque el rango también esté fuera del horario.
        [Fact]
        public async Task CrearAsync_HoraFinMenorYFueraDeHorario_SoloInformaHoraFinInvalida()
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            var servicio =
                new ReservaService(repositorio, _reloj);

            var request = new CrearReservaRequest(
                3,
                new DateOnly(2026, 10, 20),
                new TimeSpan(22, 0, 0),
                new TimeSpan(21, 30, 0));

            // Act
            var resultado =
                await servicio.CrearAsync(27, request);

            // Assert
            var error =
                Assert.Single(resultado.Errores);

            Assert.Equal("horaFin", error.Key);
            Assert.Equal(ReglasReserva.MensajeHoraFinInvalida, error.Value);

            Assert.Empty(
                repositorio.ReceivedCalls());
        }

        // HU5 - AB#82: sin hora inicial solo se informa el campo faltante; el
        // horario de funcionamiento no se evalúa para no mezclar mensajes.
        [Fact]
        public async Task CrearAsync_SinHoraInicioYFueraDeHorario_SoloInformaCampoFaltante()
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            var servicio =
                new ReservaService(repositorio, _reloj);

            var request = new CrearReservaRequest(
                3,
                new DateOnly(2026, 10, 20),
                null,
                new TimeSpan(22, 0, 0));

            // Act
            var resultado =
                await servicio.CrearAsync(27, request);

            // Assert
            var error =
                Assert.Single(resultado.Errores);

            Assert.Equal("horaInicio", error.Key);
            Assert.Equal("Ingresa la hora inicial.", error.Value);

            Assert.Empty(
                repositorio.ReceivedCalls());
        }

        // HU5 - AB#83: con el reloj en 2026-10-17, hoy y mañana se aceptan.
        [Theory]
        [InlineData(17)]
        [InlineData(18)]
        public async Task CrearAsync_FechaHoyOManana_GuardaReserva(
            int dia)
        {
            // Arrange
            var repositorio = RepositorioQueGuarda();

            var servicio =
                new ReservaService(repositorio, _reloj);

            var request = new CrearReservaRequest(
                3,
                new DateOnly(2026, 10, dia),
                new TimeSpan(9, 0, 0),
                new TimeSpan(11, 0, 0));

            // Act
            var resultado =
                await servicio.CrearAsync(27, request);

            // Assert
            Assert.True(resultado.Exitoso);

            await repositorio
                .Received(1)
                .CrearAsync(
                    Arg.Is<Reserva>(reserva =>
                        reserva.Fecha == new DateTime(2026, 10, dia)));
        }

        // HU5 - AB#83: el servicio usa la hora local del TimeProvider. A las 03:00 UTC
        // del 18 todavía son las 21:00 del 17 en UTC-6, así que el 17 es "hoy".
        [Theory]
        [InlineData(17, true)]
        [InlineData(16, false)]
        public async Task CrearAsync_CercaDeMedianoche_UsaFechaLocal(
            int dia,
            bool esValida)
        {
            // Arrange
            var reloj = new FakeTimeProvider(
                new DateTimeOffset(2026, 10, 18, 3, 0, 0, TimeSpan.Zero));

            reloj.SetLocalTimeZone(
                TimeZoneInfo.CreateCustomTimeZone(
                    "UTC-06",
                    TimeSpan.FromHours(-6),
                    "UTC-06",
                    "UTC-06"));

            var repositorio = RepositorioQueGuarda();

            var servicio =
                new ReservaService(repositorio, reloj);

            var request = new CrearReservaRequest(
                3,
                new DateOnly(2026, 10, dia),
                new TimeSpan(9, 0, 0),
                new TimeSpan(11, 0, 0));

            // Act
            var resultado =
                await servicio.CrearAsync(27, request);

            // Assert
            Assert.Equal(esValida, resultado.Exitoso);

            if (!esValida)
            {
                Assert.Equal(
                    ReglasReserva.MensajeFechaPasada,
                    resultado.Errores["fecha"]);
            }
        }

        // HU5 - AB#84: 3:59 y 4:00 están dentro de la duración máxima.
        [Theory]
        [InlineData(12, 59)]
        [InlineData(13, 0)]
        public async Task CrearAsync_DuracionLimite_GuardaReserva(
            int horaFin,
            int minutoFin)
        {
            // Arrange
            var repositorio = RepositorioQueGuarda();

            var servicio =
                new ReservaService(repositorio, _reloj);

            var request = new CrearReservaRequest(
                3,
                new DateOnly(2026, 10, 20),
                new TimeSpan(9, 0, 0),
                new TimeSpan(horaFin, minutoFin, 0));

            // Act
            var resultado =
                await servicio.CrearAsync(27, request);

            // Assert
            Assert.True(resultado.Exitoso);

            await repositorio
                .Received(1)
                .CrearAsync(Arg.Any<Reserva>());
        }

        // HU5 - AB#84: si el rango sale del horario, el mensaje es el de horario
        // aunque también exceda (o alcance justo) la duración máxima.
        [Theory]
        [InlineData(6, 11)]  // 5 horas y empieza antes de abrir
        [InlineData(18, 22)] // 4 horas y termina después de cerrar
        public async Task CrearAsync_DuracionCombinadaConHorario_DevuelveMensajeHorario(
            int horaInicio,
            int horaFin)
        {
            // Arrange
            var repositorio =
                Substitute.For<IReservaRepository>();

            var servicio =
                new ReservaService(repositorio, _reloj);

            var request = new CrearReservaRequest(
                3,
                new DateOnly(2026, 10, 20),
                new TimeSpan(horaInicio, 0, 0),
                new TimeSpan(horaFin, 0, 0));

            // Act
            var resultado =
                await servicio.CrearAsync(27, request);

            // Assert
            var error =
                Assert.Single(resultado.Errores);

            Assert.Equal("horaFin", error.Key);
            Assert.Equal(ReglasReserva.MensajeFueraDeHorario, error.Value);

            await repositorio
                .DidNotReceive()
                .CrearAsync(Arg.Any<Reserva>());
        }
    }
}
