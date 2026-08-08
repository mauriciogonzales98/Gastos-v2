using GestionGastos.Api.Data;
using GestionGastos.Api.Entidades;
using Microsoft.EntityFrameworkCore;

namespace GestionGastos.Api.Monedas;

/// <summary>
/// Resolucion de codigos de moneda contra el catalogo. La comparten el listado y el
/// dashboard: los dos tienen que aceptar y rechazar exactamente los mismos codigos.
/// </summary>
public static class ConsultasDeMonedas
{
    /// <summary>Busca una moneda del catalogo por codigo, sin distinguir mayusculas.</summary>
    public static Task<Moneda?> Buscar(string codigo, GestionGastosDbContext db, CancellationToken ct)
    {
        var normalizado = codigo.Trim().ToUpperInvariant();
        return db.Monedas.SingleOrDefaultAsync(m => m.Codigo == normalizado, ct);
    }

    /// <summary>Igual que <see cref="Buscar"/> pero devuelve solo el codigo canonico.</summary>
    public static async Task<string?> Normalizar(
        string codigo, GestionGastosDbContext db, CancellationToken ct) =>
        (await Buscar(codigo, db, ct))?.Codigo;

    public static Task<Moneda> Predeterminada(GestionGastosDbContext db, CancellationToken ct) =>
        db.Monedas.SingleAsync(m => m.EsPredeterminada, ct);

    /// <summary>
    /// El mensaje enumera el catalogo en vez de nombrar monedas fijas: cuando se sume una,
    /// el error se actualiza solo.
    /// </summary>
    public static async Task<string> MensajeDeCodigoInvalido(
        GestionGastosDbContext db, CancellationToken ct)
    {
        var codigos = await db.Monedas.OrderBy(m => m.Orden).Select(m => m.Codigo).ToListAsync(ct);
        return $"La moneda tiene que ser una de: {string.Join(", ", codigos)}.";
    }
}
