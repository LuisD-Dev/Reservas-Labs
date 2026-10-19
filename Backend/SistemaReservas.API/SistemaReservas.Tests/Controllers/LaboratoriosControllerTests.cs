using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SistemaReservas.API.Controllers;
using SistemaReservas.API.Models;
using SistemaReservas.API.Repositories;
using SistemaReservas.API.Services;

namespace SistemaReservas.Tests.Controllers
{
    // Taller V - Pruebas unitarias de LaboratoriosController (HU2): GET /api/laboratorios.
    // LaboratorioService es una clase concreta: se usa la real sobre un repositorio
    // sustituto (NSubstitute). El [Authorize] lo aplica el pipeline HTTP, fuera de esta prueba.
    // Patrón AAA: el Arrange común (repositorio sustituto y controlador) está en el constructor.
    public class LaboratoriosControllerTests
    {
        private readonly ILaboratorioRepository _repositorio = Substitute.For<ILaboratorioRepository>();
        private readonly LaboratoriosController _controller;

        public LaboratoriosControllerTests()
        {
            _controller = new LaboratoriosController(new LaboratorioService(_repositorio));
        }

        [Fact]
        public async Task ObtenerLaboratorios_ConLaboratorios_Devuelve200ConLista()
        {
            // Arrange
            var laboratorios = new List<Laboratorio>
            {
                new() { Id = 2, Nombre = "Laboratorio de Redes", Ubicacion = "Edificio B", Capacidad = 25, Estado = "Habilitado" },
                new() { Id = 1, Nombre = "Laboratorio de Software", Ubicacion = "Edificio A", Capacidad = 30, Estado = "Habilitado" }
            };
            _repositorio.ObtenerTodosAsync().Returns(laboratorios);

            // Act
            var respuesta = await _controller.ObtenerLaboratorios();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(respuesta);
            Assert.Equal(200, ok.StatusCode);
            Assert.Same(laboratorios, ok.Value);
        }

        [Fact]
        public async Task ObtenerLaboratorios_SinLaboratorios_Devuelve200ConListaVacia()
        {
            // Arrange
            _repositorio.ObtenerTodosAsync().Returns(new List<Laboratorio>());

            // Act
            var respuesta = await _controller.ObtenerLaboratorios();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(respuesta);
            Assert.Equal(200, ok.StatusCode);
            Assert.Empty(Assert.IsType<List<Laboratorio>>(ok.Value));
        }
    }
}
