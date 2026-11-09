using SistemaReservas.Domain.DTO;

namespace SistemaReservas.Application.Interfaces
{
    public interface IVerificacionDisponibilidadService
    {
        Task<VerificacionDisponibilidadResponse> VerificarAsync(
            int laboratorioId, ConsultaDisponibilidadRequest request);
    }
}
