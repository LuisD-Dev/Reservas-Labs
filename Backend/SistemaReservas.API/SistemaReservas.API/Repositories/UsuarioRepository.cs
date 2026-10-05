using Microsoft.Data.SqlClient;
using SistemaReservas.API.Data;
using SistemaReservas.API.Models;

namespace SistemaReservas.API.Repositories
{
    // NFR2 - Separar acceso a datos: implementación con SQL Server.
    // Las consultas son las mismas que antes estaban en AuthService.
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly DatabaseConnection _database;

        public UsuarioRepository(DatabaseConnection database)
        {
            _database = database;
        }

        public async Task<Usuario?> ObtenerPorUsernameAsync(string username)
        {
            using var connection = _database.CreateConnection();
            await connection.OpenAsync();

            const string sql = @"SELECT Id, Nombre, Username, PasswordHash, Rol, IntentosFallidos, BloqueadoHasta
                                 FROM Usuarios
                                 WHERE Username = @Username";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Username", username);

            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new Usuario
            {
                Id = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Username = reader.GetString(2),
                PasswordHash = reader.GetString(3),
                Rol = reader.GetString(4),
                IntentosFallidos = reader.GetInt32(5),
                BloqueadoHasta = reader.IsDBNull(6) ? null : reader.GetDateTime(6)
            };
        }

        public async Task<int> RegistrarIntentoFallidoAsync(int usuarioId)
        {
            using var connection = _database.CreateConnection();
            await connection.OpenAsync();

            const string sql = @"UPDATE Usuarios
                                 SET IntentosFallidos = IntentosFallidos + 1
                                 OUTPUT INSERTED.IntentosFallidos
                                 WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", usuarioId);

            var resultado = await command.ExecuteScalarAsync();
            return Convert.ToInt32(resultado);
        }

        public async Task ReiniciarIntentosAsync(int usuarioId)
        {
            using var connection = _database.CreateConnection();
            await connection.OpenAsync();

            const string sql = @"UPDATE Usuarios
                                 SET IntentosFallidos = 0,
                                     BloqueadoHasta = NULL
                                 WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", usuarioId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task BloquearAsync(int usuarioId, DateTime bloqueadoHasta)
        {
            using var connection = _database.CreateConnection();
            await connection.OpenAsync();

            const string sql = @"UPDATE Usuarios
                                 SET BloqueadoHasta = @BloqueadoHasta,
                                     IntentosFallidos = 0
                                 WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@BloqueadoHasta", bloqueadoHasta);
            command.Parameters.AddWithValue("@Id", usuarioId);

            await command.ExecuteNonQueryAsync();
        }
    }
}
