namespace SistemaReservas.API.DTOs
{
    public enum LoginEstado
    {
        Exitoso,
        CredencialesInvalidas,
        Bloqueado
    }

    public record LoginResult(
        LoginEstado Estado,
        int? UsuarioId = null,
        string? Nombre = null,
        string? Rol = null,
        DateTime? BloqueadoHasta = null
    );
}
