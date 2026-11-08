namespace SistemaReservas.Domain.DTO
{
    public record ResultadoCrearReserva(
        ReservaCreadaResponse? Reserva,
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
            ReservaCreadaResponse reserva)
        {
            return new ResultadoCrearReserva(
                reserva,
                new Dictionary<string, string>());
        }
    }
}
