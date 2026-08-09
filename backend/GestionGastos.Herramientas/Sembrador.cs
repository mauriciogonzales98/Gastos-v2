using GestionGastos.Api.Autenticacion;
using GestionGastos.Api.Data;
using GestionGastos.Api.Entidades;
using Microsoft.EntityFrameworkCore;

namespace GestionGastos.Herramientas;

/// <summary>
/// F.1 del paso final: deja la cuenta de carga con exactamente N movimientos.
///
/// Inserta por EF y no por HTTP a proposito. Diez mil POST tardan una eternidad y ademas
/// medirian otra cosa (el costo de la API), cuando lo que hace falta es solo el dato para
/// que despues se mida el dashboard.
/// </summary>
public static class Sembrador
{
    /// <summary>
    /// Tamano de lote. Un unico SaveChanges con 10000 entidades hace que el
    /// change tracker se vuelva el cuello de botella; en lotes se mantiene plano.
    /// </summary>
    private const int TamanoDeLote = 500;

    public static async Task Sembrar(GestionGastosDbContext db, int cantidad)
    {
        var usuario = await AsegurarUsuario(db);
        await BorrarMovimientos(db, usuario.Id);

        var categorias = await db.Categorias
            .Where(c => c.EsDelSistema)
            .OrderBy(c => c.Id)
            .ToListAsync();

        var monedas = await db.Monedas.OrderBy(m => m.Orden).ToListAsync();

        if (categorias.Count == 0 || monedas.Count == 0)
        {
            throw new InvalidOperationException(
                "Faltan las categorias del sistema o el catalogo de monedas. " +
                "Corre `dotnet ef database update` antes de sembrar.");
        }

        // Semilla fija: dos corridas con la misma cantidad producen los mismos datos, asi
        // que una medicion se puede repetir y comparar contra la anterior.
        var azar = new Random(20260809);
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var creacion = DateTime.UtcNow;

        // Con el tracker prendido, cada Add recorre lo ya agregado y el alta se vuelve
        // cuadratica. Sembrar 10000 filas pasa de minutos a segundos.
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            for (var insertados = 0; insertados < cantidad; insertados += TamanoDeLote)
            {
                var lote = Math.Min(TamanoDeLote, cantidad - insertados);

                for (var i = 0; i < lote; i++)
                {
                    var categoria = categorias[azar.Next(categorias.Count)];
                    var moneda = monedas[azar.Next(monedas.Count)];

                    db.Movimientos.Add(new Movimiento
                    {
                        Id = Guid.NewGuid(),
                        UsuarioId = usuario.Id,
                        CategoriaId = categoria.Id,
                        // Montos con dos decimales, del orden de un gasto real.
                        Monto = Math.Round((decimal)(azar.NextDouble() * 50000 + 100), 2),
                        MonedaCodigo = moneda.Codigo,
                        // Repartidos en dos anios hacia atras: el dashboard filtra por
                        // rango, asi que todo en el mismo mes no ejercitaria el indice.
                        Fecha = hoy.AddDays(-azar.Next(730)),
                        Descripcion = null,
                        FechaCreacionUtc = creacion,
                    });
                }

                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();

                Console.WriteLine($"  sembrados {insertados + lote}/{cantidad}");
            }
        }
        finally
        {
            db.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        var total = await db.Movimientos.CountAsync(m => m.UsuarioId == usuario.Id);
        Console.WriteLine($"Listo: la cuenta {CuentaDeCarga.Email} tiene {total} movimientos.");
    }

    /// <summary>
    /// Saca la cuenta de carga y todo lo suyo. El plan del paso final pide borrarla al
    /// terminar para no dejar 10000 movimientos ensuciando la base de desarrollo.
    ///
    /// Lo hace la misma herramienta que sembro, y no `003-borrar-usuarios-de-prueba.sql`,
    /// porque aca la cadena de conexion ya esta a mano: no hay que pedir la contrasena de
    /// MySQL de nuevo. El script sigue existiendo para lo que se creo a mano.
    /// </summary>
    public static async Task Limpiar(GestionGastosDbContext db)
    {
        var email = Usuario.NormalizarEmail(CuentaDeCarga.Email);
        var usuario = await db.Usuarios.SingleOrDefaultAsync(u => u.Email == email);

        if (usuario is null)
        {
            Console.WriteLine($"No hay nada que limpiar: {email} no existe.");
            return;
        }

        // Mismo orden explicito que el script SQL: Movimiento -> Categoria es Restrict, y
        // dejar que casquee desde Usuarios puede chocar contra esa restriccion.
        var movimientos = await db.Movimientos
            .Where(m => m.UsuarioId == usuario.Id)
            .ExecuteDeleteAsync();

        var categorias = await db.Categorias
            .Where(c => c.UsuarioId == usuario.Id)
            .ExecuteDeleteAsync();

        db.Usuarios.Remove(usuario);
        await db.SaveChangesAsync();

        Console.WriteLine(
            $"Borrada la cuenta {email}: {movimientos} movimientos y {categorias} categorias propias.");
    }

    /// <summary>Deja la cuenta de carga creada, o la reusa si ya existe.</summary>
    private static async Task<Usuario> AsegurarUsuario(GestionGastosDbContext db)
    {
        var email = Usuario.NormalizarEmail(CuentaDeCarga.Email);
        var existente = await db.Usuarios.SingleOrDefaultAsync(u => u.Email == email);

        if (existente is not null)
        {
            return existente;
        }

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Email = email,
            // El mismo hasheo que usa la API (RNF-03): la cuenta tiene que poder loguearse
            // por /auth/login como cualquier otra, o la medicion no valdria.
            HashContrasena = new ServicioContrasenasBCrypt().Hashear(CuentaDeCarga.Contrasena),
            FechaCreacionUtc = DateTime.UtcNow,
        };

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        Console.WriteLine($"Cuenta de carga creada: {email}");
        return usuario;
    }

    /// <summary>
    /// Borra los movimientos previos para que sembrar sea idempotente: sembrar 1000 y
    /// despues 10000 tiene que dejar 10000, no 11000.
    /// </summary>
    private static async Task BorrarMovimientos(GestionGastosDbContext db, Guid usuarioId)
    {
        var borrados = await db.Movimientos
            .Where(m => m.UsuarioId == usuarioId)
            .ExecuteDeleteAsync();

        if (borrados > 0)
        {
            Console.WriteLine($"Borrados {borrados} movimientos de la corrida anterior.");
        }
    }
}
