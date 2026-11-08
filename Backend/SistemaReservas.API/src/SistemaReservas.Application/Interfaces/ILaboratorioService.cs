using SistemaReservas.Domain.DTO;

namespace SistemaReservas.Application.Interfaces
{
    public interface ILaboratorioService
    {
        Task<IReadOnlyList<LaboratorioResponse>> ObtenerLaboratoriosAsync();

        Task<ResultadoActualizarEstadoLaboratorio> ActualizarEstadoAsync(int id, string? estado);
    }
}
