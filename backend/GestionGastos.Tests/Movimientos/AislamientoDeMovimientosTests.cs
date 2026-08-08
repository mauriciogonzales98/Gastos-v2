using System.Net;
using System.Text;
using GestionGastos.Api.Entidades;
using GestionGastos.Tests.Infraestructura;

namespace GestionGastos.Tests.Movimientos;

/// <summary>
/// RF-04: cada usuario accede unicamente a sus propios datos. Son los AC que quedaron
/// pendientes de la feature 1 porque necesitaban movimientos para poder verificarse.
/// </summary>
public class AislamientoDeMovimientosTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    [Fact(DisplayName = "AC-06: el listado de A no muestra ningun movimiento de B")]
    public async Task AC06_CadaUnoVeSoloLoSuyo()
    {
        var (clienteA, clienteB) = await DosUsuariosConUnMovimientoCadaUno();

        var deA = await clienteA.Movimientos();
        var deB = await clienteB.Movimientos();

        Assert.Equal(111m, Assert.Single(deA).Monto);
        Assert.Equal(222m, Assert.Single(deB).Monto);
    }

    [Fact(DisplayName = "AC-07: A no puede modificar ni eliminar un movimiento de B indicando su id")]
    public async Task AC07_ElMovimientoDeOtroEsInaccesible()
    {
        var (clienteA, clienteB) = await DosUsuariosConUnMovimientoCadaUno();
        var deB = Assert.Single(await clienteB.Movimientos());
        var categoriaDeA = await clienteA.CategoriaDelSistema(TipoCategoria.Gasto);

        var modificacion = await clienteA.ModificarMovimiento(deB.Id, 1m, categoriaDeA.Id, Fechas.Hoy);
        var eliminacion = await clienteA.EliminarMovimiento(deB.Id);

        Assert.Equal(HttpStatusCode.NotFound, modificacion.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, eliminacion.StatusCode);

        // El movimiento de B quedo sin cambios.
        var sigueIgual = Assert.Single(await clienteB.Movimientos());
        Assert.Equal(deB.Id, sigueIgual.Id);
        Assert.Equal(222m, sigueIgual.Monto);
    }

    [Fact(DisplayName = "AC-07: filtrar por una categoria de otro usuario no devuelve sus movimientos")]
    public async Task AC07_NiSiquieraPorFiltro()
    {
        var (clienteA, clienteB) = await DosUsuariosConUnMovimientoCadaUno();
        var deB = Assert.Single(await clienteB.Movimientos());

        var intento = await clienteA.Movimientos($"?categoriaId={deB.CategoriaId}");

        Assert.Empty(intento);
    }

    [Fact(DisplayName = "AC-08: mandar el usuario de B en el alta no cambia el dueno del movimiento")]
    public async Task AC08_ElDuenoSaleDeLaSesion()
    {
        var (clienteA, clienteB) = await DosUsuariosConUnMovimientoCadaUno();
        var deB = Assert.Single(await clienteB.Movimientos());
        var categoriaDeA = await clienteA.CategoriaDelSistema(TipoCategoria.Gasto);

        // El id de usuario ni siquiera esta en el contrato: si el cliente lo manda, se ignora.
        var cuerpo = $$"""
            {"monto":333,"categoriaId":"{{categoriaDeA.Id}}","fecha":"{{Fechas.Hoy:yyyy-MM-dd}}",
             "usuarioId":"{{Guid.NewGuid()}}"}
            """;

        var alta = await clienteA.PostAsync(
            "/movimientos", new StringContent(cuerpo, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);

        // Quedo en A...
        Assert.Contains(await clienteA.Movimientos(), m => m.Monto == 333m);

        // ...y el listado de B no cambio.
        var deBAhora = Assert.Single(await clienteB.Movimientos());
        Assert.Equal(deB.Id, deBAhora.Id);
    }

    private async Task<(HttpClient A, HttpClient B)> DosUsuariosConUnMovimientoCadaUno()
    {
        var clienteA = await fabrica.ClienteConSesion("a");
        var clienteB = await fabrica.ClienteConSesion("b");

        var categoriaA = await clienteA.CategoriaDelSistema(TipoCategoria.Gasto);
        // B usa una categoria propia, para que tambien se vea que el filtro por categoria
        // ajena no filtra nada de otro usuario.
        var categoriaB = await (await clienteB.CrearCategoria("Solo de B", TipoCategoria.Gasto))
            .LeerComo<Api.Categorias.CategoriaResponse>();

        await clienteA.MovimientoNuevo(111m, categoriaA.Id, Fechas.Hoy);
        await clienteB.MovimientoNuevo(222m, categoriaB.Id, Fechas.Hoy);

        return (clienteA, clienteB);
    }
}
