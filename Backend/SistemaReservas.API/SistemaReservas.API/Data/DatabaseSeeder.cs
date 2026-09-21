using Microsoft.Data.SqlClient;
using SistemaReservas.API.Services;

namespace SistemaReservas.API.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(
            DatabaseConnection database,
            AuthService authService)
        {
            using var connection = database.CreateConnection();
            await connection.OpenAsync();

            await CrearUsuarioSiNoExisteAsync(
                connection,
                authService,
                "Administrador",
                "admin",
                "1234",
                "Administrador"
            );

            await CrearUsuarioSiNoExisteAsync(
                connection,
                authService,
                "Usuario de prueba",
                "usuario",
                "1234",
                "Usuario"
            );
        }

        private static async Task CrearUsuarioSiNoExisteAsync(
            SqlConnection connection,
            AuthService authService,
            string nombre,
            string username,
            string password,
            string rol)
        {
            const string verificarSql = @"
                SELECT COUNT(*)
                FROM Usuarios
                WHERE Username = @Username";

            using var verificarCommand =
                new SqlCommand(verificarSql, connection);

            verificarCommand.Parameters.AddWithValue(
                "@Username",
                username
            );

            var existe = Convert.ToInt32(
                await verificarCommand.ExecuteScalarAsync()
            );

            if (existe > 0)
            {
                return;
            }

            var passwordHash =
                authService.HashPassword(password);

            const string insertarSql = @"
                INSERT INTO Usuarios
                (
                    Nombre,
                    Username,
                    PasswordHash,
                    Rol,
                    IntentosFallidos,
                    BloqueadoHasta
                )
                VALUES
                (
                    @Nombre,
                    @Username,
                    @PasswordHash,
                    @Rol,
                    0,
                    NULL
                )";

            using var insertarCommand =
                new SqlCommand(insertarSql, connection);

            insertarCommand.Parameters.AddWithValue(
                "@Nombre",
                nombre
            );

            insertarCommand.Parameters.AddWithValue(
                "@Username",
                username
            );

            insertarCommand.Parameters.AddWithValue(
                "@PasswordHash",
                passwordHash
            );

            insertarCommand.Parameters.AddWithValue(
                "@Rol",
                rol
            );

            await insertarCommand.ExecuteNonQueryAsync();
        }
    }
}