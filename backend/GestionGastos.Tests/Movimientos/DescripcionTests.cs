using System.Net;
using GestionGastos.Api.Entidades;
using GestionGastos.Tests.Infraestructura;

namespace GestionGastos.Tests.Movimientos;

/// <summary>RF-33: la nota descriptiva opcional del movimiento.</summary>
public class DescripcionTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    [Fact(DisplayName = "AC-50: la nota se guarda y viaja en el listado")]
    public async Task AC50_LaNotaSeGuardaYSeLista()
    {
        var cliente = await fabrica.ClienteConSesion();
        var vivienda = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);

        var creado = await cliente.MovimientoNuevo(
            450000m, vivienda.Id, Fechas.Hoy, descripcion: "alquiler agosto");

        Assert.Equal("alquiler agosto", creado.Descripcion);
        Assert.Equal("alquiler agosto", Assert.Single(await cliente.Movimientos()).Descripcion);
    }

    [Theory(DisplayName = "AC-51: omitida, vacia o en blanco son todas 'sin nota' y quedan en null")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AC51_SinNotaQuedaEnNull(string? descripcion)
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);

        var creado = await cliente.MovimientoNuevo(500m, comida.Id, Fechas.Hoy, descripcion: descripcion);

        // Un solo estado "sin nota": nunca cadena vacia.
        Assert.Null(creado.Descripcion);
        Assert.Null(Assert.Single(await cliente.Movimientos()).Descripcion);
    }

    [Fact(DisplayName = "AC-52: una nota mas larga que el maximo se rechaza y no crea nada")]
    public async Task AC52_LaNotaDemasiadoLargaSeRechaza()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        var excedida = new string('a', Movimiento.LargoMaximoDescripcion + 1);

        var respuesta = await cliente.CrearMovimiento(
            500m, comida.Id, Fechas.Hoy, descripcion: excedida);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Empty(await cliente.Movimientos());
    }

    [Fact(DisplayName = "AC-52: una nota justo en el maximo se acepta")]
    public async Task AC52_LaNotaEnElLimiteSeAcepta()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        var justa = new string('a', Movimiento.LargoMaximoDescripcion);

        var creado = await cliente.MovimientoNuevo(500m, comida.Id, Fechas.Hoy, descripcion: justa);

        Assert.Equal(justa, creado.Descripcion);
    }

    [Fact(DisplayName = "La nota se recorta: los espacios de los bordes no ocupan ni cuentan")]
    public async Task LaNotaSeRecorta()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        // Con los espacios se pasa del maximo; recortada, entra justo.
        var conEspacios = "   " + new string('a', Movimiento.LargoMaximoDescripcion) + "   ";

        var creado = await cliente.MovimientoNuevo(500m, comida.Id, Fechas.Hoy, descripcion: conEspacios);

        Assert.Equal(new string('a', Movimiento.LargoMaximoDescripcion), creado.Descripcion);
    }

    [Fact(DisplayName = "AC-53: editar la nota la cambia sin tocar los totales del dashboard")]
    public async Task AC53_EditarLaNotaNoMueveLosTotales()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        var creado = await cliente.MovimientoNuevo(
            1000m, comida.Id, Fechas.Hoy, descripcion: "verduleria");
        var gastadoAntes = (await cliente.DashboardDe("ARS")).TotalGastos;

        var respuesta = await cliente.ModificarMovimiento(
            creado.Id, 1000m, comida.Id, Fechas.Hoy, descripcion: "verduleria del barrio");
        respuesta.EnsureSuccessStatusCode();

        Assert.Equal(
            "verduleria del barrio",
            Assert.Single(await cliente.Movimientos()).Descripcion);
        Assert.Equal(gastadoAntes, (await cliente.DashboardDe("ARS")).TotalGastos);
    }

    [Fact(DisplayName = "AC-53: mandar la nota en blanco al editar la borra")]
    public async Task AC53_LaNotaSePuedeBorrarEditando()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        var creado = await cliente.MovimientoNuevo(
            1000m, comida.Id, Fechas.Hoy, descripcion: "algo");

        var respuesta = await cliente.ModificarMovimiento(
            creado.Id, 1000m, comida.Id, Fechas.Hoy, descripcion: "");
        respuesta.EnsureSuccessStatusCode();

        Assert.Null(Assert.Single(await cliente.Movimientos()).Descripcion);
    }
}
