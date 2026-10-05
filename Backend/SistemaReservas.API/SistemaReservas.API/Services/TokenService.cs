using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SistemaReservas.API.Services
{
    // NFR1 - #43 Genera el token JWT que identifica al usuario
    // y lleva su rol, para que el backend pueda autorizar por rol.
    public class TokenService
    {
        private readonly IConfiguration _configuration;
        private readonly TimeProvider _timeProvider;

        // NFR2 - Tiempo inyectable: la expiración se calcula con la hora de TimeProvider.
        public TokenService(IConfiguration configuration, TimeProvider timeProvider)
        {
            _configuration = configuration;
            _timeProvider = timeProvider;
        }

        public (string Token, DateTime ExpiraUtc) GenerarToken(int usuarioId, string nombre, string rol)
        {
            var jwt = _configuration.GetSection("Jwt");

            var clave = jwt["Key"]
                ?? throw new InvalidOperationException("Falta la configuración Jwt:Key.");

            var horas = int.TryParse(jwt["ExpiraHoras"], out var valor) ? valor : 8;
            var expiraUtc = _timeProvider.GetUtcNow().UtcDateTime.AddHours(horas);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
                new Claim(ClaimTypes.Name, nombre),
                new Claim(ClaimTypes.Role, rol),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var credenciales = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: expiraUtc,
                signingCredentials: credenciales);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiraUtc);
        }
    }
}
