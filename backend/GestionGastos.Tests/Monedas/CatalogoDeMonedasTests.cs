using System.Net;
using GestionGastos.Api.Entidades;
using GestionGastos.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace GestionGastos.Tests.Monedas;

/// <summary>
/// RF-24: el catalogo de monedas es una tabla. Estos tests existen para que sumar una
/// moneda mas adelante sea insertar una fila y nada mas.
/// </summary>
public class CatalogoDeMonedasTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    [Fact(DisplayName = "GET /monedas devuelve el catalogo con lo que el frontend necesita")]
    public async Task ElCatalogoSeExpone()
    {
        var cliente = await fabrica.ClienteConSesion();

        var monedas = await cliente.Monedas();

        var pesos = Assert.Single(monedas, m => m.Codigo == "ARS");
        Assert.Equal("Pesos", pesos.Nombre);
        Assert.Equal("$", pesos.Simbolo);
        Assert.Equal(2, pesos.Decimales);

        var dolares = Assert.Single(monedas, m => m.Codigo == "USD");
        Assert.Equal("US$", dolares.Simbolo);
    }

    [Fact(DisplayName = "RF-25: hay exactamente una moneda predeterminada")]
    public async Task HayUnaSolaPredeterminada()
    {
        var cliente = await fabrica.ClienteConSesion();

        var monedas = await cliente.Monedas();

        Assert.Equal("ARS", Assert.Single(monedas, m => m.EsPredeterminada).Codigo);
    }

    [Fact(DisplayName = "El codigo de moneda no distingue mayusculas")]
    public async Task ElCodigoNoDistingueMayusculas()
    {
        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);

        var creado = await cliente.MovimientoNuevo(10m, comida.Id, Fechas.Hoy, "usd");

        // Se guarda con el codigo canonico del catalogo, no como lo escribio el cliente.
        Assert.Equal("USD", creado.Moneda);
    }

    [Fact(DisplayName = "El error de moneda invalida enumera el catalogo, no dos monedas fijas")]
    public async Task ElErrorEnumeraElCatalogo()
    {
        var cliente = await fabrica.ClienteConSesion();

        // "ZZZ" y no "EUR": otro test de esta clase agrega el euro al catalogo, y los
        // tests comparten la base.
        var respuesta = await cliente.ListarMovimientos("?moneda=ZZZ");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("ARS", cuerpo, StringComparison.Ordinal);
        Assert.Contains("USD", cuerpo, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "AC-49: una moneda agregada solo como dato queda usable de punta a punta")]
    public async Task AC49_SumarUnaMonedaEsInsertarUnaFila()
    {
        // Esta es la razon de ser de la tabla: se inserta la fila y no se toca una linea
        // de codigo ni del backend ni del frontend.
        await fabrica.ConsultarBase(async db =>
        {
            if (!await db.Monedas.AnyAsync(m => m.Codigo == "EUR"))
            {
                db.Monedas.Add(new Moneda
                {
                    Codigo = "EUR",
                    Nombre = "Euros",
                    Simbolo = "€",
                    Decimales = 2,
                    EsPredeterminada = false,
                    Orden = 3,
                });
                await db.SaveChangesAsync();
            }

            return true;
        });

        var cliente = await fabrica.ClienteConSesion();
        var comida = await cliente.CategoriaDelSistema(TipoCategoria.Gasto);

        // Aparece en el catalogo que consume el frontend...
        Assert.Contains(await cliente.Monedas(), m => m.Codigo == "EUR");

        // ...se puede registrar un movimiento con ella...
        var creado = await cliente.MovimientoNuevo(75m, comida.Id, Fechas.Hoy, "EUR");
        Assert.Equal("EUR", creado.Moneda);

        // ...y el filtro la reconoce.
        Assert.Equal(75m, Assert.Single(await cliente.Movimientos("?moneda=EUR")).Monto);
    }

    [Fact(DisplayName = "AC-05: el catalogo tambien exige sesion")]
    public async Task AC05_ElCatalogoExigeSesion()
    {
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await fabrica.CreateClient().ListarMonedas()).StatusCode);
    }
}
