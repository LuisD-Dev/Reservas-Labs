using SistemaReservas.Domain.Entities;

namespace SistemaReservas.Application.Abstractions.Persistence
{
    // NFR2 - Separar acceso a datos: consultas sobre la tabla Laboratorios (HU2).
    public interface ILaboratorioRepository
    {
        // Devuelve todos los laboratorios ordenados por nombre.
        Task<List<Laboratorio>> ObtenerTodosAsync();
    }
}
