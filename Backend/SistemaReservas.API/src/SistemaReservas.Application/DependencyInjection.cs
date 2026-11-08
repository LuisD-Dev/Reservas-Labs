using MapsterMapper;
using SistemaReservas.Application.Mapper;
using Microsoft.Extensions.DependencyInjection;
using SistemaReservas.Application.Interfaces;
using SistemaReservas.Application.Implementation;

namespace SistemaReservas.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton(MapsterConfig.RegisterMaps());
            services.AddScoped<IMapper, ServiceMapper>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ILaboratorioService, LaboratorioService>();
            services.AddScoped<IDisponibilidadService, DisponibilidadService>();
            services.AddScoped<IVerificacionDisponibilidadService, VerificacionDisponibilidadService>();
            services.AddScoped<IReservaService, ReservaService>();

            return services;
        }
    }
}
