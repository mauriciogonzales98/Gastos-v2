using System.Net;
using System.Text;
using GestionGastos.Api.Entidades;
using GestionGastos.Tests.Infraestructura;

namespace GestionGastos.Tests.Movimientos;

/// <summary>
/// RF-24 a RF-29: dos monedas sin conversion.
/// AC-41, AC-42, AC-43 y AC-46 verifican que los totales del dashboard no se mezclen;
/// se cubren en la feature 3, junto con el dashboard.
/// </summary>
public class MonedasTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    [Fact(DisplayName = "AC-37: un gasto cargado en dolares queda y se lista en dolares")]
    public async Task AC37_AltaEnDolares()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);

        var creado = await cliente.MovimientoNuevo(150m, comida.Id, Fechas.Hoy, "USD");

        Assert.Equal("USD", creado.Moneda);
        Assert.Equal("USD", Assert.Single(await cliente.Movimientos()).Moneda);
    }

    [Fact(DisplayName = "AC-38: sin tocar el campo moneda, el movimiento queda en pesos")]
    public async Task AC38_ElDefaultEsPesos()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);

        var creado = await cliente.MovimientoNuevo(150m, comida.Id, Fechas.Hoy, moneda: null);

        Assert.Equal("ARS", creado.Moneda);
    }

    [Theory(DisplayName = "AC-39: una moneda que no sea pesos ni dolares se rechaza y no crea nada")]
    [InlineData("Euros")]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("Pesos Argentinos")]
    public async Task AC39_LaMonedaInvalidaSeRechaza(string moneda)
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);

        var cuerpo = $$"""
            {"monto":100,"categoriaId":"{{comida.Id}}","fecha":"{{Fechas.Hoy:yyyy-MM-dd}}","moneda":"{{moneda}}"}
            """;

        var respuesta = await cliente.PostAsync(
            "/movimientos", new StringContent(cuerpo, Encoding.UTF8, "application/json"));

        // "" se trata como "no vino": vale, y significa pesos.
        if (moneda.Length == 0)
        {
            Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
            Assert.Equal("ARS", Assert.Single(await cliente.Movimientos()).Moneda);
            return;
        }

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Empty(await cliente.Movimientos());
    }

    [Fact(DisplayName = "AC-44: los movimientos conservan su moneda, aunque tengan el mismo monto")]
    public async Task AC44_CadaMovimientoConservaSuMoneda()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        await cliente.MovimientoNuevo(1000m, comida.Id, Fechas.Hoy, "ARS");
        await cliente.MovimientoNuevo(1000m, comida.Id, Fechas.Hoy, "USD");

        var movimientos = await cliente.Movimientos();

        Assert.Equal(2, movimientos.Count);
        Assert.Single(movimientos, m => m.Moneda == "ARS");
        Assert.Single(movimientos, m => m.Moneda == "USD");
    }

    [Fact(DisplayName = "AC-45: el filtro por moneda acota, y sin filtro se ven las dos")]
    public async Task AC45_FiltroPorMoneda()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        await cliente.MovimientoNuevo(1000m, comida.Id, Fechas.Hoy, "ARS");
        await cliente.MovimientoNuevo(50m, comida.Id, Fechas.Hoy, "USD");

        var soloDolares = await cliente.Movimientos("?moneda=USD");
        var soloPesos = await cliente.Movimientos("?moneda=ARS");
        var todas = await cliente.Movimientos();

        Assert.Equal(50m, Assert.Single(soloDolares).Monto);
        Assert.Equal(1000m, Assert.Single(soloPesos).Monto);
        Assert.Equal(2, todas.Count);
    }

    [Fact(DisplayName = "AC-45: el filtro por moneda se combina con el de categoria")]
    public async Task AC45_ElFiltroSeCombinaConElDeCategoria()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        var ocio = (await cliente.Categorias("Gasto")).First(c => c.Nombre == "Ocio");
        await cliente.MovimientoNuevo(1000m, comida.Id, Fechas.Hoy, "USD");
        await cliente.MovimientoNuevo(2000m, ocio.Id, Fechas.Hoy, "USD");
        await cliente.MovimientoNuevo(3000m, comida.Id, Fechas.Hoy, "ARS");

        var comidaEnDolares = await cliente.Movimientos($"?moneda=USD&categoriaId={comida.Id}");

        Assert.Equal(1000m, Assert.Single(comidaEnDolares).Monto);
    }

    [Fact(DisplayName = "Una moneda invalida en el filtro se rechaza con 400")]
    public async Task LaMonedaInvalidaEnElFiltroSeRechaza()
    {
        var cliente = await fabrica.ClienteConSesion();

        var respuesta = await cliente.ListarMovimientos("?moneda=ZZZ");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact(DisplayName = "AC-47: cambiar la moneda de un movimiento lo mueve de moneda")]
    public async Task AC47_LaMonedaSePuedeCorregir()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        var movimiento = await cliente.MovimientoNuevo(200m, comida.Id, Fechas.Hoy, "ARS");

        var respuesta = await cliente.ModificarMovimiento(
            movimiento.Id, 200m, comida.Id, Fechas.Hoy, "USD");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        // Deja de estar entre los de pesos y pasa a estar entre los de dolares.
        Assert.Empty(await cliente.Movimientos("?moneda=ARS"));
        Assert.Equal(200m, Assert.Single(await cliente.Movimientos("?moneda=USD")).Monto);
    }
}
