using Microsoft.Extensions.DependencyInjection;
using SistemaReservas.Application;
using SistemaReservas.Application.Interfaces;
using SistemaReservas.Application.Implementation;

namespace SistemaReservas.Tests.Application
{
    public class DependencyInjectionTests
    {
        [Fact]
        public void AddApplication_RegistraCincoCasosDeUsoScopedYMapper()
        {
            var services = new ServiceCollection();

            var resultado = services.AddApplication();

            Assert.Same(services, resultado);
            var esperados = new Dictionary<Type, Type>
            {
                [typeof(IAuthService)] = typeof(AuthService),
                [typeof(ILaboratorioService)] = typeof(LaboratorioService),
                [typeof(IDisponibilidadService)] = typeof(DisponibilidadService),
                [typeof(IVerificacionDisponibilidadService)] = typeof(VerificacionDisponibilidadService),
                [typeof(IReservaService)] = typeof(ReservaService)
            };
            // Lista cerrada: no admite registros de persistencia, hashing, JWT ni reloj.
            Assert.Equal(esperados.Count + 2, services.Count);
            Assert.Equal(ServiceLifetime.Singleton, Assert.Single(services, s => s.ServiceType == typeof(Mapster.TypeAdapterConfig)).Lifetime);
            Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, s => s.ServiceType == typeof(MapsterMapper.IMapper)).Lifetime);
            foreach (var (contrato, implementacion) in esperados)
            {
                var registro = Assert.Single(services, descriptor => descriptor.ServiceType == contrato);
                Assert.Equal(implementacion, registro.ImplementationType);
                Assert.Equal(ServiceLifetime.Scoped, registro.Lifetime);
            }
        }
    }
}
