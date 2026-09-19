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

        // HU1 - #21 Bloqueo temporal: 5 intentos fallidos => 5 minutos bloqueado
        private const int MaxIntentosFallidos = 5;
        private static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(5);

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

            // HU1 - #21 Si el usuario sigue bloqueado, se rechaza sin revisar la contraseña.
            if (usuario.BloqueadoHasta is not null && usuario.BloqueadoHasta > DateTime.UtcNow)
            {
                return new LoginResult(LoginEstado.Bloqueado, BloqueadoHasta: usuario.BloqueadoHasta);
            }

            if (!VerificarPassword(usuario, request.Password))
            {
                // HU1 - #20 Implementar intentos fallidos: llevar contador
                var intentos = await RegistrarIntentoFallidoAsync(connection, usuario.Id);

                // HU1 - #21 Al llegar al 5.º intento fallido se bloquea el acceso.
                if (intentos >= MaxIntentosFallidos)
                {
                    var bloqueadoHasta = DateTime.UtcNow.Add(DuracionBloqueo);
                    await BloquearUsuarioAsync(connection, usuario.Id, bloqueadoHasta);
                    return new LoginResult(LoginEstado.Bloqueado, BloqueadoHasta: bloqueadoHasta);
                }

                return new LoginResult(LoginEstado.CredencialesInvalidas);
            }

            // Login exitoso: el contador vuelve a cero y se limpia cualquier bloqueo vencido.
            if (usuario.IntentosFallidos > 0 || usuario.BloqueadoHasta is not null)
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
                                 SET IntentosFallidos = 0,
                                     BloqueadoHasta = NULL
                                 WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", usuarioId);

            await command.ExecuteNonQueryAsync();
        }

        // Guarda hasta cuándo queda bloqueado y reinicia el contador
        // para que, al vencer el bloqueo, tenga otros 5 intentos.
        private static async Task BloquearUsuarioAsync(SqlConnection connection, int usuarioId, DateTime bloqueadoHasta)
        {
            const string sql = @"UPDATE Usuarios
                                 SET BloqueadoHasta = @BloqueadoHasta,
                                     IntentosFallidos = 0
                                 WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@BloqueadoHasta", bloqueadoHasta);
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
