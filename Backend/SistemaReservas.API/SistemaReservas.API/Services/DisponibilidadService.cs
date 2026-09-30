using Microsoft.Data.SqlClient;
using SistemaReservas.API.Data;
using SistemaReservas.API.Models;

namespace SistemaReservas.API.Services
{
    public class DisponibilidadService
    {
        private readonly DatabaseConnection _database;

        public DisponibilidadService(DatabaseConnection database)
        {
            _database = database;
        }


        public async Task<List<Disponibilidad>> ObtenerDisponibilidadAsync(
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