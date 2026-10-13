using SistemaReservas.API.Reglas;

namespace SistemaReservas.Tests.Reglas
{
    // NFR2 - Pruebas unitarias de ReglasRoles.
    // Patrón AAA: en los [Theory] los datos de entrada vienen de [InlineData].
    public class ReglasRolesTests
    {
        [Fact]
        public void Constantes_CoincidenConLosRolesDeLaBaseDeDatos()
        {
            // Act & Assert
            Assert.Equal("Administrador", ReglasRoles.Administrador);
            Assert.Equal("Usuario", ReglasRoles.Usuario);
        }

        [Theory]
        [InlineData("Administrador")]
        [InlineData("Usuario")]
        public void EsRolValido_RolesDelSistema_DevuelveTrue(string rol)
        {
            // Act & Assert
            Assert.True(ReglasRoles.EsRolValido(rol));
        }

        [Theory]
        [InlineData("administrador")]
        [InlineData("USUARIO")]
        [InlineData("Invitado")]
        [InlineData("")]
        [InlineData(null)]
        public void EsRolValido_OtrosValores_DevuelveFalse(string? rol)
        {
            // Act & Assert
            Assert.False(ReglasRoles.EsRolValido(rol));
        }
    }
}
