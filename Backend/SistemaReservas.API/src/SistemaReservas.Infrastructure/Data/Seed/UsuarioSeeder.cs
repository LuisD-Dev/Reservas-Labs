using Microsoft.EntityFrameworkCore;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Infrastructure.Interfaces;

namespace SistemaReservas.Infrastructure.Data.Seed
{
    public static class UsuarioSeeder
    {
        public static async Task SeedAsync(AppDbContext context, IPasswordHashService passwordHashService)
        {
            if (!await context.Usuarios.AnyAsync(u => u.Username == "admin"))
            {
                var admin = new Usuario
                {
                    Nombre = "Administrador",
                    Username = "admin",
                    PasswordHash = passwordHashService.HashPassword("1234"),
                    Rol = "Administrador",
                    IntentosFallidos = 0
                };

                context.Usuarios.Add(admin);
            }

            if (!await context.Usuarios.AnyAsync(u => u.Username == "usuario"))
            {
                var usuario = new Usuario
                {
                    Nombre = "Usuario de prueba",
                    Username = "usuario",
                    PasswordHash = passwordHashService.HashPassword("1234"),
                    Rol = "Usuario",
                    IntentosFallidos = 0
                };

                context.Usuarios.Add(usuario);
            }

        }
    }
}
