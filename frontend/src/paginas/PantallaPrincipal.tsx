import { useCallback, useEffect, useState } from 'react'
import {
  api,
  ErrorApi,
  type Categoria,
  type Dashboard,
  type FiltrosDashboard,
  type FiltrosMovimientos,
  type Moneda,
  type Movimiento,
} from '../api/cliente'
import { useAutenticacion } from '../auth/contexto'
import { PanelCategorias } from '../categorias/PanelCategorias'
import { PanelDashboard } from '../dashboard/PanelDashboard'
import { ResumenDelMes } from '../dashboard/ResumenDelMes'
import { FormularioMovimiento } from '../movimientos/FormularioMovimiento'
import { ListadoMovimientos } from '../movimientos/ListadoMovimientos'
import { Modal } from '../ui/Modal'
import { Pestanas } from '../ui/Pestanas'
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
  const [dashboard, setDashboard] = useState<Dashboard | null>(null)
  const [resumenDelMes, setResumenDelMes] = useState<Dashboard | null>(null)
  // RF-21 y RF-30: el dashboard tiene su propio filtro, que arranca en el mes actual.
  const [filtrosDashboard, setFiltrosDashboard] = useState<FiltrosDashboard>(() => ({
    ...mesActual(),
    moneda: '',
  }))
  const [enEdicion, setEnEdicion] = useState<Movimiento | null>(null)
  const [pestana, setPestana] = useState('movimientos')
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

  const cargarDashboard = useCallback(async () => {
    setDashboard(await api.dashboard.obtener(filtrosDashboard))
  }, [filtrosDashboard])

  // RF-22 / AC-30: el resumen es el mismo endpoint pedido con el mes actual, sin importar
  // qué período esté mirando el dashboard.
  const cargarResumenDelMes = useCallback(async () => {
    setResumenDelMes(await api.dashboard.obtener({ ...mesActual(), moneda: '' }))
  }, [])

  useEffect(() => {
    cargarCategorias().catch(() => setError('No se pudieron cargar las categorías.'))
  }, [cargarCategorias])

  useEffect(() => {
    cargarMonedas().catch(() => setError('No se pudieron cargar las monedas.'))
  }, [cargarMonedas])

  useEffect(() => {
    cargarMovimientos().catch(() => setError('No se pudieron cargar los movimientos.'))
  }, [cargarMovimientos])

  useEffect(() => {
    cargarDashboard().catch(() => setError('No se pudo cargar el dashboard.'))
  }, [cargarDashboard])

  useEffect(() => {
    cargarResumenDelMes().catch(() => setError('No se pudo cargar el resumen del mes.'))
  }, [cargarResumenDelMes])

  /** Todo lo que toca movimientos mueve también los totales: se recargan juntos. */
  async function recargarTodo() {
    await Promise.all([cargarMovimientos(), cargarDashboard(), cargarResumenDelMes()])
  }

  async function despuesDeGuardar() {
    setEnEdicion(null)
    setError(null)
    await recargarTodo()
  }

  async function eliminar(movimiento: Movimiento) {
    setError(null)
    try {
      await api.movimientos.eliminar(movimiento.id)
      // Si se estaba editando justo ese, el formulario vuelve a modo alta.
      setEnEdicion((actual) => (actual?.id === movimiento.id ? null : actual))
      await recargarTodo()
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
    await recargarTodo()
  }

  const formulario = (
    <FormularioMovimiento
      categorias={categorias}
      monedas={monedas}
      enEdicion={enEdicion}
      onGuardado={() => void despuesDeGuardar()}
      onCancelar={() => setEnEdicion(null)}
    />
  )

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

        <Pestanas
          activa={pestana}
          onCambiar={setPestana}
          pestanas={[
            {
              id: 'movimientos',
              etiqueta: 'Movimientos',
              contenido: (
                <>
                  <ResumenDelMes resumen={resumenDelMes} monedas={monedas} />

                  {/* Al editar, el formulario se muda al modal: hay uno solo en pantalla,
                      asi no quedan dos campos "Monto" compitiendo por el mismo rotulo. */}
                  {!enEdicion && formulario}

                  <ListadoMovimientos
                    movimientos={movimientos}
                    categorias={categorias}
                    monedas={monedas}
                    filtros={filtros}
                    onCambiarFiltros={setFiltros}
                    onEditar={setEnEdicion}
                    onEliminar={(movimiento) => void eliminar(movimiento)}
                  />
                </>
              ),
            },
            {
              id: 'dashboard',
              etiqueta: 'Dashboard',
              contenido: (
                <PanelDashboard
                  dashboard={dashboard}
                  monedas={monedas}
                  filtros={filtrosDashboard}
                  onCambiarFiltros={setFiltrosDashboard}
                />
              ),
            },
            {
              id: 'categorias',
              etiqueta: 'Categorías',
              contenido: (
                <PanelCategorias categorias={categorias} onCambio={despuesDeTocarCategorias} />
              ),
            },
          ]}
        />
      </main>

      {enEdicion && (
        <Modal titulo="Editar movimiento" onCerrar={() => setEnEdicion(null)}>
          {formulario}
        </Modal>
      )}
    </div>
  )
}
