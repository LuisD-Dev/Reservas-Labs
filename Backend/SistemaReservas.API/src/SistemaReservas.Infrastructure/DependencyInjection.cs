using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaReservas.Infrastructure.Interfaces;
using SistemaReservas.Infrastructure.Data;
using SistemaReservas.Infrastructure.Implementation;

namespace SistemaReservas.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            string? connectionString)
        {
            services.AddDbContext<AppDbContext>(
                options =>
                    options.UseSqlServer(
                        connectionString,
                        sqlServerOptions =>
                            sqlServerOptions.MigrationsAssembly(
                                typeof(AppDbContext)
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
