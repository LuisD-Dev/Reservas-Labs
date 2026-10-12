using SistemaReservas.API.Models;

namespace SistemaReservas.API.Reglas
{
    // HU3 - Resultado de evaluar si un laboratorio está disponible para una consulta.
    public record ResultadoDisponibilidad(
        bool Disponible,
        string Motivo,
        List<Disponibilidad> HorariosDelDia
    );

    // NFR2 - Reglas de negocio aisladas: consulta de disponibilidad (HU3).
    // Equivalen a frontend/src/utils/disponibilidad.js (mismos resultados y mensajes)
    // para poder validarlas también en el servidor. No usan base de datos, HTTP ni reloj:
    // la fecha de hoy llega como parámetro.
    public static class ReglasDisponibilidad
    {
        public const string EstadoLaboratorioHabilitado = "Habilitado";
        public const string EstadoHorarioDisponible = "Disponible";

        // Devuelve los errores por campo, con las mismas claves que el frontend.
        // Si la consulta es válida, el diccionario queda vacío.
        public static Dictionary<string, string> ValidarConsulta(
            int? laboratorioId,
            DateOnly? fecha,
            TimeSpan? horaInicio,
            TimeSpan? horaFin,
            DateOnly hoy)
        {
            var errores = new Dictionary<string, string>();

            if (laboratorioId is null || laboratorioId <= 0)
            {
                errores["laboratorioId"] = "Selecciona un laboratorio.";
            }
            if (fecha is null)
            {
                errores["fecha"] = "Ingresa la fecha a consultar.";
            }
            else if (EsFechaPasada(fecha.Value, hoy))
            {
                errores["fecha"] = "La fecha no puede ser anterior a hoy.";
            }
            if (horaInicio is null)
            {
                errores["horaInicio"] = "Ingresa la hora inicial.";
            }
            if (horaFin is null)
            {
                errores["horaFin"] = "Ingresa la hora final.";
            }
            if (horaInicio is not null && horaFin is not null &&
                !EsRangoHorasValido(horaInicio.Value, horaFin.Value))
            {
                errores["horaFin"] = "La hora final debe ser mayor que la hora inicial.";
            }

            return errores;
        }

        public static bool EsFechaPasada(DateOnly fecha, DateOnly hoy)
        {
            return fecha < hoy;
        }

        // La hora final debe ser mayor que la inicial.
        public static bool EsRangoHorasValido(TimeSpan horaInicio, TimeSpan horaFin)
        {
            return horaFin > horaInicio;
        }

        // Un horario cubre la consulta si está "Disponible" y abarca todo el rango pedido.
        public static bool CubreRango(Disponibilidad horario, TimeSpan horaInicio, TimeSpan horaFin)
        {
            return horario.Estado == EstadoHorarioDisponible &&
                   horario.HoraInicio <= horaInicio &&
                   horario.HoraFin >= horaFin;
        }

        // Cualquier estado distinto de "Habilitado" deja al laboratorio fuera de servicio.
        public static bool EstaFueraDeServicio(Laboratorio laboratorio)
        {
            return laboratorio.Estado != EstadoLaboratorioHabilitado;
        }

        // HU3 - #39 Decide si el laboratorio está disponible para la consulta.
        // Está disponible si ese día existe un horario "Disponible" que cubre todo
        // el rango pedido. Un laboratorio "Fuera de servicio" nunca está disponible.
        public static ResultadoDisponibilidad EvaluarDisponibilidad(
            IEnumerable<Disponibilidad> horarios,
            DateOnly fecha,
            TimeSpan horaInicio,
            TimeSpan horaFin,
            Laboratorio? laboratorio)
        {
            var horariosDelDia = horarios
                .Where(h => DateOnly.FromDateTime(h.Fecha) == fecha)
                .OrderBy(h => h.HoraInicio)
                .ToList();

            if (laboratorio is not null && EstaFueraDeServicio(laboratorio))
            {
                return new ResultadoDisponibilidad(
                    false,
                    "El laboratorio está fuera de servicio.",
                    horariosDelDia);
            }

            var cubre = horariosDelDia.FirstOrDefault(h => CubreRango(h, horaInicio, horaFin));

            if (cubre is not null)
            {
                return new ResultadoDisponibilidad(
                    true,
                    $"El horario de {FormatearHora(cubre.HoraInicio)} a {FormatearHora(cubre.HoraFin)} está libre.",
                    horariosDelDia);
            }

            return new ResultadoDisponibilidad(
                false,
                horariosDelDia.Count == 0
                    ? "No hay horarios registrados para esa fecha."
                    : "Ningún horario disponible cubre todo el rango solicitado.",
                horariosDelDia);
        }

        // Muestra la hora como "08:00", igual que el frontend.
        private static string FormatearHora(TimeSpan hora)
        {
            return hora.ToString(@"hh\:mm");
        }
    }
}
