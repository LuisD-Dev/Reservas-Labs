using Microsoft.EntityFrameworkCore;
using SistemaReservas.API.Models;

namespace SistemaReservas.API.Data
{
    public class SistemaReservasDbContext : DbContext
    {
        public SistemaReservasDbContext(DbContextOptions<SistemaReservasDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Laboratorio> Laboratorios { get; set; } = null!;
        public DbSet<Disponibilidad> Disponibilidades { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new Configurations.UsuarioConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.LaboratorioConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.DisponibilidadConfiguration());

            base.OnModelCreating(modelBuilder);
        }
    }
}
