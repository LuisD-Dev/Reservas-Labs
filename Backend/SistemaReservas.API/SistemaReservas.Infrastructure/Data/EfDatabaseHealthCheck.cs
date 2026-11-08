using Microsoft.EntityFrameworkCore;
using SistemaReservas.Application.Abstractions.Persistence;

namespace SistemaReservas.Infrastructure.Data
{
    public class EfDatabaseHealthCheck(SistemaReservasDbContext context) : IDatabaseHealthCheck
    {
        public Task<bool> CanConnectAsync()
        {
            return context.Database.CanConnectAsync();
        }
    }
}
