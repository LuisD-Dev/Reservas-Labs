namespace SistemaReservas.API.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Rol { get; set; } = string.Empty;

        public int IntentosFallidos { get; set; }

        public DateTime? BloqueadoHasta { get; set; }
    }
}