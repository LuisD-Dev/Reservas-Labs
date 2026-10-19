using SistemaReservas.API.Reglas;

namespace SistemaReservas.Tests.Reglas
{
    public class ReglasReservaTests
    {
        [Theory]
        [InlineData(16, ReglasReserva.MensajeFechaPasada)]
        [InlineData(17, null)]
        [InlineData(18, null)]
        public void ValidarFechaReserva_AyerHoyYManana_DevuelveResultadoEsperado(
            int dia,
            string? esperado)
        {
            var resultado = ReglasReserva.ValidarFechaReserva(
                new DateOnly(2026, 10, dia),
                new DateOnly(2026, 10, 17));

            Assert.Equal(esperado, resultado);
        }

        private static readonly DateOnly Fecha =
            new(2026, 10, 20);

        private static readonly TimeSpan Nueve =
            new(9, 0, 0);

        private static readonly TimeSpan Once =
            new(11, 0, 0);

        [Fact]
        public void ValidarCamposObligatorios_SinDatos_ExigeTodosLosCampos()
        {
            // Act
            var errores =
                ReglasReserva.ValidarCamposObligatorios(
                    null,
                    null,
                    null,
                    null);

            // Assert
            Assert.Equal(4, errores.Count);
            Assert.Equal(
                "Selecciona un laboratorio.",
                errores["laboratorioId"]);
            Assert.Equal(
                "Ingresa la fecha de la reserva.",
                errores["fecha"]);
            Assert.Equal(
                "Ingresa la hora inicial.",
                errores["horaInicio"]);
            Assert.Equal(
                "Ingresa la hora final.",
                errores["horaFin"]);
        }

        [Fact]
        public void ValidarCamposObligatorios_DatosCompletos_SinErrores()
        {
            // Act
            var errores =
                ReglasReserva.ValidarCamposObligatorios(
                    1,
                    Fecha,
                    Nueve,
                    Once);

            // Assert
            Assert.Empty(errores);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ValidarCamposObligatorios_LaboratorioInvalido_DevuelveError(
            int laboratorioId)
        {
            // Act
            var errores =
                ReglasReserva.ValidarCamposObligatorios(
                    laboratorioId,
                    Fecha,
                    Nueve,
                    Once);

            // Assert
            Assert.Equal(
                "Selecciona un laboratorio.",
                Assert.Single(errores).Value);
        }

        [Fact]
        public void ValidarCamposObligatorios_SoloHoraFinalFaltante_DevuelveEseError()
        {
            // Act
            var errores =
                ReglasReserva.ValidarCamposObligatorios(
                    1,
                    Fecha,
                    Nueve,
                    null);

            // Assert
            var error =
                Assert.Single(errores);

            Assert.Equal("horaFin", error.Key);
            Assert.Equal(
                "Ingresa la hora final.",
                error.Value);
        }

        [Fact]
        public void ValidarHoraFinalizacion_HoraFinMayorQueInicio_RetornaNull()
        {
            // Arrange
            var inicio =
                new TimeSpan(8, 0, 0);

            var fin =
                new TimeSpan(10, 0, 0);

            // Act
            var resultado =
                ReglasReserva.ValidarHoraFinalizacion(
                    inicio,
                    fin);

            // Assert
            Assert.Null(resultado);
        }

        [Fact]
        public void ValidarHoraFinalizacion_SinHoraFin_RetornaMensajeRequerida()
        {
            // Arrange
            TimeSpan? inicio =
                new TimeSpan(8, 0, 0);

            TimeSpan? fin =
                null;

            // Act
            var resultado =
                ReglasReserva.ValidarHoraFinalizacion(
                    inicio,
                    fin);

            // Assert
            Assert.Equal(
                ReglasReserva.MensajeHoraFinRequerida,
                resultado);
        }

        [Fact]
        public void ValidarHoraFinalizacion_HoraFinIgualAInicio_RetornaMensajeInvalida()
        {
            // Arrange
            var hora =
                new TimeSpan(9, 0, 0);

            // Act
            var resultado =
                ReglasReserva.ValidarHoraFinalizacion(
                    hora,
                    hora);

            // Assert
            Assert.Equal(
                ReglasReserva.MensajeHoraFinInvalida,
                resultado);
        }

        [Fact]
        public void ValidarHoraFinalizacion_HoraFinMenorQueInicio_RetornaMensajeInvalida()
        {
            // Arrange
            var inicio =
                new TimeSpan(10, 0, 0);

            var fin =
                new TimeSpan(8, 0, 0);

            // Act
            var resultado =
                ReglasReserva.ValidarHoraFinalizacion(
                    inicio,
                    fin);

            // Assert
            Assert.Equal(
                ReglasReserva.MensajeHoraFinInvalida,
                resultado);
        }

        [Fact]
        public void ValidarHoraFinalizacion_SinHoraInicio_NoCompara_RetornaNull()
        {
            // Arrange
            TimeSpan? inicio =
                null;

            TimeSpan? fin =
                new TimeSpan(10, 0, 0);

            // Act
            var resultado =
                ReglasReserva.ValidarHoraFinalizacion(
                    inicio,
                    fin);

            // Assert
            Assert.Null(resultado);
        }

        [Fact]
        public void ValidarHorarioFuncionamiento_RangoDentroDelHorario_RetornaNull()
        {
            // Arrange
            var inicio =
                new TimeSpan(9, 0, 0);

            var fin =
                new TimeSpan(11, 0, 0);

            // Act
            var resultado =
                ReglasReserva.ValidarHorarioFuncionamiento(
                    inicio,
                    fin);

            // Assert
            Assert.Null(resultado);
        }

        [Fact]
        public void ValidarHorarioFuncionamiento_RangoExactoDeAperturaACierre_RetornaNull()
        {
            // Arrange
            var inicio =
                ReglasReserva.HoraApertura;

            var fin =
                ReglasReserva.HoraCierre;

            // Act
            var resultado =
                ReglasReserva.ValidarHorarioFuncionamiento(
                    inicio,
                    fin);

            // Assert
            Assert.Null(resultado);
        }

        [Fact]
        public void ValidarHorarioFuncionamiento_InicioAntesDeApertura_RetornaMensaje()
        {
            // Arrange
            var inicio =
                new TimeSpan(6, 0, 0);

            var fin =
                new TimeSpan(8, 0, 0);

            // Act
            var resultado =
                ReglasReserva.ValidarHorarioFuncionamiento(
                    inicio,
                    fin);

            // Assert
            Assert.Equal(
                ReglasReserva.MensajeFueraDeHorario,
                resultado);
        }

        [Fact]
        public void ValidarHorarioFuncionamiento_FinDespuesDelCierre_RetornaMensaje()
        {
            // Arrange
            var inicio =
                new TimeSpan(20, 0, 0);

            var fin =
                new TimeSpan(22, 0, 0);

            // Act
            var resultado =
                ReglasReserva.ValidarHorarioFuncionamiento(
                    inicio,
                    fin);

            // Assert
            Assert.Equal(
                ReglasReserva.MensajeFueraDeHorario,
                resultado);
        }

        [Fact]
        public void ValidarHorarioFuncionamiento_SinHoras_RetornaNull()
        {
            // Arrange
            TimeSpan? inicio =
                null;

            TimeSpan? fin =
                null;

            // Act
            var resultado =
                ReglasReserva.ValidarHorarioFuncionamiento(
                    inicio,
                    fin);

            // Assert
            Assert.Null(resultado);
        }

        // HU5 - AB#82: valores límite al minuto del horario de 07:00 a 21:00.
        [Theory]
        [InlineData(6, 59, 8, 0, false)]  // empieza un minuto antes de abrir
        [InlineData(7, 0, 8, 0, true)]    // empieza justo al abrir
        [InlineData(20, 0, 21, 0, true)]  // termina justo al cerrar
        [InlineData(20, 0, 21, 1, false)] // termina un minuto después de cerrar
        public void ValidarHorarioFuncionamiento_LimitesAlMinuto_DevuelveResultadoEsperado(
            int horaInicio,
            int minutoInicio,
            int horaFin,
            int minutoFin,
            bool esValida)
        {
            // Arrange
            var inicio =
                new TimeSpan(horaInicio, minutoInicio, 0);

            var fin =
                new TimeSpan(horaFin, minutoFin, 0);

            // Act
            var resultado =
                ReglasReserva.ValidarHorarioFuncionamiento(
                    inicio,
                    fin);

            // Assert
            Assert.Equal(
                esValida ? null : ReglasReserva.MensajeFueraDeHorario,
                resultado);
        }

        [Fact]
        public void ValidarDuracionMaxima_DuracionMenorAlMaximo_RetornaNull()
        {
            // Arrange
            var inicio =
                new TimeSpan(9, 0, 0);

            var fin =
                new TimeSpan(10, 0, 0);

            // Act
            var resultado =
                ReglasReserva.ValidarDuracionMaxima(
                    inicio,
                    fin);

            // Assert
            Assert.Null(resultado);
        }

        [Fact]
        public void ValidarDuracionMaxima_DuracionExactaAlMaximo_RetornaNull()
        {
            // Arrange
            var inicio =
                new TimeSpan(9, 0, 0);

            var fin =
                inicio + ReglasReserva.DuracionMaxima;

            // Act
            var resultado =
                ReglasReserva.ValidarDuracionMaxima(
                    inicio,
                    fin);

            // Assert
            Assert.Null(resultado);
        }

        [Fact]
        public void ValidarDuracionMaxima_UnMinutoMasDelMaximo_RetornaMensaje()
        {
            // Arrange
            var inicio =
                new TimeSpan(9, 0, 0);

            var fin =
                inicio + ReglasReserva.DuracionMaxima + TimeSpan.FromMinutes(1);

            // Act
            var resultado =
                ReglasReserva.ValidarDuracionMaxima(
                    inicio,
                    fin);

            // Assert
            Assert.Equal(
                ReglasReserva.MensajeDuracionExcedida,
                resultado);
        }

        // HU5 - AB#84: un minuto menos que el máximo de 4 horas sigue siendo válido.
        [Fact]
        public void ValidarDuracionMaxima_TresHorasCincuentaYNueveMinutos_RetornaNull()
        {
            // Arrange
            var inicio =
                new TimeSpan(9, 0, 0);

            var fin =
                new TimeSpan(12, 59, 0);

            // Act
            var resultado =
                ReglasReserva.ValidarDuracionMaxima(
                    inicio,
                    fin);

            // Assert
            Assert.Null(resultado);
        }

        [Fact]
        public void ValidarDuracionMaxima_DuracionMuyLarga_RetornaMensaje()
        {
            // Arrange
            var inicio =
                new TimeSpan(8, 0, 0);

            var fin =
                new TimeSpan(16, 0, 0);

            // Act
            var resultado =
                ReglasReserva.ValidarDuracionMaxima(
                    inicio,
                    fin);

            // Assert
            Assert.Equal(
                ReglasReserva.MensajeDuracionExcedida,
                resultado);
        }

        [Fact]
        public void ValidarDuracionMaxima_RangoInvalido_NoOpina_RetornaNull()
        {
            // Arrange
            var inicio =
                new TimeSpan(12, 0, 0);

            var fin =
                new TimeSpan(9, 0, 0);

            // Act
            var resultado =
                ReglasReserva.ValidarDuracionMaxima(
                    inicio,
                    fin);

            // Assert
            Assert.Null(resultado);
        }

        [Fact]
        public void ValidarDuracionMaxima_SinHoras_RetornaNull()
        {
            // Arrange
            TimeSpan? inicio =
                null;

            TimeSpan? fin =
                null;

            // Act
            var resultado =
                ReglasReserva.ValidarDuracionMaxima(
                    inicio,
                    fin);

            // Assert
            Assert.Null(resultado);
        }
    }
}