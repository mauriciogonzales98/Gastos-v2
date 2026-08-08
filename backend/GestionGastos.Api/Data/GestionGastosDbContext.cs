using GestionGastos.Api.Entidades;
using Microsoft.EntityFrameworkCore;

namespace GestionGastos.Api.Data;

public class GestionGastosDbContext(DbContextOptions<GestionGastosDbContext> options)
    : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Categoria> Categorias => Set<Categoria>();

    public DbSet<Movimiento> Movimientos => Set<Movimiento>();

    public DbSet<Moneda> Monedas => Set<Moneda>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(usuario =>
        {
            usuario.ToTable("Usuarios");
            usuario.HasKey(u => u.Id);

            usuario.Property(u => u.Email).HasMaxLength(254).IsRequired();
            // Indice unico: el chequeo previo en /auth/register evita la mayoria de los
            // duplicados, pero esto es lo que los descarta ante dos altas simultaneas (AC-02).
            usuario.HasIndex(u => u.Email).IsUnique();

            usuario.Property(u => u.HashContrasena).HasMaxLength(255).IsRequired();
            usuario.Property(u => u.FechaCreacionUtc).IsRequired();
        });

        modelBuilder.Entity<Categoria>(categoria =>
        {
            categoria.ToTable("Categorias");
            categoria.HasKey(c => c.Id);

            categoria.Property(c => c.Nombre).HasMaxLength(60).IsRequired();
            // El tipo se guarda como texto ("Gasto" / "Ingreso"): la tabla se puede leer a
            // mano sin tener que acordarse de que numero es cada valor del enum.
            categoria.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(10).IsRequired();
            categoria.Property(c => c.EsDelSistema).IsRequired();
            categoria.Ignore(c => c.EstaActiva);

            categoria.HasOne(c => c.Usuario)
                .WithMany()
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cubre la consulta del selector: las del usuario y las del sistema, por tipo.
            categoria.HasIndex(c => new { c.UsuarioId, c.Tipo });

            categoria.HasData(CatalogoDeCategorias.Predefinidas);
        });

        modelBuilder.Entity<Moneda>(moneda =>
        {
            moneda.ToTable("Monedas");
            // El codigo ISO es la clave: es estable, corto y unico por definicion.
            moneda.HasKey(m => m.Codigo);

            moneda.Property(m => m.Codigo).HasMaxLength(3).IsFixedLength();
            moneda.Property(m => m.Nombre).HasMaxLength(40).IsRequired();
            moneda.Property(m => m.Simbolo).HasMaxLength(8).IsRequired();
            moneda.Property(m => m.Decimales).IsRequired();
            moneda.Property(m => m.EsPredeterminada).IsRequired();
            moneda.Property(m => m.Orden).IsRequired();

            moneda.HasData(CatalogoDeMonedas.Iniciales);
        });

        modelBuilder.Entity<Movimiento>(movimiento =>
        {
            movimiento.ToTable("Movimientos");
            movimiento.HasKey(m => m.Id);

            movimiento.Property(m => m.Monto).HasPrecision(18, 2).IsRequired();
            movimiento.Property(m => m.MonedaCodigo).HasMaxLength(3).IsFixedLength().IsRequired();
            movimiento.Property(m => m.Fecha).IsRequired();
            movimiento.Property(m => m.FechaCreacionUtc).IsRequired();

            movimiento.HasOne(m => m.Usuario)
                .WithMany()
                .HasForeignKey(m => m.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict y no Cascade: las categorias se dan de baja logica justamente para
            // que los movimientos historicos conserven su nombre (AC-14).
            movimiento.HasOne(m => m.Categoria)
                .WithMany()
                .HasForeignKey(m => m.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            // La foreign key es lo que impide que un UPDATE a mano meta una moneda que no
            // existe: la validacion de la API deja de ser la unica defensa.
            movimiento.HasOne(m => m.Moneda)
                .WithMany()
                .HasForeignKey(m => m.MonedaCodigo)
                .OnDelete(DeleteBehavior.Restrict);

            // Todas las consultas de listado y dashboard filtran por usuario y rango de
            // fechas; este indice es el que sostiene el RNF-01 con 10000 movimientos.
            // La moneda va al final porque el dashboard agrupa por ella dentro del rango.
            movimiento.HasIndex(m => new { m.UsuarioId, m.Fecha, m.MonedaCodigo });
        });
    }
}
