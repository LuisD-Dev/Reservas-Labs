using Microsoft.EntityFrameworkCore;
using SistemaReservas.API.Models;
using SistemaReservas.API.Services;

namespace SistemaReservas.API.Data
{
    public static class EfDatabaseSeeder
    {
        public static async Task SeedAsync(
            SistemaReservasDbContext context,
            AuthService authService,
            TimeProvider timeProvider)
        {
            // Solo agrega los datos de prueba que falten; no borra nada.
            // Aquí no se llama a context.Database.Migrate(): las migraciones se aplican
            // aparte con "dotnet ef database update".

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

            // Disponibilidades con fechas relativas: si no queda ningún horario de hoy
            // en adelante, se crean para mañana y pasado mañana en cada laboratorio
            // (mismo patrón de las pruebas de HU3). Así la consulta de disponibilidad
            // siempre tiene fechas válidas para probar.
            // "Hoy" es la fecha local, la misma que usa el frontend para validar.
            var hoy = timeProvider.GetLocalNow().Date;

            if (!await context.Disponibilidades.AnyAsync(d => d.Fecha >= hoy))
            {
                var laboratorios = await context.Laboratorios
                    .OrderBy(l => l.Id)
                    .ToListAsync();

                foreach (var laboratorio in laboratorios)
                {
                    foreach (var fecha in new[] { hoy.AddDays(1), hoy.AddDays(2) })
                    {
                        context.Disponibilidades.AddRange(
                            new Disponibilidad
                            {
                                LaboratorioId = laboratorio.Id,
                                Fecha = fecha,
                                HoraInicio = TimeSpan.Parse("08:00"),
                                HoraFin = TimeSpan.Parse("12:00"),
                                Estado = "Disponible"
                            },
                            new Disponibilidad
                            {
                                LaboratorioId = laboratorio.Id,
                                Fecha = fecha,
                                HoraInicio = TimeSpan.Parse("13:00"),
                                HoraFin = TimeSpan.Parse("15:00"),
                                Estado = "No disponible"
                            }
                        );
                    }
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
