using Microsoft.EntityFrameworkCore;
using SistemaReservas.API.Data;

namespace SistemaReservas.Tests.Infraestructura
{
    // Taller V - DbContext solo para pruebas con SQLite en memoria.
    // Usa el mismo modelo de producción: no redefine OnModelCreating, así que se aplican
    // tal cual las configuraciones de SistemaReservasDbContext (tablas, check constraints
    // e índice único), y ConfigureConventions llama primero a la configuración base.
    // Lo único que agrega es guardar TimeSpan como long (ticks): el proveedor SQLite de
    // EF Core no traduce ORDER BY sobre TimeSpan (DisponibilidadRepository ordena por
    // HoraInicio). No aplica a producción, donde SQL Server ordena la columna time sin problema.
    public class SistemaReservasDbContextSqlite : SistemaReservasDbContext
    {
        public SistemaReservasDbContextSqlite(DbContextOptions<SistemaReservasDbContext> options)
            : base(options)
        {
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            configurationBuilder.Properties<TimeSpan>().HaveConversion<long>();
        }
    }
}
