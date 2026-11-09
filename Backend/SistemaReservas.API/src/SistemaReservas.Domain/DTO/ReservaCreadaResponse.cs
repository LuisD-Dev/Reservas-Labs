namespace SistemaReservas.Domain.DTO
{
    public record ReservaCreadaResponse(int Id, int LaboratorioId, DateOnly Fecha, TimeSpan HoraInicio, TimeSpan HoraFin, string Estado);
}
