using GestionGastos.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("MySql")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexion 'ConnectionStrings:MySql'. " +
        "Configurala con user-secrets (ver appsettings.Development.example.json).");

// Version fijada a proposito en vez de ServerVersion.AutoDetect: AutoDetect abre una
// conexion al construir el DbContext, con lo que una base caida rompe el arranque en
// lugar de dejar que /health/db responda 503.
var versionServidor = new MySqlServerVersion(new Version(8, 4, 5));

builder.Services.AddDbContext<GestionGastosDbContext>(options =>
    options.UseMySql(connectionString, versionServidor));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Smoke check que no toca la base.
app.MapGet("/health", () => Results.Ok(new { estado = "ok" }));

// Smoke check que verifica que MySQL este accesible.
app.MapGet("/health/db", async (GestionGastosDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { estado = "ok", baseDeDatos = "conectada" })
        : Results.Problem("No se pudo conectar a la base de datos.", statusCode: 503));

app.Run();

// Expuesto para que los tests de integracion puedan referenciar el host.
public partial class Program;
