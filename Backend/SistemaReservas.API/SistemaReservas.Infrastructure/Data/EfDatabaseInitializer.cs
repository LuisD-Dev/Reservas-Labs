using SistemaReservas.Application.Abstractions.Persistence;
using SistemaReservas.Application.Abstractions.Security;

namespace SistemaReservas.Infrastructure.Data
{
    public class EfDatabaseInitializer(
        SistemaReservasDbContext context,
        IPasswordHashService passwordHashService,
        TimeProvider timeProvider) : IDatabaseInitializer
    {
        public Task InitializeAsync()
        {
            return EfDatabaseSeeder.SeedAsync(context, passwordHashService, timeProvider);
        }
    }
}
