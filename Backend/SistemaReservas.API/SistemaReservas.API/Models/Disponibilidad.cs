namespace SistemaReservas.API.Models
{
    public class Disponibilidad
    {
        public int Id { get; set; }

        public int LaboratorioId { get; set; }

        public DateTime Fecha { get; set; }

        public TimeSpan HoraInicio { get; set; }

        public TimeSpan HoraFin { get; set; }

        public string Estado { get; set; } = string.Empty;

        public Laboratorio Laboratorio { get; set; } = null!;
    }
}