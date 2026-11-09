namespace SistemaReservas.Domain.DTO
{
    public record LaboratorioResponse(int Id, string Nombre, string Ubicacion, int Capacidad, string Estado);
}
