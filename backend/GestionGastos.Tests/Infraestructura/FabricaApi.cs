using GestionGastos.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GestionGastos.Tests.Infraestructura;

/// <summary>
/// Levanta la API en memoria contra SQLite en vez de MySQL, para que los tests corran
/// sin depender de una base local. Se usa SQLite y no el proveedor InMemory porque este
/// ultimo ignora los indices unicos, que son justamente parte de lo que se verifica (AC-02).
/// </summary>
public class FabricaApi : WebApplicationFactory<Program>
{
    // Una conexion abierta durante toda la vida de la fabrica: la base ":memory:" de SQLite
    // se borra cuando se cierra la ultima conexion.
    private readonly SqliteConnection _conexion = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Program.cs exige una cadena de conexion para arrancar; en los tests se reemplaza
        // el proveedor mas abajo, asi que este valor nunca se usa para conectarse.
        builder.UseSetting("ConnectionStrings:MySql", "Server=127.0.0.1;Database=no-se-usa");

        builder.ConfigureServices(servicios =>
        {
            var registrosDelContexto = servicios
                .Where(d => d.ServiceType.FullName?.Contains(nameof(GestionGastosDbContext), StringComparison.Ordinal) == true)
                .ToList();

            foreach (var registro in registrosDelContexto)
            {
                servicios.Remove(registro);
            }

            _conexion.Open();
            servicios.AddDbContext<GestionGastosDbContext>(opciones => opciones.UseSqlite(_conexion));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var alcance = host.Services.CreateScope();
        alcance.ServiceProvider.GetRequiredService<GestionGastosDbContext>().Database.EnsureCreated();

        return host;
    }

    /// <summary>Ejecuta algo contra la base de la API (para verificar lo que quedo guardado).</summary>
    public async Task<T> ConsultarBase<T>(Func<GestionGastosDbContext, Task<T>> consulta)
    {
        using var alcance = Services.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<GestionGastosDbContext>();
        return await consulta(db);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _conexion.Dispose();
        }
    }
}
