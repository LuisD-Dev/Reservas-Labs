using Microsoft.Data.SqlClient;
using SistemaReservas.API.Data;
using SistemaReservas.API.Models;

namespace SistemaReservas.API.Services
{
    public class LaboratorioService
    {
        private readonly DatabaseConnection _database;

        public LaboratorioService(DatabaseConnection database)
        {
            _database = database;
        }

        public async Task<List<Laboratorio>> ObtenerLaboratoriosAsync()
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