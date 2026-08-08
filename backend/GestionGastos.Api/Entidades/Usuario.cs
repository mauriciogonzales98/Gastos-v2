namespace GestionGastos.Api.Entidades;

/// <summary>
/// Cuenta de la aplicacion. Cada usuario ve unicamente sus propios datos (RF-04).
/// </summary>
public class Usuario
{
    public Guid Id { get; set; }

    /// <summary>Se guarda normalizado (minusculas, sin espacios) y es unico (AC-02).</summary>
    public required string Email { get; set; }

    /// <summary>Hash bcrypt. Nunca la contrasena en texto plano (RNF-03, AC-35).</summary>
    public required string HashContrasena { get; set; }

    public DateTime FechaCreacionUtc { get; set; }

    /// <summary>Normaliza un email para comparar y persistir sin duplicados por mayusculas.</summary>
    public static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();
}
