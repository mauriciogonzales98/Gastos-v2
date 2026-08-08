using System.Net;
using System.Text;
using GestionGastos.Api.Entidades;
using GestionGastos.Tests.Infraestructura;

namespace GestionGastos.Tests.Movimientos;

/// <summary>RF-10 a RF-18.</summary>
public class MovimientosTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    [Fact(DisplayName = "AC-15: el gasto guardado aparece en el listado con su categoria")]
    public async Task AC15_ElGastoSeGuardaYSeLista()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);

        var creado = await cliente.MovimientoNuevo(1234.56m, comida.Id, Fechas.Hoy);

        var movimiento = Assert.Single(await cliente.Movimientos());
        Assert.Equal(creado.Id, movimiento.Id);
        Assert.Equal(1234.56m, movimiento.Monto);
        Assert.Equal(comida.Nombre, movimiento.CategoriaNombre);
        Assert.Equal(TipoCategoria.Gasto, movimiento.Tipo);
    }

    [Fact(DisplayName = "AC-22: el listado trae gastos e ingresos juntos")]
    public async Task AC22_ElListadoTraeLosDosTipos()
    {
        var cliente = await fabrica.ClienteConSesion();
        var gasto = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        var ingreso = await cliente.CategoriaDelSistema(TipoCategoria.Ingreso);

        await cliente.MovimientoNuevo(500m, gasto.Id, Fechas.Hoy);
        await cliente.MovimientoNuevo(9000m, ingreso.Id, Fechas.Hoy);

        var movimientos = await cliente.Movimientos();

        Assert.Equal(2, movimientos.Count);
        Assert.Contains(movimientos, m => m.Tipo == TipoCategoria.Gasto);
        Assert.Contains(movimientos, m => m.Tipo == TipoCategoria.Ingreso);
    }

    [Fact(DisplayName = "AC-17: sin fecha, el movimiento queda con la de hoy")]
    public async Task AC17_LaFechaPorDefectoEsHoy()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);

        var creado = await cliente.MovimientoNuevo(300m, comida.Id, fecha: null);

        Assert.Equal(Fechas.Hoy, creado.Fecha);
    }

    [Theory(DisplayName = "AC-18: monto vacio, cero, negativo o con mas de dos decimales se rechaza")]
    [InlineData("{\"categoriaId\":\"CATEGORIA\"}")]
    [InlineData("{\"monto\":0,\"categoriaId\":\"CATEGORIA\"}")]
    [InlineData("{\"monto\":-50,\"categoriaId\":\"CATEGORIA\"}")]
    [InlineData("{\"monto\":10.999,\"categoriaId\":\"CATEGORIA\"}")]
    public async Task AC18_ElMontoInvalidoSeRechaza(string plantilla)
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        var cuerpo = plantilla.Replace("CATEGORIA", comida.Id.ToString(), StringComparison.Ordinal);

        var respuesta = await cliente.PostAsync(
            "/movimientos", new StringContent(cuerpo, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Empty(await cliente.Movimientos());
    }

    [Fact(DisplayName = "AC-18: sin categoria el movimiento se rechaza y no se crea")]
    public async Task AC18_SinCategoriaSeRechaza()
    {
        var cliente = await fabrica.ClienteConSesion();

        var respuesta = await cliente.PostAsync(
            "/movimientos",
            new StringContent("{\"monto\":100}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Empty(await cliente.Movimientos());
    }

    [Fact(DisplayName = "AC-19: al modificar el monto, el listado muestra el nuevo y no el anterior")]
    public async Task AC19_LaModificacionDelMontoSeRefleja()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        var movimiento = await cliente.MovimientoNuevo(1000m, comida.Id, Fechas.Hoy);

        var respuesta = await cliente.ModificarMovimiento(movimiento.Id, 2500.75m, comida.Id, Fechas.Hoy);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(2500.75m, Assert.Single(await cliente.Movimientos()).Monto);
    }

    [Fact(DisplayName = "AC-20: al cambiar categoria y fecha, el movimiento sale del periodo filtrado")]
    public async Task AC20_ElCambioDeCategoriaYFechaSeRefleja()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        var ocio = (await cliente.Categorias("Gasto")).First(c => c.Nombre == "Ocio");
        var movimiento = await cliente.MovimientoNuevo(800m, comida.Id, Fechas.Hoy);

        await cliente.ModificarMovimiento(movimiento.Id, 800m, ocio.Id, Fechas.DelMesPasado);

        // Con el filtro por defecto (mes actual) ya no aparece.
        Assert.Empty(await cliente.Movimientos());

        // Y en el mes pasado aparece con la categoria nueva.
        var delMesPasado = Assert.Single(
            await cliente.Movimientos($"?desde={Fechas.DelMesPasado:yyyy-MM-dd}&hasta={Fechas.DelMesPasado:yyyy-MM-dd}"));
        Assert.Equal("Ocio", delMesPasado.CategoriaNombre);
    }

    [Fact(DisplayName = "AC-21: el movimiento eliminado deja de aparecer")]
    public async Task AC21_LaEliminacionSacaElMovimiento()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        var movimiento = await cliente.MovimientoNuevo(700m, comida.Id, Fechas.Hoy);

        var respuesta = await cliente.EliminarMovimiento(movimiento.Id);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Empty(await cliente.Movimientos());
    }

    [Fact(DisplayName = "AC-23 y AC-24: el filtro por categoria acota, y sin filtro se ven todas")]
    public async Task AC23_AC24_FiltroPorCategoria()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        var ocio = (await cliente.Categorias("Gasto")).First(c => c.Nombre == "Ocio");
        await cliente.MovimientoNuevo(100m, comida.Id, Fechas.Hoy);
        await cliente.MovimientoNuevo(200m, ocio.Id, Fechas.Hoy);

        var soloComida = await cliente.Movimientos($"?categoriaId={comida.Id}");
        var todas = await cliente.Movimientos();

        Assert.Equal(comida.Id, Assert.Single(soloComida).CategoriaId);
        Assert.Equal(2, todas.Count);
    }

    [Fact(DisplayName = "AC-25: sin filtro de fecha se ve solo el mes actual")]
    public async Task AC25_ElDefaultEsElMesActual()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        await cliente.MovimientoNuevo(100m, comida.Id, Fechas.Hoy);
        await cliente.MovimientoNuevo(999m, comida.Id, Fechas.DelMesPasado);

        var movimientos = await cliente.Movimientos();

        Assert.Equal(100m, Assert.Single(movimientos).Monto);
    }

    [Fact(DisplayName = "AC-26: el rango de fechas incluye sus extremos")]
    public async Task AC26_ElRangoIncluyeLosExtremos()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        var primero = Fechas.PrimeroDelMes;
        var ultimo = Fechas.UltimoDelMes;
        await cliente.MovimientoNuevo(10m, comida.Id, primero);
        await cliente.MovimientoNuevo(20m, comida.Id, ultimo);
        await cliente.MovimientoNuevo(30m, comida.Id, Fechas.DelMesPasado);

        var enElRango = await cliente.Movimientos($"?desde={primero:yyyy-MM-dd}&hasta={ultimo:yyyy-MM-dd}");

        Assert.Equal(2, enElRango.Count);
        Assert.Contains(enElRango, m => m.Fecha == primero);
        Assert.Contains(enElRango, m => m.Fecha == ultimo);
    }

    [Fact(DisplayName = "Un rango invertido se rechaza con 400 en vez de devolver vacio")]
    public async Task ElRangoInvertidoSeRechaza()
    {
        var cliente = await fabrica.ClienteConSesion();

        var respuesta = await cliente.ListarMovimientos("?desde=2026-03-31&hasta=2026-03-01");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact(DisplayName = "AC-05: sin sesion no se listan ni se crean movimientos")]
    public async Task AC05_SinSesionNoHayMovimientos()
    {
        var anonimo = fabrica.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.ListarMovimientos()).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonimo.CrearMovimiento(100m, Guid.NewGuid(), Fechas.Hoy)).StatusCode);
    }
}
