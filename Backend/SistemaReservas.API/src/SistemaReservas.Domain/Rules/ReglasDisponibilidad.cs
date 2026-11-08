using SistemaReservas.Domain.Entities;

namespace SistemaReservas.Domain.Rules
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
        public const string EstadoLaboratorioHabilitado = ReglasEstadoLaboratorio.Habilitado;
        public const string MensajeFueraDeServicio = "El laboratorio está fuera de servicio.";
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
            return ReglasReserva.ValidarSolicitud(
                laboratorioId, fecha, horaInicio, horaFin, hoy, consulta: true);
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
                    MensajeFueraDeServicio,
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

        // Divide horarios nominales para mostrar los tramos libres y ocupados reales.
        public static IEnumerable<Disponibilidad> ObtenerTramosEfectivos(
            IEnumerable<Disponibilidad> horarios, IReadOnlyList<Reserva> reservas,
            int laboratorioId, DateOnly fecha, bool habilitado)
        {
            foreach (var horario in horarios)
            {
                var limites = new SortedSet<TimeSpan> { horario.HoraInicio, horario.HoraFin };
                foreach (var reserva in reservas.Where(r => r.Estado == "Activa"
                    && r.LaboratorioId == laboratorioId && DateOnly.FromDateTime(r.Fecha) == fecha
                    && ReglasTraslape.HayTraslape(horario.HoraInicio, horario.HoraFin, r.HoraInicio, r.HoraFin)))
                {
                    limites.Add(reserva.HoraInicio > horario.HoraInicio ? reserva.HoraInicio : horario.HoraInicio);
                    limites.Add(reserva.HoraFin < horario.HoraFin ? reserva.HoraFin : horario.HoraFin);
                }
                var puntos = limites.ToArray();
                for (var i = 0; i < puntos.Length - 1; i++)
                {
                    yield return new Disponibilidad
                    {
                        Id = horario.Id, LaboratorioId = laboratorioId, Fecha = horario.Fecha,
                        HoraInicio = puntos[i], HoraFin = puntos[i + 1],
                        Estado = !habilitado || TieneConflicto(reservas, laboratorioId, fecha, puntos[i], puntos[i + 1])
                            ? "No disponible" : horario.Estado
                    };
                }
            }
        }

        public static bool TieneConflicto(
            IEnumerable<Reserva> reservas, int laboratorioId, DateOnly fecha,
            TimeSpan horaInicio, TimeSpan horaFin)
        {
            return reservas.Any(r => r.LaboratorioId == laboratorioId
                && DateOnly.FromDateTime(r.Fecha) == fecha && r.Estado == "Activa"
                && ReglasTraslape.HayTraslape(horaInicio, horaFin, r.HoraInicio, r.HoraFin));
        }

        // Muestra la hora como "08:00", igual que el frontend.
        private static string FormatearHora(TimeSpan hora)
        {
            return hora.ToString(@"hh\:mm");
        }
    }
}
