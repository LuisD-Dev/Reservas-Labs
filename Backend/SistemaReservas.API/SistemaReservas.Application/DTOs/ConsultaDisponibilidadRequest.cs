namespace SistemaReservas.Application.DTOs
{
    public record ConsultaDisponibilidadRequest(
        DateOnly? Fecha, TimeSpan? HoraInicio, TimeSpan? HoraFin);

    public record HorarioDisponibilidadResponse(
        int Id, DateOnly Fecha, TimeSpan HoraInicio, TimeSpan HoraFin, string Estado);

    public record VerificacionDisponibilidadResponse(
        bool Disponible, string Motivo,
        List<HorarioDisponibilidadResponse> HorariosDelDia,
        Dictionary<string, string> Errores);
}
