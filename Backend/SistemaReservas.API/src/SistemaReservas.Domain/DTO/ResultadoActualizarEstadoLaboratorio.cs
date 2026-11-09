namespace SistemaReservas.Domain.DTO
{
    public enum EstadoActualizacionLaboratorio
    {
        Exitosa,
        DatosInvalidos,
        NoEncontrado
    }

    public record ResultadoActualizarEstadoLaboratorio(
        EstadoActualizacionLaboratorio Estado,
        string Mensaje,
        LaboratorioResponse? Laboratorio = null);
}
