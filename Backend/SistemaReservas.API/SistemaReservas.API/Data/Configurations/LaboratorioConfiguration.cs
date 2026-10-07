using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaReservas.API.Models;

namespace SistemaReservas.API.Data.Configurations
{
    public class LaboratorioConfiguration : IEntityTypeConfiguration<Laboratorio>
    {
        public void Configure(EntityTypeBuilder<Laboratorio> builder)
        {
            builder.ToTable("Laboratorios");
            builder.HasKey(l => l.Id);

            builder.Property(l => l.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(l => l.Ubicacion)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(l => l.Capacidad)
                .IsRequired();

            builder.Property(l => l.Estado)
                .IsRequired()
                .HasMaxLength(30);

            builder.HasCheckConstraint("CK_Laboratorios_Capacidad", "Capacidad > 0");
            builder.HasCheckConstraint("CK_Laboratorios_Estado", "Estado IN ('Habilitado','Fuera de servicio')");

            builder.HasMany(l => l.Disponibilidades)
                .WithOne(d => d.Laboratorio)
                .HasForeignKey(d => d.LaboratorioId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
