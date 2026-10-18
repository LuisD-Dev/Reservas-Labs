using Microsoft.EntityFrameworkCore;
using SistemaReservas.API.Models;
using SistemaReservas.API.Repositories;
using SistemaReservas.Tests.Infraestructura;

namespace SistemaReservas.Tests.Repositories
{
    public sealed class ReservaRepositoryTests : IDisposable
    {
        private readonly BaseDatosEnMemoria _baseDatos =
            new();

        private readonly int _usuarioId;
        private readonly int _laboratorioId;

        public ReservaRepositoryTests()
        {
            using var contexto =
                _baseDatos.CrearContexto();

            var usuario = new Usuario
            {
                Nombre = "Usuario responsable",
                Username = "responsable",
                PasswordHash = "hash",
                Rol = "Usuario"
            };

            var laboratorio = new Laboratorio
            {
                Nombre = "Laboratorio de Software",
                Ubicacion = "Edificio A",
                Capacidad = 30,
                Estado = "Habilitado"
            };

            contexto.Usuarios.Add(usuario);
            contexto.Laboratorios.Add(laboratorio);
            contexto.SaveChanges();

            _usuarioId = usuario.Id;
            _laboratorioId = laboratorio.Id;
        }

        public void Dispose()
        {
            _baseDatos.Dispose();
        }

        [Fact]
        public async Task ObtenerActivasPorLaboratorioYFechaAsync_FiltraLaboratorioFechaYEstado()
        {
            // Arrange
            var fechaObjetivo = new DateTime(2026, 10, 20);
            int reservaEsperadaId;

            using (var contexto = _baseDatos.CrearContexto())
            {
                var otroLaboratorio = new Laboratorio
                {
                    Nombre = "Laboratorio de Redes",
                    Ubicacion = "Edificio B",
                    Capacidad = 20,
                    Estado = "Habilitado"
                };

                contexto.Laboratorios.Add(otroLaboratorio);
                await contexto.SaveChangesAsync();

                var reservaEsperada = new Reserva
                {
                    LaboratorioId = _laboratorioId,
                    UsuarioId = _usuarioId,
                    Fecha = fechaObjetivo,
                    HoraInicio = new TimeSpan(8, 0, 0),
                    HoraFin = new TimeSpan(9, 0, 0),
                    Estado = "Activa"
                };

                contexto.Reservas.AddRange(
                    reservaEsperada,
                    new Reserva
                    {
                        LaboratorioId = _laboratorioId,
                        UsuarioId = _usuarioId,
                        Fecha = fechaObjetivo,
                        HoraInicio = new TimeSpan(10, 0, 0),
                        HoraFin = new TimeSpan(11, 0, 0),
                        Estado = "Cancelada"
                    },
                    new Reserva
                    {
                        LaboratorioId = otroLaboratorio.Id,
                        UsuarioId = _usuarioId,
                        Fecha = fechaObjetivo,
                        HoraInicio = new TimeSpan(12, 0, 0),
                        HoraFin = new TimeSpan(13, 0, 0),
                        Estado = "Activa"
                    },
                    new Reserva
                    {
                        LaboratorioId = _laboratorioId,
                        UsuarioId = _usuarioId,
                        Fecha = fechaObjetivo.AddDays(1),
                        HoraInicio = new TimeSpan(14, 0, 0),
                        HoraFin = new TimeSpan(15, 0, 0),
                        Estado = "Activa"
                    });

                await contexto.SaveChangesAsync();
                reservaEsperadaId = reservaEsperada.Id;
            }

            using var contextoConsulta = _baseDatos.CrearContexto();
            var repositorio = new ReservaRepository(contextoConsulta);

            // Act
            var resultado = await repositorio.ObtenerActivasPorLaboratorioYFechaAsync(
                _laboratorioId,
                fechaObjetivo.AddHours(15));

            // Assert
            var reserva = Assert.Single(resultado);
            Assert.Equal(reservaEsperadaId, reserva.Id);
            Assert.Equal(_laboratorioId, reserva.LaboratorioId);
            Assert.Equal(fechaObjetivo, reserva.Fecha);
            Assert.Equal("Activa", reserva.Estado);
            Assert.Equal(new TimeSpan(8, 0, 0), reserva.HoraInicio);
            Assert.Equal(new TimeSpan(9, 0, 0), reserva.HoraFin);
        }

        [Fact]
        public async Task CrearAsync_ReservaValida_GuardaUsuarioResponsable()
        {
            // Arrange
            using var contexto =
                _baseDatos.CrearContexto();

            var repositorio =
                new ReservaRepository(contexto);

            var reserva = new Reserva
            {
                LaboratorioId = _laboratorioId,
                UsuarioId = _usuarioId,
                Fecha = new DateTime(2026, 10, 20),
                HoraInicio = new TimeSpan(9, 0, 0),
                HoraFin = new TimeSpan(11, 0, 0)
            };

            // Act
            var resultado =
                await repositorio.CrearAsync(reserva);

            // Assert
            Assert.Same(reserva, resultado);
            Assert.True(resultado.Id > 0);
            Assert.Equal(_usuarioId, resultado.UsuarioId);

            using var contextoConsulta =
                _baseDatos.CrearContexto();

            var guardada =
                await contextoConsulta.Reservas
                    .Include(r => r.Usuario)
                    .SingleAsync();

            Assert.Equal(_usuarioId, guardada.UsuarioId);
            Assert.Equal(
                "responsable",
                guardada.Usuario.Username);

            Assert.Equal("Activa", guardada.Estado);
        }
    }
}
