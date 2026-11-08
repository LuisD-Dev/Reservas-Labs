namespace SistemaReservas.Application.Abstractions.Security
{
    public interface ITokenService
    {
        (string Token, DateTime ExpiraUtc) GenerarToken(
            int usuarioId,
            string nombre,
            string rol);
    }
}
