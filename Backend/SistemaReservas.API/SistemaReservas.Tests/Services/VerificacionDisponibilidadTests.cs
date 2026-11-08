using Microsoft.Extensions.Time.Testing;
using SistemaReservas.Application.DTOs;
using SistemaReservas.Application.Services;
using SistemaReservas.Domain.Entities;
using SistemaReservas.Domain.Rules;
using SistemaReservas.Infrastructure.Repositories;
using SistemaReservas.Tests.Infraestructura;
using System.Text.Json;

namespace SistemaReservas.Tests.Services
{
    public class VerificacionDisponibilidadTests
    {
        [Theory]
        [InlineData(null, 9, 10, false, 0, true)]
        [InlineData("Activa", 9, 10, false, 0, false)]
        [InlineData("Cancelada", 9, 10, false, 0, true)]
        [InlineData("Activa", 8, 9, false, 0, true)]
        [InlineData("Activa", 10, 11, false, 0, true)]
        [InlineData("Activa", 9, 10, true, 0, true)]
        [InlineData("Activa", 9, 10, false, 1, true)]
        public async Task VerificarAsync_UsaReservasPersistidasSinExponerDatosPersonales(
            string? estado, int inicio, int fin, bool otroLaboratorio, int dias, bool disponible)
        {
            using var db = new BaseDatosEnMemoria();
            using var context = db.CrearContexto();
            var fecha = new DateTime(2030, 1, 2);
            var laboratorio = new Laboratorio
            {
                Nombre = "Software", Ubicacion = "A", Capacidad = 20, Estado = "Habilitado"
            };
            var otro = new Laboratorio
            {
                Nombre = "Redes", Ubicacion = "B", Capacidad = 20, Estado = "Habilitado"
            };
            var usuario = new Usuario
            {
                Nombre = "Nombre privado", Username = "privado", PasswordHash = "hash", Rol = "Usuario"
            };
            context.Laboratorios.AddRange(laboratorio, otro);
            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();
            context.Disponibilidades.Add(new Disponibilidad
            {
                LaboratorioId = laboratorio.Id, Fecha = fecha,
                HoraInicio = TimeSpan.FromHours(8), HoraFin = TimeSpan.FromHours(12), Estado = "Disponible"
            });
            if (estado is not null) context.Reservas.Add(new Reserva
            {
                LaboratorioId = otroLaboratorio ? otro.Id : laboratorio.Id,
                UsuarioId = usuario.Id, Fecha = fecha.AddDays(dias),
                HoraInicio = TimeSpan.FromHours(inicio), HoraFin = TimeSpan.FromHours(fin), Estado = estado
            });
            await context.SaveChangesAsync();
            var servicio = new VerificacionDisponibilidadService(
                new DisponibilidadRepository(context), new ReservaRepository(context),
                new LaboratorioRepository(context),
                new FakeTimeProvider(new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero)));

            var resultado = await servicio.VerificarAsync(laboratorio.Id,
                new ConsultaDisponibilidadRequest(DateOnly.FromDateTime(fecha),
                    TimeSpan.FromHours(9), TimeSpan.FromHours(10)));

            Assert.Equal(disponible, resultado.Disponible);
            Assert.Empty(resultado.Errores);
            if (!disponible)
            {
                Assert.Equal(ReservaService.MensajeConflicto, resultado.Motivo);
                Assert.Contains(resultado.HorariosDelDia, h => h.Estado == "No disponible"
                    && h.HoraInicio == TimeSpan.FromHours(9) && h.HoraFin == TimeSpan.FromHours(10));
            }
            Assert.All(resultado.HorariosDelDia.Where(h => h.Estado == "Disponible"), h =>
                Assert.False(ReglasDisponibilidad.TieneConflicto(
                    context.Reservas.ToList(), laboratorio.Id, DateOnly.FromDateTime(fecha), h.HoraInicio, h.HoraFin)));
            var json = JsonSerializer.Serialize(resultado);
            Assert.DoesNotContain("Nombre privado", json);
            Assert.DoesNotContain("UsuarioId", json);
            Assert.DoesNotContain("PasswordHash", json);
        }

        [Theory]
        [InlineData(6, 8)]
        [InlineData(20, 22)]
        [InlineData(8, 13)]
        [InlineData(10, 9)]
        public void Validaciones_DeConsultaYRegistroCoinciden(int inicio, int fin)
        {
            var fecha = new DateOnly(2030, 1, 2);
            var consulta = ReglasDisponibilidad.ValidarConsulta(1, fecha,
                TimeSpan.FromHours(inicio), TimeSpan.FromHours(fin), fecha);
            var registro = ReglasReserva.ValidarSolicitud(1, fecha,
                TimeSpan.FromHours(inicio), TimeSpan.FromHours(fin), fecha);
            Assert.NotEmpty(consulta);
            Assert.Equal(registro, consulta);
        }
    }
}
