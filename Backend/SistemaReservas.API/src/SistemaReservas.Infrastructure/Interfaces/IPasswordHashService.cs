using SistemaReservas.Domain.Entities;

namespace SistemaReservas.Infrastructure.Interfaces
{
    public interface IPasswordHashService
    {
        string HashPassword(string password);

        bool VerifyPassword(
            Usuario usuario,
            string password);
    }
}
