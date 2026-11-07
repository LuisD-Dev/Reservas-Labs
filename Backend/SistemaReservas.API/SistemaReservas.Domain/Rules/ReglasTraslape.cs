namespace SistemaReservas.API.Reglas
{
    // HU6 - AB#74: detección de traslapes entre rangos de hora.
    // Regla pura: no usa base de datos, HTTP ni reloj.
    public static class ReglasTraslape
    {
        // Dos rangos se traslapan si cada uno empieza antes de que el otro termine.
        // Con "<" estricto, dos reservas consecutivas (una termina justo cuando
        // la otra empieza) no chocan.
        public static bool HayTraslape(
            TimeSpan inicioA,
            TimeSpan finA,
            TimeSpan inicioB,
            TimeSpan finB)
        {
            return inicioA < finB && inicioB < finA;
        }
    }
}
