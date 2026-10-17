using SistemaReservas.API.Reglas;

namespace SistemaReservas.Tests.Reglas
{
    public class ReglasReservaTests
    {
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
            var error = Assert.Single(errores);

            Assert.Equal("horaFin", error.Key);
            Assert.Equal(
                "Ingresa la hora final.",
                error.Value);
        }
    }
}
