using SistemaReservas.API.Models;
using SistemaReservas.API.Reglas;

namespace SistemaReservas.Tests.Reglas
{
    // NFR2 - Pruebas unitarias de ReglasDisponibilidad (HU3).
    // Los casos y mensajes son los mismos de frontend/src/utils/disponibilidad.test.js;
    // la fecha de hoy se pasa fija.
    public class ReglasDisponibilidadTests
    {
        private static readonly DateOnly Hoy = new(2026, 10, 5);
        private static readonly DateOnly Fecha = new(2026, 10, 10);
        private static readonly TimeSpan Nueve = new(9, 0, 0);
        private static readonly TimeSpan Once = new(11, 0, 0);

        private static Disponibilidad CrearHorario(
            int id,
            DateOnly fecha,
            int horaInicio,
            int horaFin,
            string estado = "Disponible")
        {
            return new Disponibilidad
            {
                Id = id,
                LaboratorioId = 1,
                Fecha = fecha.ToDateTime(TimeOnly.MinValue),
                HoraInicio = TimeSpan.FromHours(horaInicio),
                HoraFin = TimeSpan.FromHours(horaFin),
                Estado = estado
            };
        }

        private static Laboratorio CrearLaboratorio(string estado)
        {
            return new Laboratorio { Id = 1, Nombre = "Laboratorio de Software", Estado = estado };
        }

        // ---- ValidarConsulta ----

        [Fact]
        public void ValidarConsulta_SinDatos_ExigeTodosLosCampos()
        {
            var errores = ReglasDisponibilidad.ValidarConsulta(null, null, null, null, Hoy);

            Assert.Equal(new Dictionary<string, string>
            {
                ["laboratorioId"] = "Selecciona un laboratorio.",
                ["fecha"] = "Ingresa la fecha a consultar.",
                ["horaInicio"] = "Ingresa la hora inicial.",
                ["horaFin"] = "Ingresa la hora final."
            }, errores);
        }

        [Fact]
        public void ValidarConsulta_FechaPasadaYRangoInvalido_DevuelveAmbosErrores()
        {
            var errores = ReglasDisponibilidad.ValidarConsulta(
                1, new DateOnly(2000, 1, 1), new TimeSpan(10, 0, 0), Nueve, Hoy);

            Assert.Equal(2, errores.Count);
            Assert.Equal("La fecha no puede ser anterior a hoy.", errores["fecha"]);
            Assert.Equal("La hora final debe ser mayor que la hora inicial.", errores["horaFin"]);
        }

        [Fact]
        public void ValidarConsulta_ConsultaCompleta_SinErrores()
        {
            Assert.Empty(ReglasDisponibilidad.ValidarConsulta(1, Fecha, Nueve, Once, Hoy));
        }

        [Fact]
        public void ValidarConsulta_FechaDeHoy_EsValida()
        {
            Assert.Empty(ReglasDisponibilidad.ValidarConsulta(1, Hoy, Nueve, Once, Hoy));
        }

        [Fact]
        public void ValidarConsulta_HoraFinIgualAInicio_EsInvalida()
        {
            var errores = ReglasDisponibilidad.ValidarConsulta(1, Fecha, Nueve, Nueve, Hoy);

            Assert.Equal("La hora final debe ser mayor que la hora inicial.", Assert.Single(errores).Value);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ValidarConsulta_LaboratorioNoValido_ExigeLaboratorio(int laboratorioId)
        {
            var errores = ReglasDisponibilidad.ValidarConsulta(laboratorioId, Fecha, Nueve, Once, Hoy);

            Assert.Equal("Selecciona un laboratorio.", errores["laboratorioId"]);
        }

        // ---- Reglas individuales ----

        [Theory]
        [InlineData(-1, true)]
        [InlineData(0, false)]
        [InlineData(1, false)]
        public void EsFechaPasada_ComparaConHoy(int diasDesdeHoy, bool esperado)
        {
            Assert.Equal(esperado, ReglasDisponibilidad.EsFechaPasada(Hoy.AddDays(diasDesdeHoy), Hoy));
        }

        [Theory]
        [InlineData(9, 11, true)]
        [InlineData(9, 9, false)]
        [InlineData(11, 9, false)]
        public void EsRangoHorasValido_HoraFinMayorQueInicio(int horaInicio, int horaFin, bool esperado)
        {
            Assert.Equal(
                esperado,
                ReglasDisponibilidad.EsRangoHorasValido(TimeSpan.FromHours(horaInicio), TimeSpan.FromHours(horaFin)));
        }

        [Theory]
        [InlineData(8, 12, "Disponible", true)]
        [InlineData(9, 11, "Disponible", true)]
        [InlineData(10, 12, "Disponible", false)]
        [InlineData(8, 10, "Disponible", false)]
        [InlineData(8, 12, "No disponible", false)]
        public void CubreRango_HorarioDisponibleQueAbarcaDe9A11(
            int horaInicio,
            int horaFin,
            string estado,
            bool esperado)
        {
            var horario = CrearHorario(1, Fecha, horaInicio, horaFin, estado);

            Assert.Equal(esperado, ReglasDisponibilidad.CubreRango(horario, Nueve, Once));
        }

        [Theory]
        [InlineData("Habilitado", false)]
        [InlineData("Fuera de servicio", true)]
        public void EstaFueraDeServicio_SegunEstadoDelLaboratorio(string estado, bool esperado)
        {
            Assert.Equal(esperado, ReglasDisponibilidad.EstaFueraDeServicio(CrearLaboratorio(estado)));
        }

        // ---- EvaluarDisponibilidad ----

        [Fact]
        public void EvaluarDisponibilidad_RangoCubiertoPorHorarioLibre_EstaDisponible()
        {
            var resultado = ReglasDisponibilidad.EvaluarDisponibilidad(
                [CrearHorario(1, Fecha, 8, 12)], Fecha, Nueve, Once, CrearLaboratorio("Habilitado"));

            Assert.True(resultado.Disponible);
            Assert.Equal("El horario de 08:00 a 12:00 está libre.", resultado.Motivo);
            Assert.Single(resultado.HorariosDelDia);
        }

        [Fact]
        public void EvaluarDisponibilidad_LaboratorioFueraDeServicio_NuncaEstaDisponible()
        {
            var resultado = ReglasDisponibilidad.EvaluarDisponibilidad(
                [CrearHorario(1, Fecha, 8, 12)], Fecha, Nueve, Once, CrearLaboratorio("Fuera de servicio"));

            Assert.False(resultado.Disponible);
            Assert.Equal("El laboratorio está fuera de servicio.", resultado.Motivo);
        }

        [Fact]
        public void EvaluarDisponibilidad_SinHorariosEnLaFecha_NoEstaDisponible()
        {
            var resultado = ReglasDisponibilidad.EvaluarDisponibilidad(
                [CrearHorario(1, Fecha.AddDays(1), 8, 12)], Fecha, Nueve, Once, CrearLaboratorio("Habilitado"));

            Assert.False(resultado.Disponible);
            Assert.Equal("No hay horarios registrados para esa fecha.", resultado.Motivo);
            Assert.Empty(resultado.HorariosDelDia);
        }

        [Fact]
        public void EvaluarDisponibilidad_NingunHorarioLibreCubreElRango_NoEstaDisponible()
        {
            var resultado = ReglasDisponibilidad.EvaluarDisponibilidad(
                [CrearHorario(1, Fecha, 8, 12, "No disponible"), CrearHorario(2, Fecha, 13, 15)],
                Fecha, Nueve, Once, CrearLaboratorio("Habilitado"));

            Assert.False(resultado.Disponible);
            Assert.Equal("Ningún horario disponible cubre todo el rango solicitado.", resultado.Motivo);
            Assert.Equal(2, resultado.HorariosDelDia.Count);
        }

        [Fact]
        public void EvaluarDisponibilidad_FiltraOtrasFechasYOrdenaPorHoraDeInicio()
        {
            var resultado = ReglasDisponibilidad.EvaluarDisponibilidad(
                [CrearHorario(3, Fecha, 13, 15), CrearHorario(1, Fecha.AddDays(1), 8, 12), CrearHorario(2, Fecha, 7, 12)],
                Fecha, Nueve, Once, CrearLaboratorio("Habilitado"));

            Assert.Equal([2, 3], resultado.HorariosDelDia.Select(h => h.Id));
            Assert.Equal("El horario de 07:00 a 12:00 está libre.", resultado.Motivo);
        }

        [Fact]
        public void EvaluarDisponibilidad_SinLaboratorio_NoRevisaElEstado()
        {
            var resultado = ReglasDisponibilidad.EvaluarDisponibilidad(
                [CrearHorario(1, Fecha, 8, 12)], Fecha, Nueve, Once, null);

            Assert.True(resultado.Disponible);
        }
    }
}
