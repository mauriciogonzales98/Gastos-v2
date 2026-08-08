using GestionGastos.Api.Entidades;

namespace GestionGastos.Api.Data;

/// <summary>
/// Monedas con las que arranca la aplicacion. Sumar una mas adelante es agregarla aca
/// con su migracion de seed, o directamente insertar la fila: nada del resto del codigo
/// conoce estos codigos.
/// </summary>
public static class CatalogoDeMonedas
{
    public const string CodigoPredeterminado = "ARS";

    public static readonly Moneda[] Iniciales =
    [
        new()
        {
            Codigo = CodigoPredeterminado,
            Nombre = "Pesos",
            Simbolo = "$",
            Decimales = 2,
            EsPredeterminada = true,
            Orden = 1,
        },
        new()
        {
            Codigo = "USD",
            Nombre = "Dolares",
            Simbolo = "US$",
            Decimales = 2,
            EsPredeterminada = false,
            Orden = 2,
        },
    ];
}
