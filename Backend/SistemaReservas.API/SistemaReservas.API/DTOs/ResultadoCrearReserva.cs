using SistemaReservas.API.Models;

namespace SistemaReservas.API.DTOs
{
    public record ResultadoCrearReserva(
        Reserva? Reserva,
        Dictionary<string, string> Errores)
    {
        public bool Exitoso =>
            Reserva is not null &&
            Errores.Count == 0;

        public static ResultadoCrearReserva Fallido(
            Dictionary<string, string> errores)
        {
            return new ResultadoCrearReserva(
                null,
                errores);
        }

        public static ResultadoCrearReserva Correcto(
            Reserva reserva)
        {
            return new ResultadoCrearReserva(
                reserva,
                new Dictionary<string, string>());
        }
    }
}
