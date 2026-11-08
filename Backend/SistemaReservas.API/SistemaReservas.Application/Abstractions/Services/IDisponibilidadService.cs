using SistemaReservas.Domain.Entities;

namespace SistemaReservas.Application.Abstractions.Services
{
    public interface IDisponibilidadService
    {
        Task<List<Disponibilidad>> ObtenerDisponibilidadAsync(int laboratorioId);
    }
}
