using Microsoft.AspNetCore.Identity;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Infrastructure.Interfaces;

namespace SistemaReservas.Infrastructure.Implementation
{
    public sealed class PasswordHashService
        : IPasswordHashService
    {
        private readonly PasswordHasher<Usuario> _passwordHasher =
            new();

        public string HashPassword(string password)
        {
            return _passwordHasher.HashPassword(
                new Usuario(),
                password);
        }

        public bool VerifyPassword(
            Usuario usuario,
            string password)
        {
            var resultado =
                _passwordHasher.VerifyHashedPassword(
                    usuario,
                    usuario.PasswordHash,
                    password);

            return resultado !=
                PasswordVerificationResult.Failed;
        }
    }
}
