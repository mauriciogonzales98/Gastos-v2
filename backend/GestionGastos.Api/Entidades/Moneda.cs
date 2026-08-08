namespace GestionGastos.Api.Entidades;

/// <summary>
/// Una moneda del catalogo (RF-24). Es una tabla y no un enum porque van a sumarse
/// monedas mas adelante: agregar una tiene que ser insertar una fila, no tocar codigo.
/// No hay conversion entre ellas: los montos de cada una se suman por separado (RF-29).
/// </summary>
public class Moneda
{
    /// <summary>
    /// Codigo ISO 4217 ("ARS", "USD"). Es la clave primaria: "Pesos" seria ambiguo, hay
    /// pesos argentinos, mexicanos, chilenos y colombianos.
    /// </summary>
    public required string Codigo { get; set; }

    /// <summary>Como se muestra en los selectores: "Pesos", "Dolares".</summary>
    public required string Nombre { get; set; }

    /// <summary>Prefijo del monto: "$", "US$".</summary>
    public required string Simbolo { get; set; }

    /// <summary>
    /// Decimales admitidos (RF-13). Vive por moneda porque no todas tienen centavos:
    /// el yen y el peso chileno usan cero.
    /// </summary>
    public int Decimales { get; set; }

    /// <summary>
    /// La que propone el formulario cuando el usuario no elige (RF-25, AC-38). Hay
    /// exactamente una en true; cambiarla es un UPDATE, no un deploy.
    /// </summary>
    public bool EsPredeterminada { get; set; }

    /// <summary>Orden en el que se ofrecen las monedas en los selectores.</summary>
    public int Orden { get; set; }
}
