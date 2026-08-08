using System.Net;
using GestionGastos.Api.Dashboard;
using GestionGastos.Api.Entidades;
using GestionGastos.Tests.Infraestructura;

namespace GestionGastos.Tests.Dashboard;

/// <summary>RF-19 a RF-22 y RF-29 a RF-30.</summary>
public class DashboardTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    [Fact(DisplayName = "AC-27: el total por categoria es la suma de los gastos de esa categoria")]
    public async Task AC27_TotalPorCategoria()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        var ocio = (await cliente.Categorias("Gasto")).First(c => c.Nombre == "Ocio");
        await cliente.MovimientoNuevo(1000m, comida.Id, Fechas.Hoy);
        await cliente.MovimientoNuevo(500.50m, comida.Id, Fechas.Hoy);
        await cliente.MovimientoNuevo(300m, ocio.Id, Fechas.Hoy);

        var pesos = await cliente.DashboardDe("ARS");

        Assert.Equal(1500.50m, TotalDe(pesos, "Comida"));
        Assert.Equal(300m, TotalDe(pesos, "Ocio"));
        Assert.Equal(1800.50m, pesos.TotalGastos);
    }

    [Fact(DisplayName = "AC-28: el balance es ingresos menos gastos del periodo")]
    public async Task AC28_Balance()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        var sueldo = await cliente.CategoriaDelSistema(TipoCategoria.Ingreso);
        await cliente.MovimientoNuevo(90000m, sueldo.Id, Fechas.Hoy);
        await cliente.MovimientoNuevo(1000m, comida.Id, Fechas.Hoy);

        var pesos = await cliente.DashboardDe("ARS");

        Assert.Equal(90000m, pesos.TotalIngresos);
        Assert.Equal(1000m, pesos.TotalGastos);
        Assert.Equal(89000m, pesos.Balance);
    }

    [Fact(DisplayName = "AC-29: el filtro de fechas acota los totales, con extremos incluidos")]
    public async Task AC29_FiltroDeFechas()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        await cliente.MovimientoNuevo(100m, comida.Id, Fechas.PrimeroDelMes);
        await cliente.MovimientoNuevo(200m, comida.Id, Fechas.UltimoDelMes);
        await cliente.MovimientoNuevo(999m, comida.Id, Fechas.DelMesPasado);

        var mesActual = await cliente.DashboardDe(
            "ARS", $"?desde={Fechas.PrimeroDelMes:yyyy-MM-dd}&hasta={Fechas.UltimoDelMes:yyyy-MM-dd}");

        // Los dos extremos entran y el del mes pasado queda afuera.
        Assert.Equal(300m, mesActual.TotalGastos);
    }

    [Fact(DisplayName = "AC-25 / AC-30: sin filtro de fechas, el dashboard es el del mes actual")]
    public async Task AC30_ElDefaultEsElMesActual()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        await cliente.MovimientoNuevo(100m, comida.Id, Fechas.Hoy);
        await cliente.MovimientoNuevo(999m, comida.Id, Fechas.DelMesPasado);

        var sinFiltro = await cliente.Dashboard();
        var conMesActual = await cliente.Dashboard(
            $"?desde={Fechas.PrimeroDelMes:yyyy-MM-dd}&hasta={Fechas.UltimoDelMes:yyyy-MM-dd}");

        // El resumen de la pantalla principal usa este mismo endpoint con el mes actual,
        // asi que por construccion no puede diferir del dashboard.
        Assert.Equal(Fechas.PrimeroDelMes, sinFiltro.Desde);
        Assert.Equal(Fechas.UltimoDelMes, sinFiltro.Hasta);
        Assert.Equal(100m, Moneda(sinFiltro, "ARS").TotalGastos);
        Assert.Equal(Moneda(conMesActual, "ARS").TotalGastos, Moneda(sinFiltro, "ARS").TotalGastos);
    }

    [Fact(DisplayName = "AC-31: sin movimientos, todo en cero y sin error")]
    public async Task AC31_PeriodoVacio()
    {
        var cliente = await fabrica.ClienteConSesion();

        var dashboard = await cliente.Dashboard("?desde=2020-01-01&hasta=2020-01-31");

        // Todas las monedas del catalogo aparecen, en cero: no falta ninguna.
        Assert.NotEmpty(dashboard.Monedas);
        Assert.All(dashboard.Monedas, m =>
        {
            Assert.Equal(0m, m.TotalIngresos);
            Assert.Equal(0m, m.TotalGastos);
            Assert.Equal(0m, m.Balance);
            Assert.Empty(m.GastosPorCategoria);
        });
    }

    [Fact(DisplayName = "AC-41: los totales por categoria no mezclan monedas")]
    public async Task AC41_TotalesPorCategoriaSeparadosPorMoneda()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        // La misma categoria, el mismo periodo, monedas distintas.
        await cliente.MovimientoNuevo(1000m, comida.Id, Fechas.Hoy, "ARS");
        await cliente.MovimientoNuevo(500m, comida.Id, Fechas.Hoy, "ARS");
        await cliente.MovimientoNuevo(30m, comida.Id, Fechas.Hoy, "USD");

        var dashboard = await cliente.Dashboard();

        Assert.Equal(1500m, TotalDe(Moneda(dashboard, "ARS"), "Comida"));
        Assert.Equal(30m, TotalDe(Moneda(dashboard, "USD"), "Comida"));
    }

    [Fact(DisplayName = "AC-42: hay un balance por moneda y ninguno incluye a la otra")]
    public async Task AC42_BalanceSeparadoPorMoneda()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        var sueldo = await cliente.CategoriaDelSistema(TipoCategoria.Ingreso);
        await cliente.MovimientoNuevo(90000m, sueldo.Id, Fechas.Hoy, "ARS");
        await cliente.MovimientoNuevo(10000m, comida.Id, Fechas.Hoy, "ARS");
        await cliente.MovimientoNuevo(500m, sueldo.Id, Fechas.Hoy, "USD");
        await cliente.MovimientoNuevo(120m, comida.Id, Fechas.Hoy, "USD");

        var dashboard = await cliente.Dashboard();

        Assert.Equal(80000m, Moneda(dashboard, "ARS").Balance);
        Assert.Equal(380m, Moneda(dashboard, "USD").Balance);

        // El chequeo que importa: ningun balance es el de la suma cruzada.
        Assert.NotEqual(80380m, Moneda(dashboard, "ARS").Balance);
    }

    [Fact(DisplayName = "AC-46: el filtro por moneda deja solo esa moneda")]
    public async Task AC46_FiltroPorMoneda()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);
        await cliente.MovimientoNuevo(1000m, comida.Id, Fechas.Hoy, "ARS");
        await cliente.MovimientoNuevo(30m, comida.Id, Fechas.Hoy, "USD");

        var soloDolares = await cliente.Dashboard("?moneda=USD");
        var sinFiltro = await cliente.Dashboard();

        Assert.Equal("USD", Assert.Single(soloDolares.Monedas).Moneda);
        Assert.Equal(30m, soloDolares.Monedas[0].TotalGastos);
        Assert.True(sinFiltro.Monedas.Count >= 2);
    }

    [Fact(DisplayName = "AC-06: el dashboard de A no incluye movimientos de B")]
    public async Task AC06_ElDashboardEsPorUsuario()
    {
        var clienteA = await fabrica.ClienteConSesion("a");
        var clienteB = await fabrica.ClienteConSesion("b");
        var categoriaA = await clienteA.CategoriaDelSistema(TipoCategoria.Gasto);
        var categoriaB = await clienteB.CategoriaDelSistema(TipoCategoria.Gasto);
        await clienteA.MovimientoNuevo(111m, categoriaA.Id, Fechas.Hoy);
        await clienteB.MovimientoNuevo(999m, categoriaB.Id, Fechas.Hoy);

        Assert.Equal(111m, (await clienteA.DashboardDe("ARS")).TotalGastos);
        Assert.Equal(999m, (await clienteB.DashboardDe("ARS")).TotalGastos);
    }

    [Fact(DisplayName = "Un rango invertido o una moneda invalida se rechazan con 400")]
    public async Task LosParametrosInvalidosSeRechazan()
    {
        var cliente = await fabrica.ClienteConSesion();

        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await cliente.GetAsync("/dashboard?desde=2026-03-31&hasta=2026-03-01")).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await cliente.GetAsync("/dashboard?moneda=ZZZ")).StatusCode);
    }

    [Fact(DisplayName = "AC-05: sin sesion el dashboard responde 401")]
    public async Task AC05_ElDashboardExigeSesion()
    {
        var respuesta = await fabrica.CreateClient().GetAsync("/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    private static ResumenDeMoneda Moneda(DashboardResponse dashboard, string codigo) =>
        Assert.Single(dashboard.Monedas, m => m.Moneda == codigo);

    private static decimal TotalDe(ResumenDeMoneda moneda, string categoria) =>
        moneda.GastosPorCategoria
            .Where(c => c.CategoriaNombre == categoria)
            .Select(c => c.Total)
            .FirstOrDefault();
}
