using GestionGastos.Api.Entidades;

namespace GestionGastos.Api.Movimientos;

/// <summary>
/// Un movimiento tal como se lista. Trae el nombre y el tipo de la categoria resueltos,
/// para que el frontend no tenga que cruzarlos contra el catalogo (y para que un
/// movimiento de una categoria dada de baja siga mostrando su nombre, AC-14).
/// La moneda viaja como codigo: el simbolo y los decimales salen de GET /monedas.
/// </summary>
/// La descripcion viaja como null cuando no hay nota (RF-33): el frontend no tiene que
/// distinguir null de cadena vacia.
public record MovimientoResponse(
    Guid Id,
    decimal Monto,
    string Moneda,
    DateOnly Fecha,
    Guid CategoriaId,
    string CategoriaNombre,
    TipoCategoria Tipo,
    string? Descripcion);

/// <summary>
/// Omitir la moneda es valido y significa la predeterminada del catalogo (RF-25, AC-38).
/// Mandar un codigo que no este en el catalogo, no (RF-26, AC-39).
/// La descripcion es opcional (RF-33): omitirla, mandarla null o mandarla en blanco son
/// todos "sin nota" (AC-51).
/// </summary>
public record GuardarMovimientoRequest(
    decimal? Monto,
    DateOnly? Fecha,
    Guid? CategoriaId,
    string? Moneda,
    string? Descripcion);
