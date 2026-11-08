using System.Text.Json;
using MapsterMapper;
using SistemaReservas.Application.Mapper;
using SistemaReservas.Domain.DTO;
using SistemaReservas.Domain.Entities;

namespace SistemaReservas.Tests.Application
{
    public class MapsterTests
    {
        private readonly IMapper _mapper = new Mapper(MapsterConfig.RegisterMaps());
        private static readonly DateTime Fecha = new(2026, 11, 10);

        [Fact]
        public void Laboratorio_MapeaTodosLosCamposPublicos()
        {
            var entidad = new Laboratorio { Id = 7, Nombre = "Redes", Ubicacion = "B", Capacidad = 25, Estado = "Habilitado" };
            Assert.Equal(new LaboratorioResponse(7, "Redes", "B", 25, "Habilitado"), _mapper.Map<LaboratorioResponse>(entidad));
        }

        [Fact]
        public void Disponibilidad_ConservaFechaDateTimeYHoras()
        {
            var entidad = Horario();
            Assert.Equal(new DisponibilidadResponse(3, 7, Fecha, TimeSpan.FromHours(8), TimeSpan.FromHours(12), "Disponible"),
                _mapper.Map<DisponibilidadResponse>(entidad));
        }

        [Fact]
        public void HorarioDisponibilidad_ConvierteSoloFechaADateOnly()
        {
            Assert.Equal(new HorarioDisponibilidadResponse(3, DateOnly.FromDateTime(Fecha), TimeSpan.FromHours(8), TimeSpan.FromHours(12), "Disponible"),
                _mapper.Map<HorarioDisponibilidadResponse>(Horario()));
        }

        [Fact]
        public void ReservaCreada_ContieneSoloDatosPublicos()
        {
            Assert.Equal(new ReservaCreadaResponse(5, 7, DateOnly.FromDateTime(Fecha), TimeSpan.FromHours(8), TimeSpan.FromHours(10), "Activa"),
                _mapper.Map<ReservaCreadaResponse>(Reserva()));
        }

        [Fact]
        public void MiReserva_MapeaNombreLaboratorioYFecha()
        {
            Assert.Equal(new MiReservaResponse(5, 7, "Redes", DateOnly.FromDateTime(Fecha), TimeSpan.FromHours(8), TimeSpan.FromHours(10), "Activa"),
                _mapper.Map<MiReservaResponse>(Reserva()));
        }

        [Fact]
        public void JsonDeSalida_NoExponeNavegacionesNiDatosPrivados()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            object[] respuestas = [_mapper.Map<LaboratorioResponse>(Reserva().Laboratorio),
                _mapper.Map<DisponibilidadResponse>(Horario()), _mapper.Map<ReservaCreadaResponse>(Reserva()),
                _mapper.Map<MiReservaResponse>(Reserva())];
            foreach (var respuesta in respuestas)
            {
                using var json = JsonDocument.Parse(JsonSerializer.Serialize(respuesta, options));
                foreach (var propiedad in json.RootElement.EnumerateObject())
                {
                    Assert.DoesNotContain(propiedad.Name, new[] { "usuario", "usuarioId", "passwordHash", "password", "laboratorio", "disponibilidades" });
                    Assert.NotEqual(JsonValueKind.Object, propiedad.Value.ValueKind);
                    Assert.NotEqual(JsonValueKind.Array, propiedad.Value.ValueKind);
                }
            }
            var disponibilidad = JsonSerializer.Serialize(_mapper.Map<DisponibilidadResponse>(Horario()), options);
            Assert.Contains("\"fecha\":\"2026-11-10T00:00:00\"", disponibilidad);
            var reserva = JsonSerializer.Serialize(_mapper.Map<ReservaCreadaResponse>(Reserva()), options);
            Assert.Contains("\"fecha\":\"2026-11-10\"", reserva);
            Assert.Contains("\"horaInicio\":\"08:00:00\"", reserva);
        }

        private static Disponibilidad Horario() => new()
        {
            Id = 3, LaboratorioId = 7, Fecha = Fecha,
            HoraInicio = TimeSpan.FromHours(8), HoraFin = TimeSpan.FromHours(12), Estado = "Disponible"
        };

        private static Reserva Reserva() => new()
        {
            Id = 5, LaboratorioId = 7, UsuarioId = 99, Fecha = Fecha,
            HoraInicio = TimeSpan.FromHours(8), HoraFin = TimeSpan.FromHours(10), Estado = "Activa",
            Laboratorio = new() { Id = 7, Nombre = "Redes" },
            Usuario = new() { Id = 99, Username = "privado", PasswordHash = "hash-privado" }
        };
    }
}
