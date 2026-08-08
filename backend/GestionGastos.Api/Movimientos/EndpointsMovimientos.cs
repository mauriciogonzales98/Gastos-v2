using System.Security.Claims;
using GestionGastos.Api.Autenticacion;
using GestionGastos.Api.Categorias;
using GestionGastos.Api.Comun;
using GestionGastos.Api.Data;
using GestionGastos.Api.Entidades;
using GestionGastos.Api.Monedas;
using Microsoft.EntityFrameworkCore;

namespace GestionGastos.Api.Movimientos;

public static class EndpointsMovimientos
{
    public static IEndpointRouteBuilder MapearEndpointsDeMovimientos(this IEndpointRouteBuilder rutas)
    {
        var grupo = rutas.MapGroup("/movimientos").WithTags("Movimientos");

        grupo.MapGet("/", Listar);
        grupo.MapPost("/", Crear);
        grupo.MapPut("/{id:guid}", Modificar);
        grupo.MapDelete("/{id:guid}", Eliminar);

        return rutas;
    }

    /// <summary>
    /// RF-16 a RF-18 y RF-28. Sin filtros de fecha devuelve el mes actual (AC-25); sin
    /// filtro de categoria, todas (AC-24); sin filtro de moneda, las dos (AC-45). El rango
    /// incluye ambos extremos (AC-26).
    /// </summary>
    private static async Task<IResult> Listar(
        ClaimsPrincipal principal,
        GestionGastosDbContext db,
        CancellationToken ct,
        DateOnly? desde = null,
        DateOnly? hasta = null,
        Guid? categoriaId = null,
        string? moneda = null)
    {
        var (inicio, fin) = Periodo.PedidoOMesActual(desde, hasta);

        if (inicio > fin)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["desde"] = ["La fecha de inicio no puede ser posterior a la de fin."],
            });
        }

        string? monedaPedida = null;

        if (!string.IsNullOrWhiteSpace(moneda))
        {
            monedaPedida = await ConsultasDeMonedas.Normalizar(moneda, db, ct);

            if (monedaPedida is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["moneda"] = [await ConsultasDeMonedas.MensajeDeCodigoInvalido(db, ct)],
                });
            }
        }

        var usuarioId = principal.ObtenerIdRequerido();

        var consulta = db.Movimientos
            .Where(m => m.UsuarioId == usuarioId)
            .Where(m => m.Fecha >= inicio && m.Fecha <= fin);

        if (categoriaId is { } categoriaPedida)
        {
            consulta = consulta.Where(m => m.CategoriaId == categoriaPedida);
        }

        if (monedaPedida is { } monedaFiltrada)
        {
            consulta = consulta.Where(m => m.MonedaCodigo == monedaFiltrada);
        }

        var movimientos = await consulta
            .OrderByDescending(m => m.Fecha)
            .ThenByDescending(m => m.FechaCreacionUtc)
            .Select(m => new MovimientoResponse(
                m.Id, m.Monto, m.MonedaCodigo, m.Fecha,
                m.CategoriaId, m.Categoria!.Nombre, m.Categoria.Tipo))
            .ToListAsync(ct);

        return Results.Ok(movimientos);
    }

    /// <summary>RF-10 y RF-11: alta de un gasto o un ingreso.</summary>
    private static async Task<IResult> Crear(
        GuardarMovimientoRequest pedido,
        ClaimsPrincipal principal,
        GestionGastosDbContext db,
        CancellationToken ct)
    {
        var usuarioId = principal.ObtenerIdRequerido();

        var (datos, rechazo) = await ValidarPedido(pedido, usuarioId, db, ct);
        if (rechazo is not null)
        {
            return rechazo;
        }

        var movimiento = new Movimiento
        {
            Id = Guid.NewGuid(),
            // El dueno sale de la sesion; si el cliente manda otro id, se ignora (AC-08).
            UsuarioId = usuarioId,
            CategoriaId = datos.Categoria.Id,
            Monto = datos.Monto,
            MonedaCodigo = datos.Moneda.Codigo,
            Fecha = datos.Fecha,
            FechaCreacionUtc = DateTime.UtcNow,
        };

        db.Movimientos.Add(movimiento);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/movimientos/{movimiento.Id}", Responder(movimiento, datos.Categoria));
    }

    /// <summary>RF-14: modificar monto, moneda, categoria y fecha de un movimiento propio.</summary>
    private static async Task<IResult> Modificar(
        Guid id,
        GuardarMovimientoRequest pedido,
        ClaimsPrincipal principal,
        GestionGastosDbContext db,
        CancellationToken ct)
    {
        var usuarioId = principal.ObtenerIdRequerido();

        var movimiento = await BuscarPropio(id, usuarioId, db, ct);
        if (movimiento is null)
        {
            return Results.NotFound();
        }

        var (datos, rechazo) = await ValidarPedido(pedido, usuarioId, db, ct);
        if (rechazo is not null)
        {
            return rechazo;
        }

        movimiento.Monto = datos.Monto;
        // AC-47: cambiar la moneda saca el monto de los totales de la anterior.
        movimiento.MonedaCodigo = datos.Moneda.Codigo;
        movimiento.Fecha = datos.Fecha;
        movimiento.CategoriaId = datos.Categoria.Id;
        await db.SaveChangesAsync(ct);

        return Results.Ok(Responder(movimiento, datos.Categoria));
    }

    /// <summary>RF-15: eliminar un movimiento propio.</summary>
    private static async Task<IResult> Eliminar(
        Guid id,
        ClaimsPrincipal principal,
        GestionGastosDbContext db,
        CancellationToken ct)
    {
        var movimiento = await BuscarPropio(id, principal.ObtenerIdRequerido(), db, ct);
        if (movimiento is null)
        {
            return Results.NotFound();
        }

        db.Movimientos.Remove(movimiento);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    /// <summary>
    /// AC-07: el movimiento de otro usuario se responde como inexistente. Un 403 seria
    /// peor: confirmaria que ese id existe.
    /// </summary>
    private static Task<Movimiento?> BuscarPropio(
        Guid id, Guid usuarioId, GestionGastosDbContext db, CancellationToken ct) =>
        db.Movimientos.SingleOrDefaultAsync(m => m.Id == id && m.UsuarioId == usuarioId, ct);

    /// <summary>
    /// RF-13, RF-23 y RF-26: monto mayor a cero con hasta dos decimales, categoria valida
    /// y moneda valida.
    /// </summary>
    private static async Task<((decimal Monto, Moneda Moneda, DateOnly Fecha, Categoria Categoria) Datos, IResult? Rechazo)>
        ValidarPedido(
            GuardarMovimientoRequest pedido,
            Guid usuarioId,
            GestionGastosDbContext db,
            CancellationToken ct)
    {
        var errores = new Dictionary<string, string[]>();

        // La moneda se resuelve primero porque de ella dependen los decimales admitidos.
        // RF-25 / AC-38: omitir la moneda vale y significa la predeterminada del catalogo.
        // Un codigo que no este en el catalogo, no (RF-26 / AC-39).
        var moneda = string.IsNullOrWhiteSpace(pedido.Moneda)
            ? await ConsultasDeMonedas.Predeterminada(db, ct)
            : await ConsultasDeMonedas.Buscar(pedido.Moneda, db, ct);

        if (moneda is null)
        {
            errores["moneda"] = [await ConsultasDeMonedas.MensajeDeCodigoInvalido(db, ct)];
        }

        if (pedido.Monto is not { } monto)
        {
            errores["monto"] = ["El monto es obligatorio."];
        }
        else if (monto <= 0)
        {
            errores["monto"] = ["El monto tiene que ser mayor a cero."];
        }
        else if (moneda is not null && decimal.Round(monto, moneda.Decimales) != monto)
        {
            // RF-13: los decimales salen de la moneda, no de una constante. Hoy las dos
            // usan 2, pero una moneda sin centavos (yen, peso chileno) usaria 0.
            errores["monto"] = [moneda.Decimales == 0
                ? $"El monto en {moneda.Nombre} no admite decimales."
                : $"El monto en {moneda.Nombre} admite hasta {moneda.Decimales} decimales."];
        }

        Categoria? categoria = null;

        if (pedido.CategoriaId is not { } categoriaId)
        {
            errores["categoriaId"] = ["La categoria es obligatoria."];
        }
        else
        {
            // Solo activas y accesibles: una categoria dada de baja deja de ofrecerse para
            // movimientos nuevos (AC-14) y la de otro usuario no existe para este (AC-12).
            categoria = await EndpointsCategorias.ActivasAccesiblesPor(db, usuarioId)
                .SingleOrDefaultAsync(c => c.Id == categoriaId, ct);

            if (categoria is null)
            {
                errores["categoriaId"] = ["La categoria no existe o no esta disponible."];
            }
        }

        if (errores.Count > 0)
        {
            return (default, Results.ValidationProblem(errores));
        }

        // RF-12 / AC-17: sin fecha, la de hoy. El frontend ya la propone, pero el default
        // tambien vive aca para que valga sea cual sea el cliente.
        var fecha = pedido.Fecha ?? DateOnly.FromDateTime(DateTime.Now);

        return ((pedido.Monto!.Value, moneda, fecha, categoria!), null);
    }

    private static MovimientoResponse Responder(Movimiento movimiento, Categoria categoria) =>
        new(movimiento.Id, movimiento.Monto, movimiento.MonedaCodigo, movimiento.Fecha,
            categoria.Id, categoria.Nombre, categoria.Tipo);
}
