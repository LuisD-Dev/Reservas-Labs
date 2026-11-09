using Microsoft.EntityFrameworkCore;
using SistemaReservas.Domain.Entities;

namespace SistemaReservas.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Laboratorio> Laboratorios { get; set; } = null!;
        public DbSet<Disponibilidad> Disponibilidades { get; set; } = null!;
        public DbSet<Reserva> Reservas { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new Configurations.UsuarioConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.LaboratorioConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.DisponibilidadConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.ReservaConfiguration());

            base.OnModelCreating(modelBuilder);
        }
    }
}
