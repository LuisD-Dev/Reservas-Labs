using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SistemaReservas.Infrastructure.Data;
using Testcontainers.MsSql;

namespace SistemaReservas.IntegrationTests.Contenedores
{
    // HU10 - AB#116 AB#117: SQL Server real en un contenedor Docker para las pruebas
    // de integración (mismo patrón que MsSqlServerContainerWrapper del Taller de la Semana 5).
    // En lugar de scripts DDL se aplican las migraciones de EF Core del proyecto, así que
    // también se comprueba que las migraciones funcionan sobre SQL Server.
    // Se comparte un contenedor por clase de prueba (IClassFixture) y cada prueba
    // empieza con las tablas vacías (LimpiarAsync).
    public sealed class SqlServerContenedor : IAsyncLifetime
    {
        private const string NOMBRE_BD = "LaboratorioOBLDPruebas";

        private readonly MsSqlContainer _contenedor = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Reservas#2026") // SQL Server exige una contraseña fuerte para el usuario sa
            .Build();

        private string _cadenaConexion = string.Empty;

        // Inicia el contenedor y crea la base de datos con las migraciones.
        // xUnit lo ejecuta una sola vez antes de las pruebas de la clase.
        public async Task InitializeAsync()
        {
            await _contenedor.StartAsync();

            // El contenedor arranca conectado a master; EF Core crea la base de pruebas al migrar.
            _cadenaConexion = new SqlConnectionStringBuilder(_contenedor.GetConnectionString())
            {
                InitialCatalog = NOMBRE_BD
            }.ConnectionString;

            await using var contexto = CrearContexto();
            await contexto.Database.MigrateAsync();
        }

        // Cada llamada devuelve un contexto con una conexión nueva, como al reiniciar la API.
        public AppDbContext CrearContexto()
        {
            var opciones = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(_cadenaConexion)
                .Options;

            return new AppDbContext(opciones);
        }

        // Deja las tablas vacías para que cada prueba sea independiente de las demás.
        public async Task LimpiarAsync()
        {
            await using var contexto = CrearContexto();

            await contexto.Database.ExecuteSqlRawAsync("""
                DELETE FROM Reservas;
                DELETE FROM Disponibilidades;
                DELETE FROM Laboratorios;
                DELETE FROM Usuarios;
                """);
        }

        // Elimina el contenedor al terminar las pruebas de la clase.
        public async Task DisposeAsync()
        {
            await _contenedor.DisposeAsync();
        }
    }
}
