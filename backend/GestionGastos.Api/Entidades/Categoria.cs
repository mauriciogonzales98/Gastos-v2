namespace GestionGastos.Api.Entidades;

public enum TipoCategoria
{
    Gasto,
    Ingreso,
}

/// <summary>
/// Categoria de un movimiento. Hay dos clases, distinguidas por <see cref="EsDelSistema"/>:
/// las predefinidas, compartidas por todos y no modificables (RF-06), y las propias de un
/// usuario (RF-07). Las del sistema tienen <see cref="UsuarioId"/> en null.
/// </summary>
public class Categoria
{
    public Guid Id { get; set; }

    public required string Nombre { get; set; }

    public TipoCategoria Tipo { get; set; }

    /// <summary>null en las predefinidas; el dueno en las propias.</summary>
    public Guid? UsuarioId { get; set; }

    public Usuario? Usuario { get; set; }

    public bool EsDelSistema { get; set; }

    /// <summary>
    /// Baja logica (RF-09): la fila se conserva para que los movimientos ya registrados
    /// sigan mostrando el nombre y sumando en el dashboard (AC-14). null = activa.
    /// </summary>
    public DateTime? FechaBajaUtc { get; set; }

    public bool EstaActiva => FechaBajaUtc is null;

    /// <summary>Una categoria es usable por el usuario si es del sistema o es suya.</summary>
    public bool EsAccesiblePor(Guid usuarioId) => EsDelSistema || UsuarioId == usuarioId;
}
