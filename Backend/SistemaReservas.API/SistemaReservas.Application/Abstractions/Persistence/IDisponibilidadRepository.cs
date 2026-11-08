using SistemaReservas.Domain.Entities;

namespace SistemaReservas.Application.Abstractions.Persistence
{
    // NFR2 - Separar acceso a datos: consultas sobre la tabla Disponibilidades (HU3).
    public interface IDisponibilidadRepository
    {
        // Devuelve los horarios del laboratorio ordenados por fecha y hora de inicio.
        Task<List<Disponibilidad>> ObtenerPorLaboratorioAsync(int laboratorioId);
    }
}
