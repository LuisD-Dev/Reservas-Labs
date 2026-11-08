using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaReservas.Application.Abstractions.Persistence;
using SistemaReservas.Application.Abstractions.Security;
using SistemaReservas.Infrastructure.Data;
using SistemaReservas.Infrastructure.Repositories;
using SistemaReservas.Infrastructure.Security;

namespace SistemaReservas.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            string? connectionString)
        {
            services.AddDbContext<SistemaReservasDbContext>(
                options =>
                    options.UseSqlServer(
                        connectionString,
                        sqlServerOptions =>
                            sqlServerOptions.MigrationsAssembly(
                                typeof(SistemaReservasDbContext)
                                    .Assembly
                                    .GetName()
                                    .Name!)));

            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<ILaboratorioRepository, LaboratorioRepository>();
            services.AddScoped<IDisponibilidadRepository, DisponibilidadRepository>();
            services.AddScoped<IReservaRepository, ReservaRepository>();
            services.AddScoped<IPasswordHashService, PasswordHashService>();

            services.AddSingleton<ITokenService, JwtTokenService>();
            services.AddScoped<IDatabaseInitializer, EfDatabaseInitializer>();
            services.AddScoped<IDatabaseHealthCheck, EfDatabaseHealthCheck>();

            return services;
        }
    }
}
