using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GestionGastos.Api.Categorias;
using GestionGastos.Api.Entidades;
using GestionGastos.Api.Monedas;
using GestionGastos.Api.Movimientos;

namespace GestionGastos.Tests.Infraestructura;

/// <summary>Atajos para los endpoints de negocio, para que los tests digan que verifican.</summary>
public static class ClienteApi
{
    /// <summary>
    /// La API manda los enums como texto; el deserializador por defecto de los tests no
    /// sabe leerlos sin este converter.
    /// </summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Un cliente con sesion abierta y su usuario recien creado.</summary>
    public static async Task<HttpClient> ClienteConSesion(this FabricaApi fabrica, string prefijo = "usuario")
    {
        var cliente = fabrica.CreateClient();
        var alta = await cliente.Registrar(ClienteAutenticacion.EmailNuevo(prefijo));
        alta.EnsureSuccessStatusCode();
        return cliente;
    }

    public static async Task<T> LeerComo<T>(this HttpResponseMessage respuesta) =>
        (await respuesta.Content.ReadFromJsonAsync<T>(Json))!;

    // --- Categorias ---

    // --- Monedas ---

    public static Task<HttpResponseMessage> ListarMonedas(this HttpClient cliente) =>
        cliente.GetAsync("/monedas");

    public static async Task<List<MonedaResponse>> Monedas(this HttpClient cliente) =>
        await (await cliente.ListarMonedas()).LeerComo<List<MonedaResponse>>();

    public static Task<HttpResponseMessage> ListarCategorias(this HttpClient cliente, string? tipo = null) =>
        cliente.GetAsync(tipo is null ? "/categorias" : $"/categorias?tipo={tipo}");

    public static async Task<List<CategoriaResponse>> Categorias(this HttpClient cliente, string? tipo = null) =>
        await (await cliente.ListarCategorias(tipo)).LeerComo<List<CategoriaResponse>>();

    public static Task<HttpResponseMessage> CrearCategoria(
        this HttpClient cliente, string nombre, TipoCategoria tipo) =>
        cliente.PostAsJsonAsync("/categorias", new { nombre, tipo = tipo.ToString() });

    public static Task<HttpResponseMessage> RenombrarCategoria(
        this HttpClient cliente, Guid id, string nombre) =>
        cliente.PutAsJsonAsync($"/categorias/{id}", new { nombre });

    public static Task<HttpResponseMessage> EliminarCategoria(this HttpClient cliente, Guid id) =>
        cliente.DeleteAsync($"/categorias/{id}");

    /// <summary>La primera categoria predefinida del tipo pedido, para no repetir el lookup.</summary>
    public static async Task<CategoriaResponse> CategoriaDelSistema(
        this HttpClient cliente, TipoCategoria tipo) =>
        (await cliente.Categorias(tipo.ToString())).First(c => c.EsDelSistema);

    // --- Movimientos ---

    /// <summary>Sin moneda explicita queda en la predeterminada del catalogo (AC-38).</summary>
    public static Task<HttpResponseMessage> CrearMovimiento(
        this HttpClient cliente,
        decimal monto,
        Guid categoriaId,
        DateOnly? fecha = null,
        string? moneda = null) =>
        cliente.PostAsJsonAsync("/movimientos", new
        {
            monto,
            categoriaId,
            fecha = fecha?.ToString("yyyy-MM-dd"),
            moneda,
        });

    public static Task<HttpResponseMessage> ModificarMovimiento(
        this HttpClient cliente,
        Guid id,
        decimal monto,
        Guid categoriaId,
        DateOnly fecha,
        string? moneda = null) =>
        cliente.PutAsJsonAsync($"/movimientos/{id}", new
        {
            monto,
            categoriaId,
            fecha = fecha.ToString("yyyy-MM-dd"),
            moneda,
        });

    public static Task<HttpResponseMessage> EliminarMovimiento(this HttpClient cliente, Guid id) =>
        cliente.DeleteAsync($"/movimientos/{id}");

    public static Task<HttpResponseMessage> ListarMovimientos(this HttpClient cliente, string consulta = "") =>
        cliente.GetAsync($"/movimientos{consulta}");

    public static async Task<List<MovimientoResponse>> Movimientos(
        this HttpClient cliente, string consulta = "") =>
        await (await cliente.ListarMovimientos(consulta)).LeerComo<List<MovimientoResponse>>();

    /// <summary>Crea un movimiento y devuelve el que quedo guardado.</summary>
    public static async Task<MovimientoResponse> MovimientoNuevo(
        this HttpClient cliente,
        decimal monto,
        Guid categoriaId,
        DateOnly? fecha = null,
        string? moneda = null)
    {
        var respuesta = await cliente.CrearMovimiento(monto, categoriaId, fecha, moneda);
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.LeerComo<MovimientoResponse>();
    }
}

/// <summary>Fechas de referencia para los tests que dependen del "mes actual" (RF-18).</summary>
public static class Fechas
{
    public static DateOnly Hoy => DateOnly.FromDateTime(DateTime.Now);

    public static DateOnly PrimeroDelMes => new(Hoy.Year, Hoy.Month, 1);

    public static DateOnly UltimoDelMes => PrimeroDelMes.AddMonths(1).AddDays(-1);

    public static DateOnly DelMesPasado => PrimeroDelMes.AddDays(-1);
}
