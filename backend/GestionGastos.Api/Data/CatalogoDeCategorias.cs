using GestionGastos.Api.Entidades;

namespace GestionGastos.Api.Data;

/// <summary>
/// Catalogo de categorias predefinidas (RF-06). Los Guid van fijos y escritos a mano
/// porque se siembran desde una migracion: si cambiaran en cada build, cada `dotnet ef
/// migrations add` generaria un borrado y un alta de todas las filas.
/// </summary>
public static class CatalogoDeCategorias
{
    public static readonly Categoria[] Predefinidas =
    [
        Predefinida("a1000000-0000-4000-8000-000000000001", "Comida", TipoCategoria.Gasto),
        Predefinida("a1000000-0000-4000-8000-000000000002", "Transporte", TipoCategoria.Gasto),
        Predefinida("a1000000-0000-4000-8000-000000000003", "Vivienda", TipoCategoria.Gasto),
        Predefinida("a1000000-0000-4000-8000-000000000004", "Servicios", TipoCategoria.Gasto),
        Predefinida("a1000000-0000-4000-8000-000000000005", "Salud", TipoCategoria.Gasto),
        Predefinida("a1000000-0000-4000-8000-000000000006", "Ocio", TipoCategoria.Gasto),
        Predefinida("a1000000-0000-4000-8000-000000000007", "Otros", TipoCategoria.Gasto),
        Predefinida("b2000000-0000-4000-8000-000000000001", "Sueldo", TipoCategoria.Ingreso),
        Predefinida("b2000000-0000-4000-8000-000000000002", "Ingreso extra", TipoCategoria.Ingreso),
        Predefinida("b2000000-0000-4000-8000-000000000003", "Otros", TipoCategoria.Ingreso),
    ];

    private static Categoria Predefinida(string id, string nombre, TipoCategoria tipo) =>
        new()
        {
            Id = Guid.Parse(id),
            Nombre = nombre,
            Tipo = tipo,
            UsuarioId = null,
            EsDelSistema = true,
        };
}
