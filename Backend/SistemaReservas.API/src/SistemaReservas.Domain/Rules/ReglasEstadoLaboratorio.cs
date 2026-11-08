namespace SistemaReservas.Domain.Rules
{
    public static class ReglasEstadoLaboratorio
    {
        public const string Habilitado = "Habilitado";
        public const string FueraDeServicio = "Fuera de servicio";

        public static bool EsEstadoValido(string? estado)
        {
            return estado == Habilitado || estado == FueraDeServicio;
        }
    }
}
