namespace SistemaReservas.API.Reglas
{
    // NFR2 - Reglas de negocio aisladas: bloqueo temporal del login (HU1).
    // No usan base de datos, HTTP ni reloj: la hora actual llega como parámetro.
    public static class ReglasBloqueo
    {
        // HU1 - #21 Bloqueo temporal: 5 intentos fallidos => 5 minutos bloqueado
        public const int MaxIntentosFallidos = 5;
        public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(5);

        // HU1 - #21 El usuario sigue bloqueado mientras la fecha de bloqueo no haya pasado.
        public static bool EstaBloqueado(DateTime? bloqueadoHasta, DateTime ahoraUtc)
        {
            return bloqueadoHasta is not null && bloqueadoHasta > ahoraUtc;
        }

        // HU1 - #21 Al llegar al 5.º intento fallido se bloquea el acceso.
        public static bool DebeBloquear(int intentosFallidos)
        {
            return intentosFallidos >= MaxIntentosFallidos;
        }

        // HU1 - #21 Fecha hasta la que queda bloqueado a partir de ahora.
        public static DateTime CalcularBloqueadoHasta(DateTime ahoraUtc)
        {
            return ahoraUtc.Add(DuracionBloqueo);
        }

        // Tras un login exitoso solo hay que limpiar si había intentos o un bloqueo vencido.
        public static bool DebeReiniciarIntentos(int intentosFallidos, DateTime? bloqueadoHasta)
        {
            return intentosFallidos > 0 || bloqueadoHasta is not null;
        }

        // Segundos que faltan para que venza el bloqueo, redondeados hacia arriba y nunca negativos.
        public static int SegundosRestantes(DateTime bloqueadoHastaUtc, DateTime ahoraUtc)
        {
            var segundosRestantes = (int)Math.Ceiling((bloqueadoHastaUtc - ahoraUtc).TotalSeconds);
            return Math.Max(segundosRestantes, 0);
        }
    }
}
