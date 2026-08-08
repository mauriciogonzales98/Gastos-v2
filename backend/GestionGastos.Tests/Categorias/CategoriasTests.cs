using System.Net;
using GestionGastos.Api.Entidades;
using GestionGastos.Tests.Infraestructura;

namespace GestionGastos.Tests.Categorias;

/// <summary>RF-06 a RF-09.</summary>
public class CategoriasTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    [Fact(DisplayName = "AC-10: un usuario nuevo ve solo categorias de gasto al pedir las de gasto")]
    public async Task AC10_ElSelectorDeGastosNoOfreceIngresos()
    {
        var cliente = await fabrica.ClienteConSesion();

        var categorias = await cliente.Categorias("Gasto");

        Assert.NotEmpty(categorias);
        Assert.All(categorias, c => Assert.Equal(TipoCategoria.Gasto, c.Tipo));
        Assert.All(categorias, c => Assert.True(c.EsDelSistema));
        Assert.Contains(categorias, c => c.Nombre == "Comida");
    }

    [Fact(DisplayName = "AC-10: el catalogo predefinido trae los dos tipos por separado")]
    public async Task AC10_ElCatalogoTieneGastosEIngresos()
    {
        var cliente = await fabrica.ClienteConSesion();

        var ingresos = await cliente.Categorias("Ingreso");

        Assert.All(ingresos, c => Assert.Equal(TipoCategoria.Ingreso, c.Tipo));
        Assert.Contains(ingresos, c => c.Nombre == "Sueldo");
        Assert.DoesNotContain(ingresos, c => c.Nombre == "Comida");
    }

    [Fact(DisplayName = "AC-11: una categoria del sistema no se puede renombrar ni eliminar")]
    public async Task AC11_LasDelSistemaSonIntocables()
    {
        var cliente = await fabrica.ClienteConSesion();
        var delSistema = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);

        var renombrado = await cliente.RenombrarCategoria(delSistema.Id, "Renombrada");
        var eliminacion = await cliente.EliminarCategoria(delSistema.Id);

        Assert.Equal(HttpStatusCode.Forbidden, renombrado.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, eliminacion.StatusCode);

        var categorias = await cliente.Categorias("Gasto");
        var sigueIgual = Assert.Single(categorias, c => c.Id == delSistema.Id);
        Assert.Equal(delSistema.Nombre, sigueIgual.Nombre);
    }

    [Fact(DisplayName = "AC-12: una categoria propia aparece solo para su dueno")]
    public async Task AC12_LaCategoriaPropiaEsPrivada()
    {
        var duenio = await fabrica.ClienteConSesion("duenio");
        var otro = await fabrica.ClienteConSesion("otro");

        var alta = await duenio.CrearCategoria("Mascotas", TipoCategoria.Gasto);
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);

        Assert.Contains(await duenio.Categorias("Gasto"), c => c.Nombre == "Mascotas");
        Assert.DoesNotContain(await otro.Categorias("Gasto"), c => c.Nombre == "Mascotas");
    }

    [Fact(DisplayName = "AC-12: la categoria propia de gasto no aparece entre las de ingreso")]
    public async Task AC12_LaCategoriaPropiaRespetaSuTipo()
    {
        var cliente = await fabrica.ClienteConSesion();
        await cliente.CrearCategoria("Freelance", TipoCategoria.Ingreso);

        Assert.Contains(await cliente.Categorias("Ingreso"), c => c.Nombre == "Freelance");
        Assert.DoesNotContain(await cliente.Categorias("Gasto"), c => c.Nombre == "Freelance");
    }

    [Fact(DisplayName = "AC-13: al renombrar una categoria propia, sus movimientos muestran el nombre nuevo")]
    public async Task AC13_ElRenombradoSeVeEnLosMovimientos()
    {
        var cliente = await fabrica.ClienteConSesion();
        var categoria = await (await cliente.CrearCategoria("Mascotas", TipoCategoria.Gasto))
            .LeerComo<Api.Categorias.CategoriaResponse>();
        await cliente.MovimientoNuevo(1500m, categoria.Id, Fechas.Hoy);

        var renombrado = await cliente.RenombrarCategoria(categoria.Id, "Veterinaria");

        Assert.Equal(HttpStatusCode.OK, renombrado.StatusCode);
        var movimientos = await cliente.Movimientos();
        Assert.Equal("Veterinaria", Assert.Single(movimientos).CategoriaNombre);
    }

    [Fact(DisplayName = "AC-14: la categoria dada de baja deja de ofrecerse pero sus movimientos conservan el nombre")]
    public async Task AC14_LaBajaEsLogica()
    {
        var cliente = await fabrica.ClienteConSesion();
        var categoria = await (await cliente.CrearCategoria("Mascotas", TipoCategoria.Gasto))
            .LeerComo<Api.Categorias.CategoriaResponse>();
        await cliente.MovimientoNuevo(1500m, categoria.Id, Fechas.Hoy);

        var baja = await cliente.EliminarCategoria(categoria.Id);

        Assert.Equal(HttpStatusCode.NoContent, baja.StatusCode);

        // Deja de ofrecerse en el formulario de registro...
        Assert.DoesNotContain(await cliente.Categorias("Gasto"), c => c.Id == categoria.Id);

        // ...pero el movimiento ya cargado sigue mostrandola.
        var movimiento = Assert.Single(await cliente.Movimientos());
        Assert.Equal("Mascotas", movimiento.CategoriaNombre);
        Assert.Equal(1500m, movimiento.Monto);
    }

    [Fact(DisplayName = "AC-14: una categoria dada de baja ya no se puede usar en un movimiento nuevo")]
    public async Task AC14_LaCategoriaDadaDeBajaNoSePuedeUsar()
    {
        var cliente = await fabrica.ClienteConSesion();
        var categoria = await (await cliente.CrearCategoria("Temporal", TipoCategoria.Gasto))
            .LeerComo<Api.Categorias.CategoriaResponse>();
        await cliente.EliminarCategoria(categoria.Id);

        var alta = await cliente.CrearMovimiento(100m, categoria.Id, Fechas.Hoy);

        Assert.Equal(HttpStatusCode.BadRequest, alta.StatusCode);
    }

    [Fact(DisplayName = "AC-07: la categoria de otro usuario no se puede tocar ni existe para el resto")]
    public async Task AC07_LaCategoriaDeOtroEsInaccesible()
    {
        var duenio = await fabrica.ClienteConSesion("duenio");
        var intruso = await fabrica.ClienteConSesion("intruso");
        var categoria = await (await duenio.CrearCategoria("Privada", TipoCategoria.Gasto))
            .LeerComo<Api.Categorias.CategoriaResponse>();

        var renombrado = await intruso.RenombrarCategoria(categoria.Id, "Hackeada");
        var eliminacion = await intruso.EliminarCategoria(categoria.Id);

        Assert.Equal(HttpStatusCode.NotFound, renombrado.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, eliminacion.StatusCode);

        var sigueIgual = Assert.Single(await duenio.Categorias("Gasto"), c => c.Id == categoria.Id);
        Assert.Equal("Privada", sigueIgual.Nombre);
    }

    [Theory(DisplayName = "El alta de categoria rechaza nombre vacio o tipo faltante")]
    [InlineData("{\"nombre\":\"\",\"tipo\":\"Gasto\"}")]
    [InlineData("{\"nombre\":\"   \",\"tipo\":\"Gasto\"}")]
    [InlineData("{\"nombre\":\"Valida\"}")]
    public async Task ElAltaValidaLosDatos(string cuerpo)
    {
        var cliente = await fabrica.ClienteConSesion();

        var respuesta = await cliente.PostAsync(
            "/categorias",
            new StringContent(cuerpo, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact(DisplayName = "AC-05: sin sesion no se pueden listar ni crear categorias")]
    public async Task AC05_SinSesionNoHayCategorias()
    {
        var anonimo = fabrica.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.ListarCategorias()).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonimo.CrearCategoria("Cualquiera", TipoCategoria.Gasto)).StatusCode);
    }
}
