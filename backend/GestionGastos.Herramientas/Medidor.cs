using System.Diagnostics;
using System.Net.Http.Json;

namespace GestionGastos.Herramientas;

/// <summary>
/// F.2 del paso final: mide contra la API real y MySQL, no contra los tests (esos corren
/// sobre SQLite en memoria y darian un numero que no existe en produccion).
/// </summary>
public static class Medidor
{
    /// <summary>Los tres AC piden explicitamente 100 ejecuciones.</summary>
    private const int Ejecuciones = 100;

    /// <summary>
    /// Corridas que se tiran antes de empezar a medir. La primera paga JIT, apertura del
    /// pool de conexiones y el primer plan de consulta: incluirla mediria el arranque, no
    /// el estado estable que describen los RNF.
    /// </summary>
    private const int Calentamiento = 10;

    public static async Task<int> Medir(string urlBase)
    {
        using var manejador = new HttpClientHandler { UseCookies = true };
        using var cliente = new HttpClient(manejador) { BaseAddress = new Uri(urlBase) };

        await IniciarSesion(cliente);

        var categoriaId = await PrimeraCategoriaDeGasto(cliente);

        // Sin parametros el dashboard mira solo el mes actual, asi que agrega una fraccion
        // de los movimientos de la cuenta. Es el uso normal, pero como numero suelto seria
        // enganoso: se mide tambien un rango que cubre todo lo sembrado, que es el peor
        // caso real para el tamano de cuenta que pide el AC.
        var dashboard = await MedirEscenario(
            "GET /dashboard (mes)",
            () => cliente.GetAsync("/dashboard"));

        var desde = DateOnly.FromDateTime(DateTime.Now).AddYears(-3).ToString("yyyy-MM-dd");
        var hasta = DateOnly.FromDateTime(DateTime.Now).AddDays(1).ToString("yyyy-MM-dd");

        var dashboardCompleto = await MedirEscenario(
            "GET /dashboard (todo)",
            () => cliente.GetAsync($"/dashboard?desde={desde}&hasta={hasta}"));

        var alta = await MedirEscenario(
            "POST /movimientos",
            () => cliente.PostAsJsonAsync("/movimientos", new
            {
                monto = 1234.56m,
                categoriaId,
                fecha = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd"),
            }));

        Console.WriteLine();
        Console.WriteLine("Resultados (p95 sobre 100 ejecuciones, calentamiento descartado):");
        dashboard.Escribir();
        dashboardCompleto.Escribir();
        alta.Escribir();

        return 0;
    }

    private static async Task IniciarSesion(HttpClient cliente)
    {
        var respuesta = await cliente.PostAsJsonAsync("/auth/login", new
        {
            email = CuentaDeCarga.Email,
            contrasena = CuentaDeCarga.Contrasena,
        });

        if (!respuesta.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"No se pudo entrar como {CuentaDeCarga.Email} ({(int)respuesta.StatusCode}). " +
                "Corre primero `sembrar`.");
        }
    }

    /// <summary>
    /// La categoria para el alta sale de la API y no de una constante: si el catalogo
    /// cambia, la medicion sigue funcionando en vez de fallar con un 400.
    /// </summary>
    private static async Task<Guid> PrimeraCategoriaDeGasto(HttpClient cliente)
    {
        var categorias = await cliente.GetFromJsonAsync<List<CategoriaMinima>>("/categorias?tipo=Gasto")
            ?? throw new InvalidOperationException("No se pudo leer el catalogo de categorias.");

        return categorias[0].Id;
    }

    private static async Task<Resultado> MedirEscenario(
        string nombre, Func<Task<HttpResponseMessage>> pedido)
    {
        Console.WriteLine($"Midiendo {nombre} ({Calentamiento} de calentamiento + {Ejecuciones})...");

        for (var i = 0; i < Calentamiento; i++)
        {
            (await pedido()).Dispose();
        }

        var tiempos = new List<double>(Ejecuciones);

        for (var i = 0; i < Ejecuciones; i++)
        {
            var reloj = Stopwatch.StartNew();
            using var respuesta = await pedido();
            // El cuerpo se lee dentro de la medicion: el AC habla del tiempo hasta ver los
            // datos, no hasta recibir los headers.
            await respuesta.Content.ReadAsStringAsync();
            reloj.Stop();

            if (!respuesta.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"{nombre} devolvio {(int)respuesta.StatusCode} en la ejecucion {i + 1}. " +
                    "La medicion no vale si los pedidos fallan.");
            }

            tiempos.Add(reloj.Elapsed.TotalMilliseconds);
        }

        return new Resultado(nombre, tiempos);
    }

    private sealed record CategoriaMinima(Guid Id);

    private sealed class Resultado(string nombre, List<double> tiempos)
    {
        public void Escribir()
        {
            var ordenados = tiempos.Order().ToList();

            Console.WriteLine(
                $"  {nombre,-24} p95 {Percentil(ordenados, 0.95),8:0.0} ms   " +
                $"(mediana {Percentil(ordenados, 0.50),7:0.0}   " +
                $"min {ordenados[0],7:0.0}   max {ordenados[^1],8:0.0})");
        }

        /// <summary>
        /// El p95 se calcula sobre los tiempos, no promediando: promediar esconde
        /// justamente la cola que el RNF quiere acotar.
        /// </summary>
        private static double Percentil(List<double> ordenados, double percentil)
        {
            var indice = (int)Math.Ceiling(percentil * ordenados.Count) - 1;
            return ordenados[Math.Clamp(indice, 0, ordenados.Count - 1)];
        }
    }
}
