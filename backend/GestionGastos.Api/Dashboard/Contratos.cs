namespace GestionGastos.Api.Dashboard;

/// <summary>Total gastado en una categoria, dentro de una moneda (RF-19).</summary>
public record TotalPorCategoria(Guid CategoriaId, string CategoriaNombre, decimal Total);

/// <summary>
/// Los numeros de una moneda. Cada moneda es un bloque cerrado: nada de lo que hay aca
/// se suma con lo de otra (RF-29).
/// </summary>
public record ResumenDeMoneda(
    string Moneda,
    decimal TotalIngresos,
    decimal TotalGastos,
    decimal Balance,
    IReadOnlyList<TotalPorCategoria> GastosPorCategoria);

/// <summary>
/// El dashboard del periodo. Devuelve un bloque por cada moneda del catalogo, incluso sin
/// movimientos: asi el frontend muestra ceros y no una pantalla vacia (AC-31).
/// </summary>
public record DashboardResponse(
    DateOnly Desde,
    DateOnly Hasta,
    IReadOnlyList<ResumenDeMoneda> Monedas);
