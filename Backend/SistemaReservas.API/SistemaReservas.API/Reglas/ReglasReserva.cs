namespace SistemaReservas.API.Reglas
{
    // Reglas de negocio compartidas por HU4 y HU5.
    public static class ReglasReserva
    {
        public const string MensajeHoraFinRequerida =
            "Ingresa la hora final.";

        public const string MensajeHoraFinInvalida =
            "La hora final debe ser mayor que la hora inicial.";

        // HU4 - AB#64: todos los datos de la reserva son obligatorios.
        public static Dictionary<string, string> ValidarCamposObligatorios(
            int? laboratorioId,
            DateOnly? fecha,
            TimeSpan? horaInicio,
            TimeSpan? horaFin)
        {
            var errores =
                new Dictionary<string, string>();

            if (laboratorioId is null ||
                laboratorioId <= 0)
            {
                errores["laboratorioId"] =
                    "Selecciona un laboratorio.";
            }

            if (fecha is null)
            {
                errores["fecha"] =
                    "Ingresa la fecha de la reserva.";
            }

            if (horaInicio is null)
            {
                errores["horaInicio"] =
                    "Ingresa la hora inicial.";
            }

            if (horaFin is null)
            {
                errores["horaFin"] =
                    MensajeHoraFinRequerida;
            }

            return errores;
        }

        // HU5 - AB#65: valida la hora de finalización.
        public static string? ValidarHoraFinalizacion(
            TimeSpan? horaInicio,
            TimeSpan? horaFin)
        {
            if (horaFin is null)
            {
                return MensajeHoraFinRequerida;
            }

            if (horaInicio is not null &&
                !ReglasDisponibilidad.EsRangoHorasValido(
                    horaInicio.Value,
                    horaFin.Value))
            {
                return MensajeHoraFinInvalida;
            }

            return null;
        }
    }
}