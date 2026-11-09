using SistemaReservas.Domain.Rules;

namespace SistemaReservas.Tests.Reglas
{
    public class ReglasEstadoLaboratorioTests
    {
        [Theory]
        [InlineData("Habilitado", true)]
        [InlineData("Fuera de servicio", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("   ", false)]
        [InlineData("Disponible", false)]
        [InlineData("habilitado", false)]
        [InlineData(" Habilitado ", false)]
        public void EsEstadoValido_ExigeValoresCanonicos(string? estado, bool esperado)
        {
            Assert.Equal(esperado, ReglasEstadoLaboratorio.EsEstadoValido(estado));
        }
    }
}
