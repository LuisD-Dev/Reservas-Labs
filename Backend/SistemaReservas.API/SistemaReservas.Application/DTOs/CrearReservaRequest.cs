namespace SistemaReservas.Application.DTOs
{
    public record CrearReservaRequest(
        int? LaboratorioId,
        DateOnly? Fecha,
        TimeSpan? HoraInicio,
        TimeSpan? HoraFin
    );
}
