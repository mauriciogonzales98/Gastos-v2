import { useState, type FormEvent } from 'react'
import { api, ErrorApi, type Categoria, type TipoCategoria } from '../api/cliente'

type Props = {
  categorias: Categoria[]
  onCambio: () => Promise<void> | void
}

/** RF-07 a RF-09: alta, renombrado y baja de categorias propias. */
export function PanelCategorias({ categorias, onCambio }: Props) {
  const [abierto, setAbierto] = useState(false)
  const [nombre, setNombre] = useState('')
  const [tipo, setTipo] = useState<TipoCategoria>('Gasto')
  const [editandoId, setEditandoId] = useState<string | null>(null)
  const [nombreEditado, setNombreEditado] = useState('')
  const [error, setError] = useState<string | null>(null)

  const propias = categorias.filter((categoria) => !categoria.esDelSistema)

  async function ejecutar(accion: () => Promise<unknown>) {
    setError(null)
    try {
      await accion()
      await onCambio()
    } catch (fallo: unknown) {
      setError(fallo instanceof ErrorApi ? fallo.message : 'No se pudo completar la operación.')
    }
  }

  async function crear(evento: FormEvent) {
    evento.preventDefault()
    if (!nombre.trim()) {
      setError('El nombre es obligatorio.')
      return
    }

    await ejecutar(async () => {
      await api.categorias.crear(nombre.trim(), tipo)
      setNombre('')
    })
  }

  async function confirmarRenombrado(id: string) {
    await ejecutar(async () => {
      await api.categorias.renombrar(id, nombreEditado.trim())
      setEditandoId(null)
    })
  }

  // Sin confirmacion: la baja es logica (RF-09), los movimientos ya cargados conservan
  // el nombre y siguen sumando. No se pierde nada.
  async function eliminar(id: string) {
    await ejecutar(() => api.categorias.eliminar(id))
  }

  return (
    <section className="panel-categorias">
      <button
        type="button"
        className="desplegar"
        aria-expanded={abierto}
        onClick={() => setAbierto(!abierto)}
      >
        Mis categor&iacute;as ({propias.length})
      </button>

      {abierto && (
        <div className="contenido">
          <form onSubmit={crear}>
            <div className="campo">
              <label htmlFor="categoria-nombre">Nombre</label>
              <input
                id="categoria-nombre"
                value={nombre}
                onChange={(evento) => setNombre(evento.target.value)}
              />
            </div>
            <div className="campo">
              <label htmlFor="categoria-tipo">Tipo</label>
              <select
                id="categoria-tipo"
                value={tipo}
                onChange={(evento) => setTipo(evento.target.value as TipoCategoria)}
              >
                <option value="Gasto">Gasto</option>
                <option value="Ingreso">Ingreso</option>
              </select>
            </div>
            <button type="submit" className="principal">
              Crear categor&iacute;a
            </button>
          </form>

          {error && (
            <p className="error" role="alert">
              {error}
            </p>
          )}

          {propias.length === 0 ? (
            <p className="vacio">
              Todav&iacute;a no creaste categor&iacute;as propias. Las predefinidas no se pueden
              modificar.
            </p>
          ) : (
            <ul>
              {propias.map((categoria) => (
                <li key={categoria.id}>
                  {editandoId === categoria.id ? (
                    <>
                      <input
                        aria-label={`Nuevo nombre de ${categoria.nombre}`}
                        value={nombreEditado}
                        onChange={(evento) => setNombreEditado(evento.target.value)}
                      />
                      <button type="button" onClick={() => void confirmarRenombrado(categoria.id)}>
                        Guardar
                      </button>
                      <button type="button" onClick={() => setEditandoId(null)}>
                        Cancelar
                      </button>
                    </>
                  ) : (
                    <>
                      <span>
                        {categoria.nombre}
                        <span className={`etiqueta ${categoria.tipo.toLowerCase()}`}>
                          {categoria.tipo === 'Gasto' ? 'gasto' : 'ingreso'}
                        </span>
                      </span>
                      <button
                        type="button"
                        onClick={() => {
                          setEditandoId(categoria.id)
                          setNombreEditado(categoria.nombre)
                          setError(null)
                        }}
                      >
                        Renombrar {categoria.nombre}
                      </button>
                      <button type="button" onClick={() => void eliminar(categoria.id)}>
                        Eliminar {categoria.nombre}
                      </button>
                    </>
                  )}
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </section>
  )
}
