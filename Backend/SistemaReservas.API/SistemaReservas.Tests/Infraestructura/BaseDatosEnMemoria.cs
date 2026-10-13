using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SistemaReservas.API.Data;

namespace SistemaReservas.Tests.Infraestructura
{
    // Taller V - Base de datos SQLite en memoria para probar los repositorios de EF Core
    // sin SQL Server. El esquema se crea con el mismo modelo (configuraciones, check
    // constraints e índice único) y vive mientras la conexión esté abierta.
    // Usa SistemaReservasDbContextSqlite por la limitación de SQLite con TimeSpan.
    public sealed class BaseDatosEnMemoria : IDisposable
    {
        private readonly SqliteConnection _conexion;
        private readonly DbContextOptions<SistemaReservasDbContext> _opciones;

        public BaseDatosEnMemoria()
        {
            _conexion = new SqliteConnection("DataSource=:memory:");
            _conexion.Open();

            _opciones = new DbContextOptionsBuilder<SistemaReservasDbContext>()
                .UseSqlite(_conexion)
                .Options;

            using var contexto = CrearContexto();
            contexto.Database.EnsureCreated();
        }

        // Cada llamada devuelve un contexto nuevo sobre la misma base de datos,
        // así lo que se lee no depende de lo que otro contexto tenga en memoria.
        public SistemaReservasDbContext CrearContexto()
        {
            return new SistemaReservasDbContextSqlite(_opciones);
        }

        public void Dispose()
        {
            _conexion.Dispose();
        }
    }
}
