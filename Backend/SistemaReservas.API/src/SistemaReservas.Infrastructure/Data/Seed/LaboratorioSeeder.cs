using Microsoft.EntityFrameworkCore;
using SistemaReservas.Domain.Entities;

namespace SistemaReservas.Infrastructure.Data.Seed
{
    public static class LaboratorioSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
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

        }
    }
}
