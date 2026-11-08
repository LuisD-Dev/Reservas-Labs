namespace SistemaReservas.Infrastructure.Interfaces
{
    public interface ITokenService
    {
        (string Token, DateTime ExpiraUtc) GenerarToken(
            int usuarioId,
            string nombre,
            string rol);
    }
}
