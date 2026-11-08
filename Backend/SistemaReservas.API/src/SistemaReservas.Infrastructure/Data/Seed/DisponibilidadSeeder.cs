using Microsoft.EntityFrameworkCore;
using SistemaReservas.Domain.Entities;

namespace SistemaReservas.Infrastructure.Data.Seed
{
    public static class DisponibilidadSeeder
    {
        public static async Task SeedAsync(AppDbContext context, TimeProvider timeProvider)
        {
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
