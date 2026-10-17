using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaReservas.API.Models;

namespace SistemaReservas.API.Data.Configurations
{
    public class ReservaConfiguration :
        IEntityTypeConfiguration<Reserva>
    {
        public void Configure(
            EntityTypeBuilder<Reserva> builder)
        {
            builder.ToTable("Reservas");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.LaboratorioId)
                .IsRequired();

            builder.Property(r => r.UsuarioId)
                .IsRequired();

            builder.Property(r => r.Fecha)
                .HasColumnType("date")
                .IsRequired();

            builder.Property(r => r.HoraInicio)
                .HasColumnType("time")
                .IsRequired();

            builder.Property(r => r.HoraFin)
                .HasColumnType("time")
                .IsRequired();

            builder.Property(r => r.Estado)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Activa");

            builder.HasOne(r => r.Laboratorio)
                .WithMany()
                .HasForeignKey(r => r.LaboratorioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Usuario)
                .WithMany()
                .HasForeignKey(r => r.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => new
            {
                r.LaboratorioId,
                r.Fecha,
                r.Estado
            });

            builder.ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Reservas_Estado",
                    "Estado IN ('Activa','Cancelada')"));
        }
    }
}
