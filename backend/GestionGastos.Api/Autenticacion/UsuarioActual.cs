using System.Security.Claims;

namespace GestionGastos.Api.Autenticacion;

/// <summary>
/// Unica forma valida de saber quien esta pidiendo algo. El id sale siempre de la cookie
/// de sesion, nunca de un parametro que mande el cliente (RF-04, AC-07, AC-08).
/// </summary>
public static class UsuarioActual
{
    /// <summary>Devuelve el id del usuario autenticado, o null si no hay sesion.</summary>
    public static Guid? ObtenerId(this ClaimsPrincipal principal)
    {
        var valor = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(valor, out var id) ? id : null;
    }

    /// <summary>
    /// Igual que <see cref="ObtenerId"/> pero exige que haya sesion. Se usa en los endpoints
    /// de negocio, donde llegar sin usuario significa que la autorizacion se configuro mal.
    /// </summary>
    public static Guid ObtenerIdRequerido(this ClaimsPrincipal principal) =>
        principal.ObtenerId()
        ?? throw new InvalidOperationException(
            "Se llego a un endpoint autenticado sin un id de usuario en la sesion.");

    public static string? ObtenerEmail(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Email);
}
