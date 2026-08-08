import type { Categoria, FiltrosMovimientos, Movimiento } from '../api/cliente'
import { comoMonto, comoTexto } from '../utiles/fechas'

type Props = {
  movimientos: Movimiento[]
  categorias: Categoria[]
  filtros: FiltrosMovimientos
  onCambiarFiltros: (filtros: FiltrosMovimientos) => void
  onEditar: (movimiento: Movimiento) => void
  onEliminar: (movimiento: Movimiento) => void
}

/** RF-16 a RF-18: listado con filtros por categoria y por rango de fechas. */
export function ListadoMovimientos({
  movimientos,
  categorias,
  filtros,
  onCambiarFiltros,
  onEditar,
  onEliminar,
}: Props) {
  function cambiar(campo: keyof FiltrosMovimientos, valor: string) {
    onCambiarFiltros({ ...filtros, [campo]: valor })
  }

  return (
    <section className="listado">
      <h2>Movimientos</h2>

      <div className="filtros">
        <div className="campo">
          <label htmlFor="filtro-desde">Desde</label>
          <input
            id="filtro-desde"
            type="date"
            value={filtros.desde}
            onChange={(evento) => cambiar('desde', evento.target.value)}
          />
        </div>
        <div className="campo">
          <label htmlFor="filtro-hasta">Hasta</label>
          <input
            id="filtro-hasta"
            type="date"
            value={filtros.hasta}
            onChange={(evento) => cambiar('hasta', evento.target.value)}
          />
        </div>
        <div className="campo">
          <label htmlFor="filtro-categoria">Filtrar por categor&iacute;a</label>
          <select
            id="filtro-categoria"
            value={filtros.categoriaId}
            onChange={(evento) => cambiar('categoriaId', evento.target.value)}
          >
            {/* RF-17: el default es "todas". */}
            <option value="">Todas</option>
            {categorias.map((categoria) => (
              <option key={categoria.id} value={categoria.id}>
                {categoria.nombre} ({categoria.tipo === 'Gasto' ? 'gasto' : 'ingreso'})
              </option>
            ))}
          </select>
        </div>
      </div>

      {movimientos.length === 0 ? (
        <p className="vacio">No hay movimientos en este per&iacute;odo.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Categor&iacute;a</th>
              <th className="numero">Monto</th>
              <th>
                <span className="oculto-visualmente">Acciones</span>
              </th>
            </tr>
          </thead>
          <tbody>
            {movimientos.map((movimiento) => (
              <tr key={movimiento.id}>
                <td>{comoTexto(movimiento.fecha)}</td>
                <td>
                  {movimiento.categoriaNombre}
                  <span className={`etiqueta ${movimiento.tipo.toLowerCase()}`}>
                    {movimiento.tipo === 'Gasto' ? 'gasto' : 'ingreso'}
                  </span>
                </td>
                <td className={`numero ${movimiento.tipo.toLowerCase()}`}>
                  {movimiento.tipo === 'Gasto' ? '-' : '+'} {comoMonto(movimiento.monto)}
                </td>
                <td className="acciones">
                  <button type="button" onClick={() => onEditar(movimiento)}>
                    Editar
                  </button>
                  <button type="button" onClick={() => onEliminar(movimiento)}>
                    Eliminar
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  )
}
