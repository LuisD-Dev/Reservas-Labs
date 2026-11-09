namespace SistemaReservas.Domain.DTO
{
    public enum EstadoResultadoCancelacion
    {
        Exitosa,
        UsuarioInvalido,
        NoEncontrada,
        NoAutorizada,
        YaCancelada,
        EstadoInvalido
    }

    public record ResultadoCancelarReserva(
        EstadoResultadoCancelacion Estado,
        string Mensaje)
    {
        public bool Exitoso =>
            Estado == EstadoResultadoCancelacion.Exitosa;
    }
}