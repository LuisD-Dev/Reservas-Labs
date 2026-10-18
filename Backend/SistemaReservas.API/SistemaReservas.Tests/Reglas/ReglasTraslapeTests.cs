using SistemaReservas.API.Reglas;

namespace SistemaReservas.Tests.Reglas
{
    public class ReglasTraslapeTests
    {
        [Theory]
        [InlineData(9, 11, 10, 12, true)]   // parcial: B empieza dentro de A
        [InlineData(10, 12, 9, 11, true)]   // parcial: B termina dentro de A
        [InlineData(9, 12, 10, 11, true)]   // B contenida en A
        [InlineData(10, 11, 9, 12, true)]   // A contenida en B
        [InlineData(9, 10, 9, 10, true)]    // idénticas
        [InlineData(9, 10, 10, 11, false)]  // consecutivas: A termina cuando B empieza
        [InlineData(10, 11, 9, 10, false)]  // consecutivas: B termina cuando A empieza
        [InlineData(8, 9, 11, 12, false)]   // separadas
        public void HayTraslape_RangosDeHoras_DevuelveResultadoEsperado(
            int inicioA,
            int finA,
            int inicioB,
            int finB,
            bool esperado)
        {
            // Arrange
            var iniA = new TimeSpan(inicioA, 0, 0);
            var terA = new TimeSpan(finA, 0, 0);
            var iniB = new TimeSpan(inicioB, 0, 0);
            var terB = new TimeSpan(finB, 0, 0);

            // Act
            var resultado =
                ReglasTraslape.HayTraslape(
                    iniA,
                    terA,
                    iniB,
                    terB);

            // Assert
            Assert.Equal(esperado, resultado);
        }

        [Fact]
        public void HayTraslape_UnMinutoDeSolape_RetornaTrue()
        {
            // Arrange
            var iniA = new TimeSpan(9, 0, 0);
            var terA = new TimeSpan(10, 0, 0);
            var iniB = new TimeSpan(9, 59, 0);
            var terB = new TimeSpan(11, 0, 0);

            // Act
            var resultado =
                ReglasTraslape.HayTraslape(
                    iniA,
                    terA,
                    iniB,
                    terB);

            // Assert
            Assert.True(resultado);
        }
    }
}
