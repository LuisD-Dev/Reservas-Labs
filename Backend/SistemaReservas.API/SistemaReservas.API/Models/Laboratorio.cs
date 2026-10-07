namespace SistemaReservas.API.Models
{
    public class Laboratorio
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Ubicacion { get; set; } = string.Empty;

        public int Capacidad { get; set; }

        public string Estado { get; set; } = string.Empty;

        // Navigation property: one laboratorio has many disponibilidades
        public List<Disponibilidad> Disponibilidades { get; set; } = new();
    }
}