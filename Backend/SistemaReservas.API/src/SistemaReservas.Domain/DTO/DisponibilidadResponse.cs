namespace SistemaReservas.Domain.DTO
{
    public record DisponibilidadResponse(int Id, int LaboratorioId, DateTime Fecha, TimeSpan HoraInicio, TimeSpan HoraFin, string Estado);
}
