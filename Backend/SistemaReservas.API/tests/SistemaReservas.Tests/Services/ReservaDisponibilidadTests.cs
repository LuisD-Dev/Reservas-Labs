using MapsterMapper;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using SistemaReservas.Application.Implementation;
using SistemaReservas.Application.Interfaces;
using SistemaReservas.Application.Mapper;
using SistemaReservas.Domain.DTO;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Domain.Rules;
using SistemaReservas.Infrastructure.Interfaces;
using SistemaReservas.Tests.Fakes;

namespace SistemaReservas.Tests.Services
{
    public class ReservaDisponibilidadTests
    {
        private readonly ILaboratorioRepository _laboratorios = Substitute.For<ILaboratorioRepository>();
        private readonly IDisponibilidadRepository _horarios = Substitute.For<IDisponibilidadRepository>();
        private readonly IReservaRepository _reservas = Substitute.For<IReservaRepository>();
        private readonly IVerificacionDisponibilidadService _verificacion = Substitute.For<IVerificacionDisponibilidadService>();
        private readonly FakeTimeProvider _reloj = new(new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero));
        private readonly IMapper _mapper = new Mapper(MapsterConfig.RegisterMaps());
        private static readonly DateOnly Fecha = new(2030, 1, 2);

        public ReservaDisponibilidadTests()
        {
            _laboratorios.ObtenerPorIdAsync(1).Returns(new Laboratorio { Id = 1, Estado = ReglasEstadoLaboratorio.Habilitado });
            _horarios.ObtenerPorLaboratorioAsync(1).Returns(new List<Disponibilidad>
            {
                new() { Id = 10, LaboratorioId = 1, Fecha = Fecha.ToDateTime(TimeOnly.MinValue),
                    HoraInicio = TimeSpan.FromHours(8), HoraFin = TimeSpan.FromHours(12), Estado = "Disponible" }
            });
            _reservas.CrearAsync(Arg.Any<Reserva>()).Returns(c => { var r = c.Arg<Reserva>(); r.Id = 25; return r; });
            var real = new VerificacionDisponibilidadService(_horarios, _reservas, _laboratorios, _reloj, _mapper);
            _verificacion.VerificarAsync(Arg.Any<int>(), Arg.Any<ConsultaDisponibilidadRequest>())
                .Returns(c => real.VerificarAsync(c.Arg<int>(), c.Arg<ConsultaDisponibilidadRequest>()));
        }

        [Theory]
        [InlineData("inexistente", "laboratorioId", "El laboratorio no existe.")]
        [InlineData("fuera", "disponibilidad", "El laboratorio está fuera de servicio.")]
        [InlineData("sin-horarios", "disponibilidad", "No hay horarios registrados para esa fecha.")]
        [InlineData("no-disponible", "disponibilidad", "Ningún horario disponible cubre todo el rango solicitado.")]
        [InlineData("parcial", "disponibilidad", "Ningún horario disponible cubre todo el rango solicitado.")]
        [InlineData("activa", "conflicto", ReglasTraslape.MensajeConflicto)]
        public async Task Crear_RechazaAutoritativamenteSinGuardar(string caso, string clave, string mensaje)
        {
            if (caso == "inexistente") _laboratorios.ObtenerPorIdAsync(1).Returns((Laboratorio?)null);
            if (caso == "fuera") _laboratorios.ObtenerPorIdAsync(1).Returns(new Laboratorio { Id = 1, Estado = ReglasEstadoLaboratorio.FueraDeServicio });
            if (caso == "sin-horarios") _horarios.ObtenerPorLaboratorioAsync(1).Returns(new List<Disponibilidad>());
            if (caso is "no-disponible" or "parcial")
                _horarios.ObtenerPorLaboratorioAsync(1).Returns(new List<Disponibilidad>
                {
                    new() { LaboratorioId = 1, Fecha = Fecha.ToDateTime(TimeOnly.MinValue),
                        HoraInicio = TimeSpan.FromHours(8), HoraFin = TimeSpan.FromHours(caso == "parcial" ? 9 : 12),
                        Estado = caso == "no-disponible" ? "No disponible" : "Disponible" }
                });
            if (caso == "activa") _reservas.ObtenerActivasPorLaboratorioYFechaAsync(1, Fecha.ToDateTime(TimeOnly.MinValue))
                .Returns(new List<Reserva> { Existente("Activa", 1) });

            var resultado = await Servicio().CrearAsync(7, Solicitud());

            Assert.False(resultado.Exitoso);
            Assert.Equal(mensaje, Assert.Single(resultado.Errores).Value);
            Assert.Equal(clave, Assert.Single(resultado.Errores).Key);
            await _reservas.DidNotReceive().CrearAsync(Arg.Any<Reserva>());
            await _reservas.DidNotReceive().GuardarCambiosAsync();
            await _verificacion.Received(1).VerificarAsync(1, Arg.Any<ConsultaDisponibilidadRequest>());
            await _laboratorios.Received(1).ObtenerPorIdAsync(1);
            if (caso is "inexistente" or "fuera")
            {
                await _horarios.DidNotReceive().ObtenerPorLaboratorioAsync(Arg.Any<int>());
                await _reservas.DidNotReceive().ObtenerActivasPorLaboratorioYFechaAsync(Arg.Any<int>(), Arg.Any<DateTime>());
            }
            else
            {
                await _horarios.Received(1).ObtenerPorLaboratorioAsync(1);
                await _reservas.Received(1).ObtenerActivasPorLaboratorioYFechaAsync(1, Fecha.ToDateTime(TimeOnly.MinValue));
            }
        }

        [Theory]
        [InlineData(null, 1, 0)]
        [InlineData("Cancelada", 1, 0)]
        [InlineData("Activa", 2, 0)]
        [InlineData("Activa", 1, 1)]
        public async Task Crear_RangoCubierto_GuardaUnaVezSinBloqueosAjenos(string? estado, int laboratorio, int dias)
        {
            if (estado is not null)
            {
                var existente = Existente(estado, laboratorio);
                existente.Fecha = existente.Fecha.AddDays(dias);
                _reservas.ObtenerActivasPorLaboratorioYFechaAsync(1, Fecha.ToDateTime(TimeOnly.MinValue))
                    .Returns(new List<Reserva> { existente });
            }
            var resultado = await Servicio().CrearAsync(7, Solicitud());
            Assert.True(resultado.Exitoso);
            Assert.Equal(25, resultado.Reserva!.Id);
            await _reservas.Received(1).CrearAsync(Arg.Is<Reserva>(r => r.UsuarioId == 7 && r.LaboratorioId == 1));
            await _verificacion.Received(1).VerificarAsync(1, Arg.Any<ConsultaDisponibilidadRequest>());
            await _laboratorios.Received(1).ObtenerPorIdAsync(1);
            await _horarios.Received(1).ObtenerPorLaboratorioAsync(1);
            await _reservas.Received(1).ObtenerActivasPorLaboratorioYFechaAsync(1, Fecha.ToDateTime(TimeOnly.MinValue));
        }

        [Fact]
        public async Task Crear_SolicitudInvalida_NoConsultaVerificacion()
        {
            var resultado = await Servicio().CrearAsync(7, new(null, null, null, null));
            Assert.False(resultado.Exitoso);
            await _verificacion.DidNotReceive().VerificarAsync(Arg.Any<int>(), Arg.Any<ConsultaDisponibilidadRequest>());
            await _reservas.DidNotReceive().CrearAsync(Arg.Any<Reserva>());
        }

        [Fact]
        public async Task Verificar_FueraDeServicio_NoExponeHorariosNiConsultaReservas()
        {
            _laboratorios.ObtenerPorIdAsync(1).Returns(new Laboratorio { Id = 1, Estado = ReglasEstadoLaboratorio.FueraDeServicio });
            var resultado = await _verificacion.VerificarAsync(1, new(Fecha, TimeSpan.FromHours(9), TimeSpan.FromHours(10)));
            Assert.False(resultado.Disponible);
            Assert.Equal("El laboratorio está fuera de servicio.", resultado.Motivo);
            Assert.Empty(resultado.HorariosDelDia);
            Assert.Empty(resultado.Errores);
            await _horarios.DidNotReceive().ObtenerPorLaboratorioAsync(Arg.Any<int>());
            await _reservas.DidNotReceive().ObtenerActivasPorLaboratorioYFechaAsync(Arg.Any<int>(), Arg.Any<DateTime>());
        }

        private ReservaService Servicio() => ReservaServiceFactory.Crear(_reservas, _reloj, _mapper, _verificacion);
        private static CrearReservaRequest Solicitud() => new(1, Fecha, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        private static Reserva Existente(string estado, int laboratorio) => new()
        {
            LaboratorioId = laboratorio, Fecha = Fecha.ToDateTime(TimeOnly.MinValue), Estado = estado,
            HoraInicio = TimeSpan.FromHours(9), HoraFin = TimeSpan.FromHours(10)
        };
    }
}
