import { useCallback, useEffect, useState } from 'react'
import {
  api,
  ErrorApi,
  type Categoria,
  type FiltrosMovimientos,
  type Moneda,
  type Movimiento,
} from '../api/cliente'
import { useAutenticacion } from '../auth/contexto'
import { PanelCategorias } from '../categorias/PanelCategorias'
import { FormularioMovimiento } from '../movimientos/FormularioMovimiento'
import { ListadoMovimientos } from '../movimientos/ListadoMovimientos'
import { mesActual } from '../utiles/fechas'

/** Interior de la aplicacion: carga de movimientos, listado con filtros y ABM de categorias. */
export function PantallaPrincipal() {
  const { usuario, cerrarSesion } = useAutenticacion()

  const [categorias, setCategorias] = useState<Categoria[]>([])
  const [monedas, setMonedas] = useState<Moneda[]>([])
  const [movimientos, setMovimientos] = useState<Movimiento[]>([])
  // RF-18: el periodo por defecto es el mes actual. RF-17 y RF-28: sin filtrar por
  // categoria ni por moneda.
  const [filtros, setFiltros] = useState<FiltrosMovimientos>(() => ({
    ...mesActual(),
    categoriaId: '',
    moneda: '',
  }))
  const [enEdicion, setEnEdicion] = useState<Movimiento | null>(null)
  const [error, setError] = useState<string | null>(null)

  const cargarCategorias = useCallback(async () => {
    setCategorias(await api.categorias.listar())
  }, [])

  // El catálogo de monedas se pide una vez: no cambia mientras dura la sesión.
  const cargarMonedas = useCallback(async () => {
    setMonedas(await api.monedas.listar())
  }, [])

  const cargarMovimientos = useCallback(async () => {
    setMovimientos(await api.movimientos.listar(filtros))
  }, [filtros])

  useEffect(() => {
    cargarCategorias().catch(() => setError('No se pudieron cargar las categorías.'))
  }, [cargarCategorias])

  useEffect(() => {
    cargarMonedas().catch(() => setError('No se pudieron cargar las monedas.'))
  }, [cargarMonedas])

  useEffect(() => {
    cargarMovimientos().catch(() => setError('No se pudieron cargar los movimientos.'))
  }, [cargarMovimientos])

  async function despuesDeGuardar() {
    setEnEdicion(null)
    setError(null)
    await cargarMovimientos()
  }

  async function eliminar(movimiento: Movimiento) {
    setError(null)
    try {
      await api.movimientos.eliminar(movimiento.id)
      // Si se estaba editando justo ese, el formulario vuelve a modo alta.
      setEnEdicion((actual) => (actual?.id === movimiento.id ? null : actual))
      await cargarMovimientos()
    } catch (fallo: unknown) {
      setError(fallo instanceof ErrorApi ? fallo.message : 'No se pudo eliminar el movimiento.')
    }
  }

  /**
   * Al tocar categorias hay que recargar las dos cosas: el selector y el listado, que
   * muestra el nombre de la categoria de cada movimiento (AC-13).
   */
  async function despuesDeTocarCategorias() {
    await cargarCategorias()
    await cargarMovimientos()
  }

  return (
    <div className="pantalla-principal">
      <header>
        <h1>Gestion de Gastos</h1>
        <div className="sesion">
          <span>{usuario?.email}</span>
          <button type="button" onClick={() => void cerrarSesion()}>
            Cerrar sesion
          </button>
        </div>
      </header>

      <main>
        {error && (
          <p className="error" role="alert">
            {error}
          </p>
        )}

        <FormularioMovimiento
          categorias={categorias}
          monedas={monedas}
          enEdicion={enEdicion}
          onGuardado={() => void despuesDeGuardar()}
          onCancelar={() => setEnEdicion(null)}
        />

        <PanelCategorias categorias={categorias} onCambio={despuesDeTocarCategorias} />

        <ListadoMovimientos
          movimientos={movimientos}
          categorias={categorias}
          monedas={monedas}
          filtros={filtros}
          onCambiarFiltros={setFiltros}
          onEditar={setEnEdicion}
          onEliminar={(movimiento) => void eliminar(movimiento)}
        />
      </main>
    </div>
  )
}
