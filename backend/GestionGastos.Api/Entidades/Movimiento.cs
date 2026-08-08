namespace GestionGastos.Api.Entidades;

/// <summary>
/// Un gasto o un ingreso del usuario (RF-10, RF-11). No guarda su propio tipo: lo hereda
/// de la categoria, asi no pueden quedar en desacuerdo.
/// </summary>
public class Movimiento
{
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

    public DateTime FechaCreacionUtc { get; set; }
}
