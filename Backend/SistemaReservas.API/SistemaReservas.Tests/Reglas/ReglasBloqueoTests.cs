using SistemaReservas.API.Reglas;

namespace SistemaReservas.Tests.Reglas
{
    // NFR2 - Pruebas unitarias de ReglasBloqueo (HU1 - #20 y #21).
    // La hora actual se pasa fija, sin depender del reloj del sistema.
    public class ReglasBloqueoTests
    {
        private static readonly DateTime Ahora = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Constantes_CincoIntentosYCincoMinutos()
        {
            Assert.Equal(5, ReglasBloqueo.MaxIntentosFallidos);
            Assert.Equal(TimeSpan.FromMinutes(5), ReglasBloqueo.DuracionBloqueo);
        }

        [Fact]
        public void EstaBloqueado_SinFechaDeBloqueo_DevuelveFalse()
        {
            Assert.False(ReglasBloqueo.EstaBloqueado(null, Ahora));
        }

        [Fact]
        public void EstaBloqueado_ConFechaFutura_DevuelveTrue()
        {
            Assert.True(ReglasBloqueo.EstaBloqueado(Ahora.AddSeconds(1), Ahora));
        }

        [Fact]
        public void EstaBloqueado_ConFechaPasada_DevuelveFalse()
        {
            Assert.False(ReglasBloqueo.EstaBloqueado(Ahora.AddSeconds(-1), Ahora));
        }

        [Fact]
        public void EstaBloqueado_JustoAlVencer_DevuelveFalse()
        {
            Assert.False(ReglasBloqueo.EstaBloqueado(Ahora, Ahora));
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(4, false)]
        [InlineData(5, true)]
        [InlineData(6, true)]
        public void DebeBloquear_SegunIntentosFallidos(int intentosFallidos, bool esperado)
        {
            Assert.Equal(esperado, ReglasBloqueo.DebeBloquear(intentosFallidos));
        }

        [Fact]
        public void CalcularBloqueadoHasta_SumaCincoMinutos()
        {
            Assert.Equal(Ahora.AddMinutes(5), ReglasBloqueo.CalcularBloqueadoHasta(Ahora));
        }

        [Theory]
        [InlineData(0, false, false)]
        [InlineData(2, false, true)]
        [InlineData(0, true, true)]
        public void DebeReiniciarIntentos_SoloSiHayIntentosOBloqueo(
            int intentosFallidos,
            bool tieneBloqueo,
            bool esperado)
        {
            DateTime? bloqueadoHasta = tieneBloqueo ? Ahora.AddMinutes(-1) : null;

            Assert.Equal(esperado, ReglasBloqueo.DebeReiniciarIntentos(intentosFallidos, bloqueadoHasta));
        }

        [Fact]
        public void SegundosRestantes_CincoMinutos_Devuelve300()
        {
            Assert.Equal(300, ReglasBloqueo.SegundosRestantes(Ahora.AddMinutes(5), Ahora));
        }

        [Fact]
        public void SegundosRestantes_ConFraccionDeSegundo_RedondeaHaciaArriba()
        {
            Assert.Equal(1, ReglasBloqueo.SegundosRestantes(Ahora.AddMilliseconds(200), Ahora));
        }

        [Fact]
        public void SegundosRestantes_BloqueoVencido_DevuelveCero()
        {
            Assert.Equal(0, ReglasBloqueo.SegundosRestantes(Ahora.AddSeconds(-30), Ahora));
        }
    }
}
