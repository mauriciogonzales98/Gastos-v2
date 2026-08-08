using GestionGastos.Api.Entidades;

namespace GestionGastos.Api.Movimientos;

/// <summary>
/// Un movimiento tal como se lista. Trae el nombre y el tipo de la categoria resueltos,
/// para que el frontend no tenga que cruzarlos contra el catalogo (y para que un
/// movimiento de una categoria dada de baja siga mostrando su nombre, AC-14).
/// </summary>
public record MovimientoResponse(
    Guid Id,
    decimal Monto,
    DateOnly Fecha,
    Guid CategoriaId,
    string CategoriaNombre,
    TipoCategoria Tipo);

public record GuardarMovimientoRequest(decimal? Monto, DateOnly? Fecha, Guid? CategoriaId);
