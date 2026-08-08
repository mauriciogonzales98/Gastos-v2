using System.Net;
using System.Net.Http.Json;
using GestionGastos.Api.Autenticacion;
using GestionGastos.Tests.Infraestructura;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GestionGastos.Tests.Autenticacion;

/// <summary>RF-02 a RF-05: inicio de sesion, acceso protegido y cierre de sesion.</summary>
public class SesionTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    [Fact(DisplayName = "AC-03: con credenciales correctas se inicia sesion y la sesion queda activa")]
    public async Task AC03_LoginConCredencialesCorrectas()
    {
        var email = ClienteAutenticacion.EmailNuevo();
        var cliente = fabrica.CreateClient();
        await cliente.Registrar(email);
        await cliente.CerrarSesion();

        var login = await cliente.IniciarSesion(email);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var sesion = await cliente.ObtenerSesion();
        Assert.Equal(HttpStatusCode.OK, sesion.StatusCode);
        var usuario = await sesion.Content.ReadFromJsonAsync<UsuarioResponse>();
        Assert.Equal(email, usuario!.Email);
    }

    [Theory(DisplayName = "AC-04: con credenciales incorrectas se rechaza el acceso y no queda sesion")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AC04_LoginConCredencialesIncorrectas(bool emailExiste)
    {
        var email = ClienteAutenticacion.EmailNuevo();
        if (emailExiste)
        {
            await fabrica.CreateClient().Registrar(email);
        }

        var cliente = fabrica.CreateClient();
        var login = await cliente.IniciarSesion(email, "contrasenaEquivocada7");

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.ObtenerSesion()).StatusCode);
    }

    [Fact(DisplayName = "AC-05: sin sesion, un endpoint de la aplicacion responde 401 y no ejecuta la accion")]
    public async Task AC05_SinSesionNoHayAcceso()
    {
        var respuesta = await fabrica.CreateClient().ObtenerSesion();

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        // 401 y no un 302 a una pagina de login inexistente: el frontend necesita el codigo
        // para redirigir el mismo.
        Assert.Null(respuesta.Headers.Location);
    }

    [Fact(DisplayName = "AC-09: al cerrar sesion se vuelve a exigir autenticacion")]
    public async Task AC09_LogoutCierraLaSesion()
    {
        var cliente = fabrica.CreateClient();
        await cliente.Registrar(ClienteAutenticacion.EmailNuevo());
        Assert.Equal(HttpStatusCode.OK, (await cliente.ObtenerSesion()).StatusCode);

        var logout = await cliente.CerrarSesion();

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.ObtenerSesion()).StatusCode);
    }

    [Fact(DisplayName = "RNF-04 / AC-36: la sesion vive 24 h y se renueva de forma deslizante")]
    public void RNF04_LaCookieExpiraPorInactividad()
    {
        var opciones = fabrica.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        // Con expiracion deslizante, cada pedido corre la ventana: la sesion solo muere
        // tras 24 h sin actividad.
        Assert.True(opciones.SlidingExpiration);
        Assert.Equal(TimeSpan.FromHours(24), opciones.ExpireTimeSpan);
    }

    [Fact(DisplayName = "La cookie de sesion es httpOnly, para que un XSS no pueda leerla")]
    public async Task LaCookieEsHttpOnly()
    {
        var respuesta = await fabrica.CreateClient().Registrar(ClienteAutenticacion.EmailNuevo());

        var cookie = Assert.Single(
            respuesta.Headers.GetValues("Set-Cookie"),
            c => c.StartsWith(ConfiguracionAutenticacion.NombreCookie, StringComparison.Ordinal));

        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
    }
}
