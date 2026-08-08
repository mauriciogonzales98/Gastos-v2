using System.Net.Mail;
using System.Security.Claims;
using GestionGastos.Api.Data;
using GestionGastos.Api.Entidades;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace GestionGastos.Api.Autenticacion;

public static class EndpointsAutenticacion
{
    private const int LargoMinimoContrasena = 8;

    // bcrypt solo usa los primeros 72 bytes de la contrasena; cortar aca evita la sorpresa
    // de que dos contrasenas largas distintas validen contra el mismo hash.
    private const int LargoMaximoContrasena = 72;

    private const int LargoMaximoEmail = 254;

    public static IEndpointRouteBuilder MapearEndpointsDeAutenticacion(this IEndpointRouteBuilder rutas)
    {
        var grupo = rutas.MapGroup("/auth").WithTags("Autenticacion");

        grupo.MapPost("/register", Registrar).AllowAnonymous();
        grupo.MapPost("/login", IniciarSesion).AllowAnonymous();
        // El cast a Delegate evita que el handler sin parametros de cuerpo se interprete
        // como RequestDelegate y se descarte el IResult que devuelve.
        grupo.MapPost("/logout", (Delegate)CerrarSesion).AllowAnonymous();
        grupo.MapGet("/me", ObtenerSesionActual).RequireAuthorization();

        return rutas;
    }

    /// <summary>RF-01: alta de cuenta. Rechaza el email ya registrado (AC-02).</summary>
    private static async Task<IResult> Registrar(
        RegistroRequest pedido,
        GestionGastosDbContext db,
        IServicioContrasenas contrasenas,
        HttpContext contexto,
        CancellationToken ct)
    {
        if (Validar(pedido.Email, pedido.Contrasena) is { Count: > 0 } errores)
        {
            return Results.ValidationProblem(errores);
        }

        var email = Usuario.NormalizarEmail(pedido.Email!);

        if (await db.Usuarios.AnyAsync(u => u.Email == email, ct))
        {
            return Results.Problem(
                title: "Email ya registrado",
                detail: "Ya existe una cuenta con ese email.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Email = email,
            HashContrasena = contrasenas.Hashear(pedido.Contrasena!),
            FechaCreacionUtc = DateTime.UtcNow,
        };

        db.Usuarios.Add(usuario);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            db.Entry(usuario).State = EntityState.Detached;

            // Dos altas simultaneas con el mismo email: el indice unico deja pasar una sola.
            if (await db.Usuarios.AnyAsync(u => u.Email == email, ct))
            {
                return Results.Problem(
                    title: "Email ya registrado",
                    detail: "Ya existe una cuenta con ese email.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            throw;
        }

        await AbrirSesion(contexto, usuario);

        return Results.Created($"/usuarios/{usuario.Id}", new UsuarioResponse(usuario.Id, usuario.Email));
    }

    /// <summary>RF-02: inicio de sesion.</summary>
    private static async Task<IResult> IniciarSesion(
        LoginRequest pedido,
        GestionGastosDbContext db,
        IServicioContrasenas contrasenas,
        HttpContext contexto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(pedido.Email) || string.IsNullOrEmpty(pedido.Contrasena))
        {
            return CredencialesInvalidas();
        }

        var email = Usuario.NormalizarEmail(pedido.Email);
        var usuario = await db.Usuarios.SingleOrDefaultAsync(u => u.Email == email, ct);

        if (usuario is null || !contrasenas.Verificar(pedido.Contrasena, usuario.HashContrasena))
        {
            // Mismo mensaje para email inexistente y contrasena equivocada: distinguirlos
            // permitiria averiguar que emails tienen cuenta.
            return CredencialesInvalidas();
        }

        await AbrirSesion(contexto, usuario);

        return Results.Ok(new UsuarioResponse(usuario.Id, usuario.Email));
    }

    /// <summary>RF-05: cierre de sesion (AC-09). Es idempotente: sin sesion tambien devuelve 204.</summary>
    private static async Task<IResult> CerrarSesion(HttpContext contexto)
    {
        await contexto.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.NoContent();
    }

    /// <summary>
    /// Quien es el usuario de esta sesion. El frontend lo usa al arrancar para saber si
    /// mostrar la app o la pantalla de login.
    /// </summary>
    private static IResult ObtenerSesionActual(ClaimsPrincipal principal)
    {
        var id = principal.ObtenerIdRequerido();
        return Results.Ok(new UsuarioResponse(id, principal.ObtenerEmail() ?? string.Empty));
    }

    private static async Task AbrirSesion(HttpContext contexto, Usuario usuario)
    {
        var identidad = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);

        // IsPersistent para que la sesion sobreviva a cerrar el navegador; la expiracion
        // deslizante de 24 h de inactividad la sigue cortando igual (RNF-04).
        await contexto.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad),
            new AuthenticationProperties { IsPersistent = true });
    }

    private static IResult CredencialesInvalidas() =>
        Results.Problem(
            title: "Credenciales invalidas",
            detail: "El email o la contrasena no son correctos.",
            statusCode: StatusCodes.Status401Unauthorized);

    private static Dictionary<string, string[]> Validar(string? email, string? contrasena)
    {
        var errores = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(email))
        {
            errores["email"] = ["El email es obligatorio."];
        }
        else if (email.Length > LargoMaximoEmail || !EsEmailValido(email))
        {
            errores["email"] = ["El email no tiene un formato valido."];
        }

        if (string.IsNullOrEmpty(contrasena))
        {
            errores["contrasena"] = ["La contrasena es obligatoria."];
        }
        else if (contrasena.Length < LargoMinimoContrasena || contrasena.Length > LargoMaximoContrasena)
        {
            errores["contrasena"] =
                [$"La contrasena debe tener entre {LargoMinimoContrasena} y {LargoMaximoContrasena} caracteres."];
        }

        return errores;
    }

    private static bool EsEmailValido(string email)
    {
        try
        {
            // MailAddress acepta cosas raras pero validas; alcanza para descartar tipeos.
            var direccion = new MailAddress(email.Trim());
            return direccion.Address == email.Trim()
                && direccion.Host.Contains('.', StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
