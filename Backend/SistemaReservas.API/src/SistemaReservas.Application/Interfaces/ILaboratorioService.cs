using SistemaReservas.Domain.DTO;

namespace SistemaReservas.Application.Interfaces
{
    public interface ILaboratorioService
    {
        Task<IReadOnlyList<LaboratorioResponse>> ObtenerLaboratoriosAsync(bool soloHabilitados = false);

        Task<ResultadoActualizarEstadoLaboratorio> ActualizarEstadoAsync(int id, string? estado);
    }
}
