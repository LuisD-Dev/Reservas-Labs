using SistemaReservas.Application.Abstractions.Services;
using SistemaReservas.Application.Abstractions.Persistence;
using SistemaReservas.Application.DTOs;
using SistemaReservas.Domain.Rules;

namespace SistemaReservas.Application.Services
{
    public class VerificacionDisponibilidadService(
        IDisponibilidadRepository disponibilidades,
        IReservaRepository reservas,
        ILaboratorioRepository laboratorios,
        TimeProvider reloj) : IVerificacionDisponibilidadService
    {
        public async Task<VerificacionDisponibilidadResponse> VerificarAsync(
            int laboratorioId, ConsultaDisponibilidadRequest request)
        {
            var errores = ReglasDisponibilidad.ValidarConsulta(
                laboratorioId, request.Fecha, request.HoraInicio, request.HoraFin,
                DateOnly.FromDateTime(reloj.GetLocalNow().DateTime));
            if (errores.Count > 0) return new(false, "", [], errores);

            var fecha = request.Fecha!.Value;
            var inicio = request.HoraInicio!.Value;
            var fin = request.HoraFin!.Value;
            var laboratorio = (await laboratorios.ObtenerTodosAsync())
                .SingleOrDefault(l => l.Id == laboratorioId);
            if (laboratorio is null)
            {
                errores["laboratorioId"] = "Selecciona un laboratorio.";
                return new(false, "", [], errores);
            }

            var horarios = await disponibilidades.ObtenerPorLaboratorioAsync(laboratorioId);
            var reservasDelDia = await reservas.ObtenerActivasPorLaboratorioYFechaAsync(
                laboratorioId, fecha.ToDateTime(TimeOnly.MinValue));
            var evaluacion = ReglasDisponibilidad.EvaluarDisponibilidad(
                horarios.Where(h => h.LaboratorioId == laboratorioId), fecha, inicio, fin, laboratorio);
            var conflicto = ReglasDisponibilidad.TieneConflicto(
                reservasDelDia, laboratorioId, fecha, inicio, fin);
            // Solo horarios públicos: nunca se serializan reservas, usuarios ni navegaciones EF.
            var tramos = ReglasDisponibilidad.ObtenerTramosEfectivos(
                evaluacion.HorariosDelDia, reservasDelDia, laboratorioId, fecha,
                !ReglasDisponibilidad.EstaFueraDeServicio(laboratorio));
            var horariosPublicos = tramos.Select(h =>
                new HorarioDisponibilidadResponse(h.Id, fecha, h.HoraInicio, h.HoraFin,
                    h.Estado)).ToList();
            return new(evaluacion.Disponible && !conflicto,
                evaluacion.Disponible && conflicto ? ReservaService.MensajeConflicto : evaluacion.Motivo,
                horariosPublicos, errores);
        }
    }
}
