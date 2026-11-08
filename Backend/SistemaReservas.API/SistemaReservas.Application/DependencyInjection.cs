using Microsoft.Extensions.DependencyInjection;
using SistemaReservas.Application.Abstractions.Services;
using SistemaReservas.Application.Services;

namespace SistemaReservas.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ILaboratorioService, LaboratorioService>();
            services.AddScoped<IDisponibilidadService, DisponibilidadService>();
            services.AddScoped<IVerificacionDisponibilidadService, VerificacionDisponibilidadService>();
            services.AddScoped<IReservaService, ReservaService>();

            return services;
        }
    }
}
