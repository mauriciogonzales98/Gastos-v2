namespace GestionGastos.Api.Entidades;

/// <summary>
/// Un gasto o un ingreso del usuario (RF-10, RF-11). No guarda su propio tipo: lo hereda
/// de la categoria, asi no pueden quedar en desacuerdo.
/// </summary>
public class Movimiento
{
    /// <summary>
    /// RF-33. Una nota, no un campo de observaciones: entra "expensas + cochera" y no
    /// entra un parrafo. El mapeo y la validacion salen los dos de aca.
    /// </summary>
    public const int LargoMaximoDescripcion = 120;

    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public Usuario? Usuario { get; set; }

    public Guid CategoriaId { get; set; }

    public Categoria? Categoria { get; set; }

    /// <summary>Siempre mayor a cero y con hasta dos decimales (RF-13).</summary>
    public decimal Monto { get; set; }

    /// <summary>
    /// Codigo de la moneda del <see cref="Monto"/> (RF-24). Nunca se convierte: un monto
    /// solo se suma con otros de la misma moneda.
    /// </summary>
    public required string MonedaCodigo { get; set; }

    public Moneda? Moneda { get; set; }

    /// <summary>Fecha del movimiento, sin hora: es un dato del usuario, no del sistema.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>
    /// Nota descriptiva opcional (RF-33): "alquiler agosto", "expensas + cochera". Es para
    /// leer, no para analizar: no se busca, no se filtra y no se agrupa. La categoria sigue
    /// siendo el unico eje del dashboard.
    /// "Sin nota" es siempre null, nunca cadena vacia: un solo estado y no dos.
    /// </summary>
    public string? Descripcion { get; set; }

    public DateTime FechaCreacionUtc { get; set; }
}
