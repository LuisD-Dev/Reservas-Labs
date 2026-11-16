namespace SistemaReservas.Domain.DTO
{
    // Gestión de Reservas: reserva vista por el Administrador, con el nombre del
    // laboratorio y quién la registró. No incluye datos privados del usuario.
    public record ReservaGestionResponse(
        int Id,
        int LaboratorioId,
        string NombreLaboratorio,
        string NombreUsuario,
        string Username,
        DateOnly Fecha,
        TimeSpan HoraInicio,
        TimeSpan HoraFin,
        string Estado);
}
