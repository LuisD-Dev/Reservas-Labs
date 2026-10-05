using Microsoft.Data.SqlClient;
using SistemaReservas.API.Data;
using SistemaReservas.API.Models;

namespace SistemaReservas.API.Repositories
{
    // NFR2 - Separar acceso a datos: implementación con SQL Server.
    // La consulta es la misma que antes estaba en LaboratorioService.
    public class LaboratorioRepository : ILaboratorioRepository
    {
        private readonly DatabaseConnection _database;

        public LaboratorioRepository(DatabaseConnection database)
        {
            _database = database;
        }

        public async Task<List<Laboratorio>> ObtenerTodosAsync()
        {
            var laboratorios = new List<Laboratorio>();

            using var connection = _database.CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT Id, Nombre, Ubicacion, Capacidad, Estado
                FROM Laboratorios
                ORDER BY Nombre";

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                laboratorios.Add(new Laboratorio
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Ubicacion = reader.GetString(2),
                    Capacidad = reader.GetInt32(3),
                    Estado = reader.GetString(4)
                });
            }

            return laboratorios;
        }
    }
}
