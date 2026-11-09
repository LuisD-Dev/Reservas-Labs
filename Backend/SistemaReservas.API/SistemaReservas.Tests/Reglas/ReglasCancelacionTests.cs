using SistemaReservas.API.Reglas;

namespace SistemaReservas.Tests.Reglas
{
    public class ReglasCancelacionTests
    {
        [Theory]
        [InlineData(5, 5, false, true)]   // el propietario cancela la suya
        [InlineData(5, 27, false, false)] // otro usuario no puede
        [InlineData(5, 27, true, true)]   // el administrador cancela cualquiera
        [InlineData(5, 5, true, true)]    // el administrador también puede cancelar la suya
        public void PuedeCancelar_SegunPropietarioYRol_DevuelveResultadoEsperado(
            int usuarioDeLaReserva,
            int usuarioSolicitante,
            bool esAdministrador,
            bool esperado)
        {
            // Act
            var resultado =
                ReglasCancelacion.PuedeCancelar(
                    usuarioDeLaReserva,
                    usuarioSolicitante,
                    esAdministrador);

            // Assert
            Assert.Equal(esperado, resultado);
        }
    }
}