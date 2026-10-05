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

            await CrearLaboratoriosSiNoExistenAsync(connection);

            await CrearDisponibilidadesSiNoExistenAsync(connection);
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


        private static async Task CrearLaboratoriosSiNoExistenAsync(
            SqlConnection connection)
        {
            const string verificarSql = @"
                SELECT COUNT(*)
                FROM Laboratorios";


            using var verificarCommand =
                new SqlCommand(verificarSql, connection);


            var existe = Convert.ToInt32(
                await verificarCommand.ExecuteScalarAsync()
            );


            if (existe > 0)
            {
                return;
            }


            const string insertarSql = @"
                INSERT INTO Laboratorios
                (
                    Nombre,
                    Ubicacion,
                    Capacidad,
                    Estado
                )
                VALUES
                (
                    'Laboratorio de Software',
                    'Edificio A',
                    30,
                    'Habilitado'
                ),
                (
                    'Laboratorio de Redes',
                    'Edificio B',
                    25,
                    'Habilitado'
                )";


            using var insertarCommand =
                new SqlCommand(insertarSql, connection);


            await insertarCommand.ExecuteNonQueryAsync();
        }


        private static async Task CrearDisponibilidadesSiNoExistenAsync(
            SqlConnection connection)
        {
            const string verificarSql = @"
                SELECT COUNT(*)
                FROM Disponibilidades";


            using var verificarCommand =
                new SqlCommand(verificarSql, connection);


            var existe = Convert.ToInt32(
                await verificarCommand.ExecuteScalarAsync()
            );


            if (existe > 0)
            {
                return;
            }


            const string insertarSql = @"
                INSERT INTO Disponibilidades
                (
                    LaboratorioId,
                    Fecha,
                    HoraInicio,
                    HoraFin,
                    Estado
                )
                VALUES
                (
                    1,
                    '2026-10-01',
                    '08:00',
                    '10:00',
                    'Disponible'
                ),
                (
                    1,
                    '2026-10-01',
                    '10:00',
                    '12:00',
                    'No disponible'
                ),
                (
                    2,
                    '2026-10-01',
                    '13:00',
                    '15:00',
                    'Disponible'
                )";


            using var insertarCommand =
                new SqlCommand(insertarSql, connection);


            await insertarCommand.ExecuteNonQueryAsync();
        }
    }
}