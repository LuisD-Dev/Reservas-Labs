namespace SistemaReservas.API.Reglas
{
    // Reglas de negocio compartidas por HU4 y HU5.
    public static class ReglasReserva
    {
        public const string MensajeFechaPasada =
            "La fecha no puede ser anterior a hoy.";

        // AB#68: el reloj se resuelve fuera de la regla.
        public static string? ValidarFechaReserva(DateOnly fecha, DateOnly hoy)
        {
            return ReglasDisponibilidad.EsFechaPasada(fecha, hoy)
                ? MensajeFechaPasada
                : null;
        }

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

        // HU5 - AB#69: horario de funcionamiento de los laboratorios (07:00 a 21:00).
        public static readonly TimeSpan HoraApertura = new TimeSpan(7, 0, 0);
        public static readonly TimeSpan HoraCierre = new TimeSpan(21, 0, 0);

        public static readonly string MensajeFueraDeHorario =
            $"La reserva debe estar dentro del horario de funcionamiento ({HoraApertura:hh\\:mm} a {HoraCierre:hh\\:mm}).";

        // Devuelve el mensaje de error, o null si el rango cae dentro del horario.
        // Si falta alguna hora no opina: eso lo validan las reglas de campos obligatorios.
        public static string? ValidarHorarioFuncionamiento(
            TimeSpan? horaInicio,
            TimeSpan? horaFin)
        {
            if (horaInicio is null || horaFin is null)
            {
                return null;
            }

            if (horaInicio < HoraApertura || horaFin > HoraCierre)
            {
                return MensajeFueraDeHorario;
            }

            return null;
        }

        // HU5 - AB#70: duración máxima de una reserva (4 horas).
        public static readonly TimeSpan DuracionMaxima = TimeSpan.FromHours(4);

        public static readonly string MensajeDuracionExcedida =
            $"La reserva no puede durar más de {DuracionMaxima.TotalHours:0.##} horas.";

        // Devuelve el mensaje de error, o null si la duración es válida.
        // Si falta una hora o el rango es inválido no opina: eso lo validan
        // las reglas de campos obligatorios y de hora de finalización.
        public static string? ValidarDuracionMaxima(
            TimeSpan? horaInicio,
            TimeSpan? horaFin)
        {
            if (horaInicio is null || horaFin is null)
            {
                return null;
            }

            if (horaFin <= horaInicio)
            {
                return null;
            }

            return horaFin - horaInicio > DuracionMaxima
                ? MensajeDuracionExcedida
                : null;
        }
    }
}