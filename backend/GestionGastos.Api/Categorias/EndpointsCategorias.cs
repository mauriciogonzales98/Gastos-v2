using System.Security.Claims;
using GestionGastos.Api.Autenticacion;
using GestionGastos.Api.Data;
using GestionGastos.Api.Entidades;
using Microsoft.EntityFrameworkCore;

namespace GestionGastos.Api.Categorias;

public static class EndpointsCategorias
{
    private const int LargoMaximoNombre = 60;

    public static IEndpointRouteBuilder MapearEndpointsDeCategorias(this IEndpointRouteBuilder rutas)
    {
        var grupo = rutas.MapGroup("/categorias").WithTags("Categorias");

        grupo.MapGet("/", Listar);
        grupo.MapPost("/", Crear);
        grupo.MapPut("/{id:guid}", Renombrar);
        grupo.MapDelete("/{id:guid}", DarDeBaja);

        return rutas;
    }

    /// <summary>
    /// Las categorias que el usuario puede usar: las del sistema mas las suyas, siempre
    /// activas. Es la consulta base tambien para validar la categoria de un movimiento.
    /// </summary>
    public static IQueryable<Categoria> ActivasAccesiblesPor(GestionGastosDbContext db, Guid usuarioId) =>
        db.Categorias.Where(c =>
            c.FechaBajaUtc == null && (c.EsDelSistema || c.UsuarioId == usuarioId));

    /// <summary>RF-06, RF-07. Con `tipo` devuelve solo las de ese tipo (AC-10).</summary>
    private static async Task<IResult> Listar(
        ClaimsPrincipal principal,
        GestionGastosDbContext db,
        CancellationToken ct,
        string? tipo = null)
    {
        TipoCategoria? filtro = null;

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            if (!Enum.TryParse<TipoCategoria>(tipo, ignoreCase: true, out var tipoParseado))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["tipo"] = [$"El tipo tiene que ser '{nameof(TipoCategoria.Gasto)}' o '{nameof(TipoCategoria.Ingreso)}'."],
                });
            }

            filtro = tipoParseado;
        }

        var consulta = ActivasAccesiblesPor(db, principal.ObtenerIdRequerido());

        if (filtro is { } tipoPedido)
        {
            consulta = consulta.Where(c => c.Tipo == tipoPedido);
        }

        var categorias = await consulta
            // Primero las del sistema, que son el camino rapido de carga.
            .OrderByDescending(c => c.EsDelSistema)
            .ThenBy(c => c.Nombre)
            .Select(c => new CategoriaResponse(c.Id, c.Nombre, c.Tipo, c.EsDelSistema))
            .ToListAsync(ct);

        return Results.Ok(categorias);
    }

    /// <summary>RF-07: categoria propia.</summary>
    private static async Task<IResult> Crear(
        CrearCategoriaRequest pedido,
        ClaimsPrincipal principal,
        GestionGastosDbContext db,
        CancellationToken ct)
    {
        if (ValidarNombre(pedido.Nombre) is { } errorNombre)
        {
            return errorNombre;
        }

        if (pedido.Tipo is not { } tipo)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["tipo"] = ["El tipo es obligatorio."],
            });
        }

        var categoria = new Categoria
        {
            Id = Guid.NewGuid(),
            Nombre = pedido.Nombre!.Trim(),
            Tipo = tipo,
            UsuarioId = principal.ObtenerIdRequerido(),
            EsDelSistema = false,
        };

        db.Categorias.Add(categoria);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/categorias/{categoria.Id}", CategoriaResponse.De(categoria));
    }

    /// <summary>RF-08: renombrar una categoria propia.</summary>
    private static async Task<IResult> Renombrar(
        Guid id,
        RenombrarCategoriaRequest pedido,
        ClaimsPrincipal principal,
        GestionGastosDbContext db,
        CancellationToken ct)
    {
        if (ValidarNombre(pedido.Nombre) is { } errorNombre)
        {
            return errorNombre;
        }

        var (categoria, rechazo) = await BuscarPropiaModificable(id, principal, db, ct);
        if (rechazo is not null)
        {
            return rechazo;
        }

        categoria!.Nombre = pedido.Nombre!.Trim();
        await db.SaveChangesAsync(ct);

        return Results.Ok(CategoriaResponse.De(categoria));
    }

    /// <summary>
    /// RF-09: baja logica. La fila queda, asi los movimientos ya registrados siguen
    /// mostrando el nombre y sumando en el dashboard (AC-14).
    /// </summary>
    private static async Task<IResult> DarDeBaja(
        Guid id,
        ClaimsPrincipal principal,
        GestionGastosDbContext db,
        CancellationToken ct)
    {
        var (categoria, rechazo) = await BuscarPropiaModificable(id, principal, db, ct);
        if (rechazo is not null)
        {
            return rechazo;
        }

        if (categoria!.EstaActiva)
        {
            categoria.FechaBajaUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return Results.NoContent();
    }

    /// <summary>
    /// Resuelve la categoria de una operacion de escritura y decide si se puede tocar.
    /// Las de otro usuario dan 404 y no 403: un 403 confirmaria que ese id existe.
    /// </summary>
    private static async Task<(Categoria? Categoria, IResult? Rechazo)> BuscarPropiaModificable(
        Guid id,
        ClaimsPrincipal principal,
        GestionGastosDbContext db,
        CancellationToken ct)
    {
        var usuarioId = principal.ObtenerIdRequerido();
        var categoria = await db.Categorias.SingleOrDefaultAsync(c => c.Id == id, ct);

        if (categoria is null || !categoria.EsAccesiblePor(usuarioId))
        {
            return (null, Results.NotFound());
        }

        // AC-11: las predefinidas no se modifican ni se eliminan.
        if (categoria.EsDelSistema)
        {
            return (null, Results.Problem(
                title: "Categoria del sistema",
                detail: "Las categorias predefinidas no se pueden modificar ni eliminar.",
                statusCode: StatusCodes.Status403Forbidden));
        }

        return (categoria, null);
    }

    private static IResult? ValidarNombre(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["nombre"] = ["El nombre es obligatorio."],
            });
        }

        if (nombre.Trim().Length > LargoMaximoNombre)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["nombre"] = [$"El nombre no puede superar los {LargoMaximoNombre} caracteres."],
            });
        }

        return null;
    }
}
