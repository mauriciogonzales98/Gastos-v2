using Microsoft.EntityFrameworkCore;

namespace GestionGastos.Api.Data;

/// <summary>
/// Contexto de EF Core. Por ahora no declara entidades: se agregan a partir de la
/// feature 1 (usuarios) y la feature 2 (categorias y movimientos).
/// </summary>
public class GestionGastosDbContext(DbContextOptions<GestionGastosDbContext> options)
    : DbContext(options)
{
}
