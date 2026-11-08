using SistemaReservas.Application.DTOs;

namespace SistemaReservas.Application.Abstractions.Services
{
    public interface IAuthService
    {
        Task<LoginResult> LoginAsync(LoginRequest request);

        string HashPassword(string password);
    }
}
