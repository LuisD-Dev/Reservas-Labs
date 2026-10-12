namespace SistemaReservas.API.Reglas
{
    // NFR2 - Reglas de negocio aisladas: roles del sistema.
    // Los textos coinciden con el CHECK CK_Usuarios_Rol de la base de datos.
    // Son const para poder usarlos en [Authorize(Roles = ...)].
    public static class ReglasRoles
    {
        public const string Administrador = "Administrador";
        public const string Usuario = "Usuario";

        // Indica si el rol es uno de los roles del sistema.
        // Distingue mayúsculas, igual que la autorización por rol del JWT.
        public static bool EsRolValido(string? rol)
        {
            return rol == Administrador || rol == Usuario;
        }
    }
}
