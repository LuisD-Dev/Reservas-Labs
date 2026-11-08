using SistemaReservas.API.Models;

namespace SistemaReservas.Application.Abstractions.Security
{
    public interface IPasswordHashService
    {
        string HashPassword(string password);

        bool VerifyPassword(
            Usuario usuario,
            string password);
    }
}
