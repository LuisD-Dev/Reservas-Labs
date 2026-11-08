namespace SistemaReservas.Domain.DTO
{
    public record MiReservaResponse(
        int Id,
        int LaboratorioId,
        string NombreLaboratorio,
        DateOnly Fecha,
        TimeSpan HoraInicio,
        TimeSpan HoraFin,
        string Estado);
}