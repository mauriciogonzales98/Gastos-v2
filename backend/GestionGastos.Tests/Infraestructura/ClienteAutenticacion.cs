using System.Net.Http.Json;

namespace GestionGastos.Tests.Infraestructura;

/// <summary>Atajos para no repetir el armado de los pedidos de /auth en cada test.</summary>
public static class ClienteAutenticacion
{
    public const string ContrasenaValida = "unaClaveSegura1";

    public static Task<HttpResponseMessage> Registrar(
        this HttpClient cliente, string email, string contrasena = ContrasenaValida) =>
        cliente.PostAsJsonAsync("/auth/register", new { email, contrasena });

    public static Task<HttpResponseMessage> IniciarSesion(
        this HttpClient cliente, string email, string contrasena = ContrasenaValida) =>
        cliente.PostAsJsonAsync("/auth/login", new { email, contrasena });

    public static Task<HttpResponseMessage> CerrarSesion(this HttpClient cliente) =>
        cliente.PostAsync("/auth/logout", content: null);

    public static Task<HttpResponseMessage> ObtenerSesion(this HttpClient cliente) =>
        cliente.GetAsync("/auth/me");

    /// <summary>Email distinto en cada llamada: los tests de una misma clase comparten la base.</summary>
    public static string EmailNuevo(string prefijo = "usuario") =>
        $"{prefijo}.{Guid.NewGuid():N}@ejemplo.com";
}
