namespace SistemaReservas.API.DTOs
{
    public record CrearReservaRequest(
        int? LaboratorioId,
        DateOnly? Fecha,
        TimeSpan? HoraInicio,
        TimeSpan? HoraFin
    );
}
