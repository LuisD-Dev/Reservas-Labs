using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaReservas.API.Models;

namespace SistemaReservas.API.Data.Configurations
{
    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("Usuarios");
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(u => u.Username).IsUnique();

            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(u => u.Rol)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(u => u.IntentosFallidos)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(u => u.BloqueadoHasta)
                .IsRequired(false);

            builder.HasCheckConstraint("CK_Usuarios_Rol", "Rol IN ('Administrador','Usuario')");
        }
    }
}
