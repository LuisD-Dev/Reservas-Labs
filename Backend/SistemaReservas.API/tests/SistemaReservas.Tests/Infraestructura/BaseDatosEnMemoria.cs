using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SistemaReservas.Infrastructure.Data;

namespace SistemaReservas.Tests.Infraestructura
{
    // Taller V - Base de datos SQLite en memoria para probar los repositorios de EF Core
    // sin SQL Server. El esquema se crea con el mismo modelo (configuraciones, check
    // constraints e índice único) y vive mientras la conexión esté abierta.
    // Usa AppDbContextSqlite por la limitación de SQLite con TimeSpan.
    public sealed class BaseDatosEnMemoria : IDisposable
    {
        private readonly SqliteConnection _conexion;
        private readonly DbContextOptions<AppDbContext> _opciones;

        public BaseDatosEnMemoria()
        {
            _conexion = new SqliteConnection("DataSource=:memory:");
            _conexion.Open();

            _opciones = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_conexion)
                .Options;

            using var contexto = CrearContexto();
            contexto.Database.EnsureCreated();
        }

        // Cada llamada devuelve un contexto nuevo sobre la misma base de datos,
        // así lo que se lee no depende de lo que otro contexto tenga en memoria.
        public AppDbContext CrearContexto()
        {
            return new AppDbContextSqlite(_opciones);
        }

        public void Dispose()
        {
            _conexion.Dispose();
        }
    }
}
