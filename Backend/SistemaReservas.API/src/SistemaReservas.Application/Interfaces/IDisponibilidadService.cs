using SistemaReservas.Domain.DTO;

namespace SistemaReservas.Application.Interfaces
{
    public interface IDisponibilidadService
    {
        Task<IReadOnlyList<DisponibilidadResponse>> ObtenerDisponibilidadAsync(int laboratorioId);
    }
}
