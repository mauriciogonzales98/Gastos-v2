using GestionGastos.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GestionGastos.Api.Monedas;

/// <summary>El catalogo tal como lo consume el frontend para armar selectores y formatos.</summary>
public record MonedaResponse(
    string Codigo,
    string Nombre,
    string Simbolo,
    int Decimales,
    bool EsPredeterminada);

public static class EndpointsMonedas
{
    public static IEndpointRouteBuilder MapearEndpointsDeMonedas(this IEndpointRouteBuilder rutas)
    {
        // RF-24: el frontend arma el selector, el filtro y el formato de los montos desde
        // esta respuesta. Sumar una moneda es insertar una fila, no tocar el cliente.
        rutas.MapGet("/monedas", async (GestionGastosDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Monedas
                .OrderBy(m => m.Orden)
                .Select(m => new MonedaResponse(
                    m.Codigo, m.Nombre, m.Simbolo, m.Decimales, m.EsPredeterminada))
                .ToListAsync(ct)))
            .WithTags("Monedas");

        return rutas;
    }
}
