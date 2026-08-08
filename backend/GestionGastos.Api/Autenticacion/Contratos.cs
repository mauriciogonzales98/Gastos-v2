namespace GestionGastos.Api.Autenticacion;

/// <summary>Alta de cuenta (RF-01).</summary>
public record RegistroRequest(string? Email, string? Contrasena);

/// <summary>Inicio de sesion (RF-02).</summary>
public record LoginRequest(string? Email, string? Contrasena);

/// <summary>Datos publicos de la sesion. Nunca incluye el hash.</summary>
public record UsuarioResponse(Guid Id, string Email);
