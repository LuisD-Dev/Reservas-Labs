using SistemaReservas.Domain.Entities;

namespace SistemaReservas.Application.Abstractions.Services
{
    public interface ILaboratorioService
    {
        Task<List<Laboratorio>> ObtenerLaboratoriosAsync();
    }
}
