using System.Security.Claims;
using GestionGastos.Api.Autenticacion;
using GestionGastos.Api.Comun;
using GestionGastos.Api.Data;
using GestionGastos.Api.Entidades;
using GestionGastos.Api.Monedas;
using Microsoft.EntityFrameworkCore;

namespace GestionGastos.Api.Dashboard;

public static class EndpointsDashboard
{
    public static IEndpointRouteBuilder MapearEndpointsDeDashboard(this IEndpointRouteBuilder rutas)
    {
        rutas.MapGet("/dashboard", Obtener).WithTags("Dashboard");

        return rutas;
    }

    /// <summary>
    /// RF-19 a RF-22 y RF-29 a RF-30.
    ///
    /// Los totales se agregan en la base con dos GROUP BY y no trayendo los movimientos
    /// para sumarlos en memoria: es la mitigacion que el propio PRD anota para el RNF-01
    /// con 10000 movimientos. Cada consulta devuelve a lo sumo una fila por
    /// (moneda x categoria), no una por movimiento.
    /// </summary>
    private static async Task<IResult> Obtener(
        ClaimsPrincipal principal,
        GestionGastosDbContext db,
        CancellationToken ct,
        DateOnly? desde = null,
        DateOnly? hasta = null,
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

        // AC-06: el filtro por usuario es lo primero, para que ningun total pueda incluir
        // movimientos de otra cuenta.
        var movimientos = db.Movimientos
            .Where(m => m.UsuarioId == usuarioId)
            .Where(m => m.Fecha >= inicio && m.Fecha <= fin);

        if (monedaPedida is { } monedaFiltrada)
        {
            movimientos = movimientos.Where(m => m.MonedaCodigo == monedaFiltrada);
        }

        // Ingresos y gastos, por moneda. Agrupar tambien por tipo evita una segunda pasada.
        var totales = await movimientos
            .GroupBy(m => new { m.MonedaCodigo, m.Categoria!.Tipo })
            .Select(g => new TotalPorTipo(g.Key.MonedaCodigo, g.Key.Tipo, g.Sum(m => m.Monto)))
            .ToListAsync(ct);

        // Gastos por categoria, por moneda (RF-19). Los ingresos no entran: el grafico es
        // de en que se va la plata.
        var porCategoria = await movimientos
            .Where(m => m.Categoria!.Tipo == TipoCategoria.Gasto)
            .GroupBy(m => new { m.MonedaCodigo, m.CategoriaId, m.Categoria!.Nombre })
            .Select(g => new TotalDeCategoria(
                g.Key.MonedaCodigo, g.Key.CategoriaId, g.Key.Nombre, g.Sum(m => m.Monto)))
            .ToListAsync(ct);

        // Se arma sobre el catalogo y no sobre lo que devolvieron las consultas: una moneda
        // sin movimientos en el periodo tiene que aparecer en cero, no faltar (AC-31).
        var codigos = await db.Monedas
            .Where(m => monedaPedida == null || m.Codigo == monedaPedida)
            .OrderBy(m => m.Orden)
            .Select(m => m.Codigo)
            .ToListAsync(ct);

        var resumenes = codigos.Select(codigo =>
        {
            var ingresos = TotalDe(totales, codigo, TipoCategoria.Ingreso);
            var gastos = TotalDe(totales, codigo, TipoCategoria.Gasto);

            var categorias = porCategoria
                .Where(c => c.MonedaCodigo == codigo)
                .OrderByDescending(c => c.Total)
                .Select(c => new TotalPorCategoria(c.CategoriaId, c.CategoriaNombre, c.Total))
                .ToList();

            // RF-20: el balance se calcula dentro de la moneda, nunca entre monedas.
            return new ResumenDeMoneda(codigo, ingresos, gastos, ingresos - gastos, categorias);
        }).ToList();

        return Results.Ok(new DashboardResponse(inicio, fin, resumenes));
    }

    private static decimal TotalDe(
        IEnumerable<TotalPorTipo> totales, string codigo, TipoCategoria tipo) =>
        totales
            .Where(t => t.MonedaCodigo == codigo && t.Tipo == tipo)
            .Select(t => t.Total)
            // Sin movimientos de ese tipo el total es cero, no "no hay dato" (AC-31).
            .FirstOrDefault();

    /// <summary>Fila que devuelve el GROUP BY de ingresos y gastos.</summary>
    private record TotalPorTipo(string MonedaCodigo, TipoCategoria Tipo, decimal Total);

    /// <summary>Fila que devuelve el GROUP BY de gastos por categoria.</summary>
    private record TotalDeCategoria(
        string MonedaCodigo, Guid CategoriaId, string CategoriaNombre, decimal Total);
}
