using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SistemaReservas.API.Controllers;
using SistemaReservas.API.Models;
using SistemaReservas.API.Repositories;
using SistemaReservas.API.Services;

namespace SistemaReservas.Tests.Controllers
{
    // Taller V - Pruebas unitarias de DisponibilidadController (HU3):
    // GET /api/laboratorios/{laboratorioId}/disponibilidad.
    // DisponibilidadService es una clase concreta: se usa la real sobre un repositorio
    // sustituto (NSubstitute). El [Authorize] lo aplica el pipeline HTTP, fuera de esta prueba.
    // Patrón AAA: el Arrange común (repositorio sustituto y controlador) está en el constructor.
    public class DisponibilidadControllerTests
    {
        private readonly IDisponibilidadRepository _repositorio = Substitute.For<IDisponibilidadRepository>();
        private readonly DisponibilidadController _controller;

        public DisponibilidadControllerTests()
        {
            _controller = new DisponibilidadController(new DisponibilidadService(_repositorio));
        }

        [Fact]
        public async Task ObtenerDisponibilidad_LaboratorioConHorarios_Devuelve200ConHorarios()
        {
            // Arrange
            var horarios = new List<Disponibilidad>
            {
                new()
                {
                    Id = 1,
                    LaboratorioId = 1,
                    Fecha = new DateTime(2026, 10, 13),
                    HoraInicio = new TimeSpan(8, 0, 0),
                    HoraFin = new TimeSpan(12, 0, 0),
                    Estado = "Disponible"
                }
            };
            _repositorio.ObtenerPorLaboratorioAsync(1).Returns(horarios);

            // Act
            var respuesta = await _controller.ObtenerDisponibilidad(1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(respuesta);
            Assert.Equal(200, ok.StatusCode);
            Assert.Same(horarios, ok.Value);
            await _repositorio.Received(1).ObtenerPorLaboratorioAsync(1);
        }

        [Fact]
        public async Task ObtenerDisponibilidad_SinHorarios_Devuelve200ConListaVacia()
        {
            // Arrange
            _repositorio.ObtenerPorLaboratorioAsync(2).Returns(new List<Disponibilidad>());

            // Act
            var respuesta = await _controller.ObtenerDisponibilidad(2);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(respuesta);
            Assert.Equal(200, ok.StatusCode);
            Assert.Empty(Assert.IsType<List<Disponibilidad>>(ok.Value));
        }
    }
}
