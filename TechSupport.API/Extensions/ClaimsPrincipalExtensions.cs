using System.Security.Claims;
using TechSupport.API.Models;

namespace TechSupport.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUsuarioId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new InvalidOperationException("El token no contiene el claim NameIdentifier."));

    public static Rol GetRol(this ClaimsPrincipal user) =>
        Enum.Parse<Rol>(user.FindFirstValue(ClaimTypes.Role)
                        ?? throw new InvalidOperationException("El token no contiene el claim Role."));
}
