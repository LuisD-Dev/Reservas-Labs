namespace SistemaReservas.Domain.Rules
{
    public static class ReglasEstadoLaboratorio
    {
        public static bool EsEstadoValido(string? estado)
        {
            return estado == "Habilitado" || estado == "Fuera de servicio";
        }
    }
}
