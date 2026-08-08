using System.Net;
using System.Net.Http.Json;
using GestionGastos.Api.Autenticacion;
using GestionGastos.Tests.Infraestructura;

namespace GestionGastos.Tests.Autenticacion;

/// <summary>
/// RF-03 y RF-04: la identidad sale siempre de la cookie de sesion.
/// AC-06, AC-07 y AC-08 hablan de movimientos, que se implementan en la feature 2; lo que
/// se verifica aca es la base sobre la que se apoyan: dos sesiones no se pisan y un id
/// mandado por el cliente no cambia quien es el usuario.
/// </summary>
public class AislamientoDeUsuariosTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    [Fact(DisplayName = "Cada sesion responde con su propio usuario, sin mezclarse con la otra")]
    public async Task CadaSesionVeSuPropioUsuario()
    {
        var emailA = ClienteAutenticacion.EmailNuevo("a");
        var emailB = ClienteAutenticacion.EmailNuevo("b");

        var clienteA = fabrica.CreateClient();
        var clienteB = fabrica.CreateClient();
        await clienteA.Registrar(emailA);
        await clienteB.Registrar(emailB);

        var sesionA = await (await clienteA.ObtenerSesion()).Content.ReadFromJsonAsync<UsuarioResponse>();
        var sesionB = await (await clienteB.ObtenerSesion()).Content.ReadFromJsonAsync<UsuarioResponse>();

        Assert.Equal(emailA, sesionA!.Email);
        Assert.Equal(emailB, sesionB!.Email);
        Assert.NotEqual(sesionA.Id, sesionB.Id);
    }

    [Fact(DisplayName = "AC-08: un id de usuario mandado por el cliente no cambia la identidad de la sesion")]
    public async Task ElIdDelClienteNoCambiaLaIdentidad()
    {
        var emailA = ClienteAutenticacion.EmailNuevo("a");
        var clienteA = fabrica.CreateClient();
        await clienteA.Registrar(emailA);

        var clienteB = fabrica.CreateClient();
        await clienteB.Registrar(ClienteAutenticacion.EmailNuevo("b"));
        var usuarioB = await (await clienteB.ObtenerSesion()).Content.ReadFromJsonAsync<UsuarioResponse>();

        var respuesta = await clienteA.GetAsync($"/auth/me?usuarioId={usuarioB!.Id}");

        var usuarioDevuelto = await respuesta.Content.ReadFromJsonAsync<UsuarioResponse>();
        Assert.Equal(emailA, usuarioDevuelto!.Email);
        Assert.NotEqual(usuarioB.Id, usuarioDevuelto.Id);
    }

    [Theory(DisplayName = "AC-05: los healthchecks siguen siendo publicos pese a la autorizacion por defecto")]
    [InlineData("/health")]
    public async Task LosHealthchecksSonPublicos(string ruta)
    {
        var respuesta = await fabrica.CreateClient().GetAsync(ruta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }
}
