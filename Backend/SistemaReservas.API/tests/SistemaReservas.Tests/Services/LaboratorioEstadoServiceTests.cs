using MapsterMapper;
using NSubstitute;
using SistemaReservas.Application.Implementation;
using SistemaReservas.Application.Mapper;
using SistemaReservas.Domain.DTO;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Infrastructure.Interfaces;

namespace SistemaReservas.Tests.Services
{
    public class LaboratorioEstadoServiceTests
    {
        private readonly ILaboratorioRepository _repository = Substitute.For<ILaboratorioRepository>();
        private LaboratorioService Servicio() => new(_repository, new Mapper(MapsterConfig.RegisterMaps()));

        [Theory]
        [InlineData("Habilitado", "Fuera de servicio")]
        [InlineData("Fuera de servicio", "Habilitado")]
        [InlineData("Habilitado", "Habilitado")]
        [InlineData("Fuera de servicio", "Fuera de servicio")]
        public async Task Actualizar_CambiaSoloEstadoYAdmiteEstadoActual(string inicial, string solicitado)
        {
            var laboratorio = new Laboratorio { Id = 12, Nombre = "Computo", Ubicacion = "A", Capacidad = 30, Estado = inicial };
            var navegacion = laboratorio.Disponibilidades;
            _repository.ObtenerPorIdAsync(12).Returns(laboratorio);

            var resultado = await Servicio().ActualizarEstadoAsync(12, solicitado);

            Assert.Equal(EstadoActualizacionLaboratorio.Exitosa, resultado.Estado);
            Assert.Equal("Estado del laboratorio actualizado correctamente.", resultado.Mensaje);
            Assert.Equal(new LaboratorioResponse(12, "Computo", "A", 30, solicitado), resultado.Laboratorio);
            Assert.Equal(12, laboratorio.Id);
            Assert.Equal("Computo", laboratorio.Nombre);
            Assert.Equal("A", laboratorio.Ubicacion);
            Assert.Equal(30, laboratorio.Capacidad);
            Assert.Equal(solicitado, laboratorio.Estado);
            Assert.Same(navegacion, laboratorio.Disponibilidades);
            await _repository.Received(1).GuardarCambiosAsync();
        }

        [Fact]
        public async Task Actualizar_Inexistente_NoGuarda()
        {
            var resultado = await Servicio().ActualizarEstadoAsync(999, "Habilitado");
            Assert.Equal(EstadoActualizacionLaboratorio.NoEncontrado, resultado.Estado);
            Assert.Null(resultado.Laboratorio);
            await _repository.DidNotReceive().GuardarCambiosAsync();
        }

        [Theory]
        [InlineData(0, "Habilitado")]
        [InlineData(-1, "Habilitado")]
        [InlineData(1, null)]
        [InlineData(1, "")]
        [InlineData(1, "   ")]
        [InlineData(1, "Disponible")]
        public async Task Actualizar_DatosInvalidos_NoConsultaNiGuarda(int id, string? estado)
        {
            var resultado = await Servicio().ActualizarEstadoAsync(id, estado);
            Assert.Equal(EstadoActualizacionLaboratorio.DatosInvalidos, resultado.Estado);
            Assert.Null(resultado.Laboratorio);
            await _repository.DidNotReceive().ObtenerPorIdAsync(Arg.Any<int>());
            await _repository.DidNotReceive().GuardarCambiosAsync();
        }
    }
}
