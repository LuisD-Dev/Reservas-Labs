using MapsterMapper;
using NSubstitute;
using SistemaReservas.Application.Implementation;
using SistemaReservas.Application.Interfaces;
using SistemaReservas.Domain.DTO;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Domain.Rules;
using SistemaReservas.Infrastructure.Interfaces;
using SistemaReservas.Infrastructure.Data;
using SistemaReservas.Infrastructure.Implementation;

namespace SistemaReservas.Tests.Fakes
{
    public static class ReservaServiceFactory
    {
        // Horario habilitado por defecto para las pruebas previas de reservas.
        // La verificacion real conserva las comprobaciones de conflictos sobre su repositorio.
        public static ReservaService Crear(IReservaRepository repository, TimeProvider reloj, IMapper mapper,
            IUsuarioRepository? usuarios = null)
        {
            var verificacion = Substitute.For<IVerificacionDisponibilidadService>();
            verificacion.VerificarAsync(Arg.Any<int>(), Arg.Any<ConsultaDisponibilidadRequest>())
                .Returns(call =>
                {
                    var id = call.Arg<int>();
                    var request = call.Arg<ConsultaDisponibilidadRequest>();
                    var laboratorios = new FakeLaboratorioRepository([
                        new Laboratorio { Id = id, Estado = ReglasEstadoLaboratorio.Habilitado }
                    ]);
                    var horarios = new FakeDisponibilidadRepository([
                        new Disponibilidad
                        {
                            LaboratorioId = id, Fecha = request.Fecha!.Value.ToDateTime(TimeOnly.MinValue),
                            HoraInicio = TimeSpan.FromHours(7), HoraFin = TimeSpan.FromHours(21), Estado = "Disponible"
                        }
                    ]);
                    return new VerificacionDisponibilidadService(horarios, repository, laboratorios, reloj, mapper)
                        .VerificarAsync(id, request);
                });
            return Crear(repository, reloj, mapper, verificacion, usuarios);
        }

        // Los usuarios solo se consultan al registrar reservas a nombre de otro usuario (Gestión de Reservas).
        public static ReservaService Crear(IReservaRepository repository, TimeProvider reloj, IMapper mapper,
            IVerificacionDisponibilidadService verificacion, IUsuarioRepository? usuarios = null) =>
            new(repository, reloj, mapper, verificacion, usuarios ?? Substitute.For<IUsuarioRepository>());

        public static ReservaService Crear(AppDbContext context, TimeProvider reloj, IMapper mapper)
        {
            var reservas = new ReservaRepository(context);
            return Crear(reservas, reloj, mapper, new VerificacionDisponibilidadService(
                new DisponibilidadRepository(context), reservas, new LaboratorioRepository(context), reloj, mapper),
                new UsuarioRepository(context));
        }
    }
}
