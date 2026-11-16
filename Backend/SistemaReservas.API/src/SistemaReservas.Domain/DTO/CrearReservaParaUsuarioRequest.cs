namespace SistemaReservas.Domain.DTO
{
    // Gestión de Reservas: el Administrador registra una reserva a nombre de otro usuario.
    public record CrearReservaParaUsuarioRequest(
        string? Username,
        int? LaboratorioId,
        DateOnly? Fecha,
        TimeSpan? HoraInicio,
        TimeSpan? HoraFin
    );
}
