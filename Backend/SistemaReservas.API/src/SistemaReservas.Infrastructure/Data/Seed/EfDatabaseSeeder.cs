using SistemaReservas.Infrastructure.Interfaces;

namespace SistemaReservas.Infrastructure.Data.Seed
{
    public static class EfDatabaseSeeder
    {
        public static async Task SeedAsync(AppDbContext context, IPasswordHashService passwordHashService, TimeProvider timeProvider)
        {
            await UsuarioSeeder.SeedAsync(context, passwordHashService);
            await LaboratorioSeeder.SeedAsync(context);
            await context.SaveChangesAsync();
            await DisponibilidadSeeder.SeedAsync(context, timeProvider);
        }
    }
}
