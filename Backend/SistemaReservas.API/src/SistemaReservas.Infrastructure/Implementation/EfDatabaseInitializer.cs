using SistemaReservas.Infrastructure.Data;
using SistemaReservas.Infrastructure.Data.Seed;
using SistemaReservas.Infrastructure.Interfaces;

namespace SistemaReservas.Infrastructure.Implementation
{
    public class EfDatabaseInitializer(
        AppDbContext context,
        IPasswordHashService passwordHashService,
        TimeProvider timeProvider) : IDatabaseInitializer
    {
        public Task InitializeAsync()
        {
            return EfDatabaseSeeder.SeedAsync(context, passwordHashService, timeProvider);
        }
    }
}
