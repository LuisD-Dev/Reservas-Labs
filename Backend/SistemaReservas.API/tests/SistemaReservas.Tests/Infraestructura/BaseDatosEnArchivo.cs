using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SistemaReservas.Infrastructure.Data;

namespace SistemaReservas.Tests.Infraestructura
{
    // HU10 - AB#116: base de datos SQLite guardada en un archivo temporal.
    // A diferencia de BaseDatosEnMemoria, aquí no queda ninguna conexión abierta:
    // cada contexto abre y cierra la suya (sin pool), así que lo que se lee después
    // de cerrar un contexto viene del archivo y no de la memoria.
    // Sirve para simular que la aplicación se cierra y vuelve a iniciar.
    public sealed class BaseDatosEnArchivo : IDisposable
    {
        private readonly string _carpeta;

        public BaseDatosEnArchivo()
        {
            _carpeta = Path.Combine(
                Path.GetTempPath(),
                "reservas-hu10-" + Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(_carpeta);

            RutaArchivo = Path.Combine(_carpeta, "reservas.db");

            using var contexto = CrearContexto();
            contexto.Database.EnsureCreated();
        }

        public string RutaArchivo { get; }

        // Cada llamada devuelve un contexto con una conexión nueva al mismo archivo.
        public AppDbContext CrearContexto()
        {
            var cadena = new SqliteConnectionStringBuilder
            {
                DataSource = RutaArchivo,
                Pooling = false
            }.ToString();

            var opciones = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(cadena)
                .Options;

            return new AppDbContextSqlite(opciones);
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(_carpeta))
            {
                Directory.Delete(_carpeta, recursive: true);
            }
        }
    }
}
