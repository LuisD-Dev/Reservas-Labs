using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using SistemaReservas.API.Data;
using SistemaReservas.API.DTOs;
using SistemaReservas.API.Models;

namespace SistemaReservas.API.Services
{
    public class AuthService
    {
        private readonly DatabaseConnection _database;
        private readonly PasswordHasher<Usuario> _passwordHasher = new();

        public AuthService(DatabaseConnection database)
        {
            _database = database;
        }

        // HU1 - #19 Implementar autenticación: validar usuario/password
        public async Task<LoginResult> LoginAsync(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return new LoginResult(LoginEstado.CredencialesInvalidas);
            }

            using var connection = _database.CreateConnection();
            await connection.OpenAsync();

            var usuario = await ObtenerUsuarioAsync(connection, request.Username);

            // Mismo resultado si el usuario no existe o si la contraseña es incorrecta,
            // para no revelar cuál de los dos datos falló.
            if (usuario is null)
            {
                return new LoginResult(LoginEstado.CredencialesInvalidas);
            }

            if (!VerificarPassword(usuario, request.Password))
            {
                // HU1 - #20 Implementar intentos fallidos: llevar contador
                await RegistrarIntentoFallidoAsync(connection, usuario.Id);
                return new LoginResult(LoginEstado.CredencialesInvalidas);
            }

            // Login exitoso: el contador vuelve a cero.
            if (usuario.IntentosFallidos > 0)
            {
                await ReiniciarIntentosAsync(connection, usuario.Id);
            }

            return new LoginResult(LoginEstado.Exitoso, usuario.Id, usuario.Nombre, usuario.Rol);
        }

        // Genera el hash que se debe guardar en la columna PasswordHash.
        public string HashPassword(string password)
        {
            return _passwordHasher.HashPassword(new Usuario(), password);
        }

        private bool VerificarPassword(Usuario usuario, string password)
        {
            var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password);
            return resultado != PasswordVerificationResult.Failed;
        }

        // Suma 1 al contador en la base de datos y devuelve el nuevo valor.
        private static async Task<int> RegistrarIntentoFallidoAsync(SqlConnection connection, int usuarioId)
        {
            const string sql = @"UPDATE Usuarios
                                 SET IntentosFallidos = IntentosFallidos + 1
                                 OUTPUT INSERTED.IntentosFallidos
                                 WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", usuarioId);

            var resultado = await command.ExecuteScalarAsync();
            return Convert.ToInt32(resultado);
        }

        private static async Task ReiniciarIntentosAsync(SqlConnection connection, int usuarioId)
        {
            const string sql = @"UPDATE Usuarios
                                 SET IntentosFallidos = 0
                                 WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", usuarioId);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task<Usuario?> ObtenerUsuarioAsync(SqlConnection connection, string username)
        {
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
    }
}
