using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaReservas.API.Models;

namespace SistemaReservas.API.Data.Configurations
{
    public class DisponibilidadConfiguration : IEntityTypeConfiguration<Disponibilidad>
    {
        public void Configure(EntityTypeBuilder<Disponibilidad> builder)
        {
            builder.ToTable("Disponibilidades");
            builder.HasKey(d => d.Id);

            builder.Property(d => d.LaboratorioId)
                .IsRequired();

            builder.Property(d => d.Fecha)
                .HasColumnType("date")
                .IsRequired();

            builder.Property(d => d.HoraInicio)
                .HasColumnType("time")
                .IsRequired();

            builder.Property(d => d.HoraFin)
                .HasColumnType("time")
                .IsRequired();

            builder.Property(d => d.Estado)
                .IsRequired()
                .HasMaxLength(30);

            builder.HasOne(d => d.Laboratorio)
                .WithMany(l => l.Disponibilidades)
                .HasForeignKey(d => d.LaboratorioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasCheckConstraint("CK_Disponibilidades_Estado", "Estado IN ('Disponible','No disponible')");
        }
    }
}
