using GestionGastos.Api.Entidades;

namespace GestionGastos.Api.Categorias;

public record CategoriaResponse(Guid Id, string Nombre, TipoCategoria Tipo, bool EsDelSistema)
{
    public static CategoriaResponse De(Categoria categoria) =>
        new(categoria.Id, categoria.Nombre, categoria.Tipo, categoria.EsDelSistema);
}

public record CrearCategoriaRequest(string? Nombre, TipoCategoria? Tipo);

public record RenombrarCategoriaRequest(string? Nombre);
