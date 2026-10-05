using Microsoft.Data.SqlClient;
using SistemaReservas.API.Data;
using SistemaReservas.API.Models;

namespace SistemaReservas.API.Repositories
{
    // NFR2 - Separar acceso a datos: implementación con SQL Server.
    // La consulta es la misma que antes estaba en DisponibilidadService.
    public class DisponibilidadRepository : IDisponibilidadRepository
    {
        private readonly DatabaseConnection _database;

        public DisponibilidadRepository(DatabaseConnection database)
        {
            _database = database;
        }


        public async Task<List<Disponibilidad>> ObtenerPorLaboratorioAsync(
            int laboratorioId)
        {
            var disponibilidades = new List<Disponibilidad>();

            using var connection = _database.CreateConnection();
            await connection.OpenAsync();


            const string sql = @"
                SELECT 
                    Id,
                    LaboratorioId,
                    Fecha,
                    HoraInicio,
                    HoraFin,
                    Estado
                FROM Disponibilidades
                WHERE LaboratorioId = @LaboratorioId
                ORDER BY Fecha, HoraInicio";


            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue(
                "@LaboratorioId",
                laboratorioId
            );


            using var reader = await command.ExecuteReaderAsync();


            while (await reader.ReadAsync())
            {
                disponibilidades.Add(new Disponibilidad
                {
                    Id = reader.GetInt32(0),
                    LaboratorioId = reader.GetInt32(1),
                    Fecha = reader.GetDateTime(2),
                    HoraInicio = reader.GetTimeSpan(3),
                    HoraFin = reader.GetTimeSpan(4),
                    Estado = reader.GetString(5)
                });
            }


            return disponibilidades;
        }
    }
}
