using SistemaReservas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SistemaReservas.Infrastructure.Interfaces;

namespace SistemaReservas.Infrastructure.Implementation
{
    public class EfDatabaseHealthCheck(AppDbContext context) : IDatabaseHealthCheck
    {
        public Task<bool> CanConnectAsync()
        {
            return context.Database.CanConnectAsync();
        }
    }
}
