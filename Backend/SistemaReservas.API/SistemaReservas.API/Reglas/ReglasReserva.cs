namespace SistemaReservas.API.Reglas
{
    // HU4 - Reglas de negocio para registrar reservas.
    public static class ReglasReserva
    {
        // Task #64 - Todos los datos de la reserva son obligatorios.
        public static Dictionary<string, string> ValidarCamposObligatorios(
            int? laboratorioId,
            DateOnly? fecha,
            TimeSpan? horaInicio,
            TimeSpan? horaFin)
        {
            var errores = new Dictionary<string, string>();

            if (laboratorioId is null || laboratorioId <= 0)
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
                    "Ingresa la hora final.";
            }

            return errores;
        }
    }
}
