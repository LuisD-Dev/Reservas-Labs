namespace SistemaReservas.API.Models
{
    public class Reserva
    {
        public int Id { get; set; }

        public int LaboratorioId { get; set; }

        public int UsuarioId { get; set; }

        public DateTime Fecha { get; set; }

        public TimeSpan HoraInicio { get; set; }

        public TimeSpan HoraFin { get; set; }

        public string Estado { get; set; } = "Activa";

        public Laboratorio Laboratorio { get; set; } = null!;

        public Usuario Usuario { get; set; } = null!;
    }
}
