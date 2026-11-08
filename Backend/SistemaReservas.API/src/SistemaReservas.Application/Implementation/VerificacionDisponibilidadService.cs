using MapsterMapper;
using SistemaReservas.Application.Interfaces;
using SistemaReservas.Infrastructure.Interfaces;
using SistemaReservas.Domain.DTO;
using SistemaReservas.Domain.Rules;

namespace SistemaReservas.Application.Implementation
{
    public class VerificacionDisponibilidadService(
        IDisponibilidadRepository disponibilidades,
        IReservaRepository reservas,
        ILaboratorioRepository laboratorios,
        TimeProvider reloj, IMapper mapper) : IVerificacionDisponibilidadService
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
            var laboratorio = await laboratorios.ObtenerPorIdAsync(laboratorioId);
            if (laboratorio is null)
            {
                errores["laboratorioId"] = "El laboratorio no existe.";
                return new(false, "", [], errores);
            }

            if (ReglasDisponibilidad.EstaFueraDeServicio(laboratorio))
                return new(false, ReglasDisponibilidad.MensajeFueraDeServicio, [], errores);

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
            var horariosPublicos = mapper.Map<List<HorarioDisponibilidadResponse>>(tramos);
            return new(evaluacion.Disponible && !conflicto,
                evaluacion.Disponible && conflicto ? ReglasTraslape.MensajeConflicto : evaluacion.Motivo,
                horariosPublicos, errores);
        }
    }
}
