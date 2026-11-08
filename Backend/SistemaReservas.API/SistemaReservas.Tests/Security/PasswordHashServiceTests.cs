using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Infrastructure.Security;

namespace SistemaReservas.Tests.Security
{
    public class PasswordHashServiceTests
    {
        [Fact]
        public void HashPassword_GeneraHashSeguroYUsaSalDistinta()
        {
            var servicio = new PasswordHashService();
            const string password = "Password-de-prueba-1234";

            var hash = servicio.HashPassword(password);
            var otroHash = servicio.HashPassword(password);

            Assert.False(string.IsNullOrWhiteSpace(hash));
            Assert.DoesNotContain(password, hash);
            Assert.NotEqual(hash, otroHash);
        }

        [Fact]
        public void VerifyPassword_HashGeneradoYPasswordCorrecta_DevuelveTrue()
        {
            var servicio = new PasswordHashService();
            const string password = "Password-de-prueba-1234";
            var usuario = new Usuario
            {
                PasswordHash = servicio.HashPassword(password)
            };

            var resultado = servicio.VerifyPassword(usuario, password);

            Assert.True(resultado);
        }

        [Fact]
        public void VerifyPassword_HashGeneradoYPasswordIncorrecta_DevuelveFalse()
        {
            var servicio = new PasswordHashService();
            var usuario = new Usuario
            {
                PasswordHash = servicio.HashPassword("Password-de-prueba-1234")
            };

            var resultado = servicio.VerifyPassword(usuario, "incorrecta");

            Assert.False(resultado);
        }

        [Fact]
        public void VerifyPassword_HashCreadoDirectamentePorIdentity_DevuelveTrue()
        {
            var servicio = new PasswordHashService();
            var identity = new PasswordHasher<Usuario>();
            var usuario = new Usuario();
            const string password = "Password-de-prueba-1234";
            usuario.PasswordHash = identity.HashPassword(usuario, password);

            var resultado = servicio.VerifyPassword(usuario, password);

            Assert.True(resultado);
        }

        [Fact]
        public void VerifyPassword_HashIdentityV2_DevuelveTrueSinModificarHash()
        {
            var servicio = new PasswordHashService();
            var identityV2 = new PasswordHasher<Usuario>(
                Options.Create(new PasswordHasherOptions
                {
                    CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2
                }));
            var usuario = new Usuario();
            const string password = "Password-de-prueba-1234";
            var hashOriginal = identityV2.HashPassword(usuario, password);
            usuario.PasswordHash = hashOriginal;
            var verificadorActual = new PasswordHasher<Usuario>();

            var resultadoIdentity = verificadorActual.VerifyHashedPassword(
                usuario, usuario.PasswordHash, password);
            var resultado = servicio.VerifyPassword(usuario, password);

            Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, resultadoIdentity);
            Assert.True(resultado);
            Assert.Equal(hashOriginal, usuario.PasswordHash);
        }
    }
}
