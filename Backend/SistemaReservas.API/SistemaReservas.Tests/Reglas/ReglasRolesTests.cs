using SistemaReservas.API.Reglas;

namespace SistemaReservas.Tests.Reglas
{
    // NFR2 - Pruebas unitarias de ReglasRoles.
    public class ReglasRolesTests
    {
        [Fact]
        public void Constantes_CoincidenConLosRolesDeLaBaseDeDatos()
        {
            Assert.Equal("Administrador", ReglasRoles.Administrador);
            Assert.Equal("Usuario", ReglasRoles.Usuario);
        }

        [Theory]
        [InlineData("Administrador")]
        [InlineData("Usuario")]
        public void EsRolValido_RolesDelSistema_DevuelveTrue(string rol)
        {
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
            Assert.False(ReglasRoles.EsRolValido(rol));
        }
    }
}
