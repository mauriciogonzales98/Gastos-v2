using System.Net;
using System.Net.Http.Json;
using GestionGastos.Api.Autenticacion;
using GestionGastos.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace GestionGastos.Tests.Autenticacion;

/// <summary>RF-01: alta de cuenta.</summary>
public class RegistroTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    [Fact(DisplayName = "AC-01: con un email no registrado la cuenta queda creada y puede iniciar sesion")]
    public async Task AC01_AltaConEmailNuevo()
    {
        var email = ClienteAutenticacion.EmailNuevo();

        var alta = await fabrica.CreateClient().Registrar(email);

        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);

        // Cliente nuevo: sin la cookie del alta, para probar que las credenciales sirven solas.
        var login = await fabrica.CreateClient().IniciarSesion(email);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var usuario = await login.Content.ReadFromJsonAsync<UsuarioResponse>();
        Assert.Equal(email, usuario!.Email);
    }

    [Fact(DisplayName = "AC-02: un email ya registrado se rechaza y sigue habiendo una sola cuenta")]
    public async Task AC02_AltaConEmailDuplicado()
    {
        var email = ClienteAutenticacion.EmailNuevo();
        await fabrica.CreateClient().Registrar(email);

        var segundaAlta = await fabrica.CreateClient().Registrar(email, "otraClaveDistinta9");

        Assert.Equal(HttpStatusCode.Conflict, segundaAlta.StatusCode);

        var cuentas = await fabrica.ConsultarBase(db => db.Usuarios.CountAsync(u => u.Email == email));
        Assert.Equal(1, cuentas);
    }

    [Fact(DisplayName = "AC-02: el email duplicado se detecta aunque cambie el uso de mayusculas")]
    public async Task AC02_AltaConEmailDuplicadoEnOtroCasing()
    {
        var email = ClienteAutenticacion.EmailNuevo();
        await fabrica.CreateClient().Registrar(email);

        var segundaAlta = await fabrica.CreateClient().Registrar(email.ToUpperInvariant());

        Assert.Equal(HttpStatusCode.Conflict, segundaAlta.StatusCode);

        var cuentas = await fabrica.ConsultarBase(db => db.Usuarios.CountAsync(u => u.Email == email));
        Assert.Equal(1, cuentas);
    }

    [Theory(DisplayName = "El alta rechaza email o contrasena invalidos y no crea la cuenta")]
    [InlineData("", ClienteAutenticacion.ContrasenaValida)]
    [InlineData("no-es-un-email", ClienteAutenticacion.ContrasenaValida)]
    [InlineData("sin@dominio", ClienteAutenticacion.ContrasenaValida)]
    [InlineData("valido@ejemplo.com", "corta")]
    [InlineData("valido@ejemplo.com", "")]
    public async Task AltaConDatosInvalidos(string email, string contrasena)
    {
        var respuesta = await fabrica.CreateClient().Registrar(email, contrasena);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var cuentas = await fabrica.ConsultarBase(db => db.Usuarios.CountAsync(u => u.Email == email));
        Assert.Equal(0, cuentas);
    }

    [Fact(DisplayName = "AC-35: la contrasena se guarda hasheada con bcrypt, nunca en texto plano")]
    public async Task AC35_ContrasenaHasheada()
    {
        var email = ClienteAutenticacion.EmailNuevo();
        await fabrica.CreateClient().Registrar(email);

        var hash = await fabrica.ConsultarBase(db =>
            db.Usuarios.Where(u => u.Email == email).Select(u => u.HashContrasena).SingleAsync());

        Assert.NotEqual(ClienteAutenticacion.ContrasenaValida, hash);
        Assert.DoesNotContain(ClienteAutenticacion.ContrasenaValida, hash, StringComparison.Ordinal);
        // Prefijo del formato bcrypt: $2a$, $2b$ o $2y$ seguidos del factor de trabajo.
        Assert.Matches(@"^\$2[aby]\$\d{2}\$", hash);
    }
}
