namespace SistemaReservas.API.Reglas
{
    // HU5 - #65 Reglas de validación de una reserva (sin BD, HTTP ni reloj).
    public static class ReglasReserva
    {
        public const string MensajeHoraFinRequerida = "Ingresa la hora final.";
        public const string MensajeHoraFinInvalida = "La hora final debe ser mayor que la hora inicial.";

        // Devuelve el mensaje de error, o null si la hora de finalización es válida.
        public static string? ValidarHoraFinalizacion(TimeSpan? horaInicio, TimeSpan? horaFin)
        {
            if (horaFin is null)
            {
                return MensajeHoraFinRequerida;
            }

            if (horaInicio is not null &&
                !ReglasDisponibilidad.EsRangoHorasValido(horaInicio.Value, horaFin.Value))
            {
                return MensajeHoraFinInvalida;
            }

            return null;
        }
    }
}