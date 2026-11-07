namespace SistemaReservas.API.Reglas
{
    // HU8 - AB#100: quién puede cancelar una reserva.
    // Regla pura: no usa base de datos, HTTP ni reloj.
    public static class ReglasCancelacion
    {
        // El propietario cancela la suya; el Administrador puede cancelar cualquiera.
        public static bool PuedeCancelar(
            int usuarioDeLaReserva,
            int usuarioSolicitante,
            bool esAdministrador)
        {
            return esAdministrador || usuarioDeLaReserva == usuarioSolicitante;
        }
    }
}