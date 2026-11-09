using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TechSupport.API.Models;

namespace TechSupport.API.Services;

public class JwtSettings
{
    public const string Seccion = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}

public interface ITokenService
{
    (string Token, DateTime Expira) GenerarToken(Usuario usuario);
}

public class TokenService(Microsoft.Extensions.Options.IOptions<JwtSettings> options) : ITokenService
{
    private readonly JwtSettings _settings = options.Value;

    public (string Token, DateTime Expira) GenerarToken(Usuario usuario)
    {
        var expira = DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes);

        var claims = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.NombreUsuario),
            new Claim(ClaimTypes.Role, usuario.Rol.ToString())
        ]);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = claims,
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Expires = expira,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expira);
    }
}
