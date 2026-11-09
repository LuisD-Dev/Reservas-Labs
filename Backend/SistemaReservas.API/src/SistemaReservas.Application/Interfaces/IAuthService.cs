using SistemaReservas.Domain.DTO;

namespace SistemaReservas.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResult> LoginAsync(LoginRequest request);
    }
}
