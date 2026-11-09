namespace SistemaReservas.Domain.DTO
{
    public record CrearReservaRequest(
        int? LaboratorioId,
        DateOnly? Fecha,
        TimeSpan? HoraInicio,
        TimeSpan? HoraFin
    );
}
