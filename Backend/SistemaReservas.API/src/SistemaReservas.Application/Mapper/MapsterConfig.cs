using Mapster;
using SistemaReservas.Domain.DTO;
using SistemaReservas.Domain.Entities;

namespace SistemaReservas.Application.Mapper
{
    public static class MapsterConfig
    {
        public static TypeAdapterConfig RegisterMaps()
        {
            var config = new TypeAdapterConfig();
            config.NewConfig<Laboratorio, LaboratorioResponse>();
            config.NewConfig<Disponibilidad, DisponibilidadResponse>();
            config.NewConfig<Reserva, ReservaCreadaResponse>()
                .Map(d => d.Fecha, s => DateOnly.FromDateTime(s.Fecha));
            config.NewConfig<Reserva, MiReservaResponse>()
                .Map(d => d.NombreLaboratorio, s => s.Laboratorio.Nombre)
                .Map(d => d.Fecha, s => DateOnly.FromDateTime(s.Fecha));
            config.NewConfig<Disponibilidad, HorarioDisponibilidadResponse>()
                .Map(d => d.Fecha, s => DateOnly.FromDateTime(s.Fecha));
            config.Compile();
            return config;
        }
    }
}
