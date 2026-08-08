using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace GestionGastos.Api.Autenticacion;

/// <summary>
/// Sesion por cookie httpOnly con expiracion deslizante de 24 h.
/// Se eligio sobre un JWT en localStorage por dos motivos: la cookie httpOnly no es
/// legible por JavaScript (no se filtra ante un XSS) y la expiracion deslizante da
/// "24 h de inactividad" (RNF-04, AC-36) sin plomeria de refresh tokens.
/// </summary>
public static class ConfiguracionAutenticacion
{
    public const string NombreCookie = "gestiongastos.sesion";

    public static readonly TimeSpan DuracionSesion = TimeSpan.FromHours(24);

    public static IServiceCollection AgregarAutenticacionPorCookie(this IServiceCollection servicios)
    {
        servicios.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(opciones =>
            {
                opciones.Cookie.Name = NombreCookie;
                opciones.Cookie.HttpOnly = true;
                opciones.Cookie.SameSite = SameSiteMode.Lax;
                // SameAsRequest para que funcione en http://localhost durante el desarrollo;
                // en produccion, detras de HTTPS, la cookie sale con Secure automaticamente.
                opciones.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                opciones.Cookie.IsEssential = true;

                opciones.ExpireTimeSpan = DuracionSesion;
                opciones.SlidingExpiration = true;

                // Esto es una API, no un sitio con paginas: ante falta de sesion se responde
                // 401 y es el frontend el que redirige a login (AC-05). Sin esto, ASP.NET
                // contestaria un 302 a una pagina de login que no existe.
                opciones.Events.OnRedirectToLogin = contexto =>
                {
                    contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                opciones.Events.OnRedirectToAccessDenied = contexto =>
                {
                    contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        return servicios;
    }

    /// <summary>
    /// Exige sesion en todo endpoint que no diga explicitamente lo contrario (RF-03).
    /// Es al reves de lo habitual a proposito: si manana alguien agrega un endpoint de
    /// negocio y se olvida de protegerlo, queda protegido igual.
    /// </summary>
    public static IServiceCollection AgregarAutorizacionPorDefecto(this IServiceCollection servicios)
    {
        servicios.AddAuthorization(opciones =>
        {
            opciones.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return servicios;
    }
}
