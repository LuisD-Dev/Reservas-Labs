using SistemaReservas.Application.DTOs;

namespace SistemaReservas.Application.Abstractions.Services
{
    public interface IVerificacionDisponibilidadService
    {
        Task<VerificacionDisponibilidadResponse> VerificarAsync(
            int laboratorioId, ConsultaDisponibilidadRequest request);
    }
}
