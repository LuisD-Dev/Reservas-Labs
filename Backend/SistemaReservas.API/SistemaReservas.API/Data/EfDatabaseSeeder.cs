using Microsoft.EntityFrameworkCore;
using SistemaReservas.API.Models;
using SistemaReservas.API.Services;

namespace SistemaReservas.API.Data
{
    public static class EfDatabaseSeeder
    {
        public static async Task SeedAsync(SistemaReservasDbContext context, AuthService authService)
        {
            // Ensure database is created (no destructive operations)
            // Note: we do not call context.Database.Migrate() here; migrations will be handled separately.

            // Usuarios: admin y usuario
            if (!await context.Usuarios.AnyAsync(u => u.Username == "admin"))
            {
                var admin = new Usuario
                {
                    Nombre = "Administrador",
                    Username = "admin",
                    PasswordHash = authService.HashPassword("1234"),
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
                    PasswordHash = authService.HashPassword("1234"),
                    Rol = "Usuario",
                    IntentosFallidos = 0
                };

                context.Usuarios.Add(usuario);
            }

            // Laboratorios
            if (!await context.Laboratorios.AnyAsync())
            {
                context.Laboratorios.AddRange(
                    new Laboratorio
                    {
                        Nombre = "Laboratorio de Software",
                        Ubicacion = "Edificio A",
                        Capacidad = 30,
                        Estado = "Habilitado"
                    },
                    new Laboratorio
                    {
                        Nombre = "Laboratorio de Redes",
                        Ubicacion = "Edificio B",
                        Capacidad = 25,
                        Estado = "Habilitado"
                    }
                );
            }

            await context.SaveChangesAsync();

            // Disponibilidades
            if (!await context.Disponibilidades.AnyAsync())
            {
                var lab1 = await context.Laboratorios.FirstAsync(l => l.Nombre == "Laboratorio de Software");
                var lab2 = await context.Laboratorios.FirstAsync(l => l.Nombre == "Laboratorio de Redes");

                context.Disponibilidades.AddRange(
                    new Disponibilidad
                    {
                        LaboratorioId = lab1.Id,
                        Fecha = new DateTime(2026, 10, 1),
                        HoraInicio = TimeSpan.Parse("08:00"),
                        HoraFin = TimeSpan.Parse("10:00"),
                        Estado = "Disponible"
                    },
                    new Disponibilidad
                    {
                        LaboratorioId = lab1.Id,
                        Fecha = new DateTime(2026, 10, 1),
                        HoraInicio = TimeSpan.Parse("10:00"),
                        HoraFin = TimeSpan.Parse("12:00"),
                        Estado = "No disponible"
                    },
                    new Disponibilidad
                    {
                        LaboratorioId = lab2.Id,
                        Fecha = new DateTime(2026, 10, 1),
                        HoraInicio = TimeSpan.Parse("13:00"),
                        HoraFin = TimeSpan.Parse("15:00"),
                        Estado = "Disponible"
                    }
                );

                await context.SaveChangesAsync();
            }
        }
    }
}
