import type { Dashboard, FiltrosDashboard, Moneda, ResumenDeMoneda } from '../api/cliente'
import { comoMonto } from '../utiles/fechas'
import { GraficoGastos } from './GraficoGastos'

type Props = {
  dashboard: Dashboard | null
  monedas: Moneda[]
  filtros: FiltrosDashboard
  onCambiarFiltros: (filtros: FiltrosDashboard) => void
}

/** RF-19 a RF-21 y RF-30: totales por categoría y balance, por moneda y por período. */
export function PanelDashboard({ dashboard, monedas, filtros, onCambiarFiltros }: Props) {
  function cambiar(campo: keyof FiltrosDashboard, valor: string) {
    onCambiarFiltros({ ...filtros, [campo]: valor })
  }

  return (
    <section className="dashboard">
      <h2>Dashboard</h2>

      <div className="filtros">
        <div className="campo">
          <label htmlFor="dashboard-desde">Desde (dashboard)</label>
          <input
            id="dashboard-desde"
            type="date"
            value={filtros.desde}
            onChange={(evento) => cambiar('desde', evento.target.value)}
          />
        </div>
        <div className="campo">
          <label htmlFor="dashboard-hasta">Hasta (dashboard)</label>
          <input
            id="dashboard-hasta"
            type="date"
            value={filtros.hasta}
            onChange={(evento) => cambiar('hasta', evento.target.value)}
          />
        </div>
        <div className="campo">
          <label htmlFor="dashboard-moneda">Moneda (dashboard)</label>
          <select
            id="dashboard-moneda"
            value={filtros.moneda}
            onChange={(evento) => cambiar('moneda', evento.target.value)}
          >
            {/* RF-30: el default es "todas". */}
            <option value="">Todas</option>
            {monedas.map((moneda) => (
              <option key={moneda.codigo} value={moneda.codigo}>
                {moneda.nombre}
              </option>
            ))}
          </select>
        </div>
      </div>

      {dashboard?.monedas.map((resumen) => (
        <BloqueDeMoneda key={resumen.moneda} resumen={resumen} catalogo={monedas} />
      ))}
    </section>
  )
}

/**
 * Un bloque cerrado por moneda. La separación es visual además de estructural para que
 * no dé la impresión de que los números se pueden sumar entre bloques (RF-29).
 */
function BloqueDeMoneda({
  resumen,
  catalogo,
}: {
  resumen: ResumenDeMoneda
  catalogo: Moneda[]
}) {
  const nombre = catalogo.find((m) => m.codigo === resumen.moneda)?.nombre ?? resumen.moneda

  return (
    <article className="moneda" aria-label={`Dashboard en ${nombre}`}>
      <h3>{nombre}</h3>

      <div className="totales">
        <Total etiqueta="Ingresos" monto={resumen.totalIngresos} resumen={resumen} catalogo={catalogo} clase="ingreso" />
        <Total etiqueta="Gastos" monto={resumen.totalGastos} resumen={resumen} catalogo={catalogo} clase="gasto" />
        <Total
          etiqueta="Balance"
          monto={resumen.balance}
          resumen={resumen}
          catalogo={catalogo}
          clase={resumen.balance < 0 ? 'gasto' : 'ingreso'}
        />
      </div>

      <h4>Gastos por categor&iacute;a</h4>
      <GraficoGastos
        gastos={resumen.gastosPorCategoria}
        moneda={resumen.moneda}
        catalogo={catalogo}
      />
    </article>
  )
}

function Total({
  etiqueta,
  monto,
  resumen,
  catalogo,
  clase,
}: {
  etiqueta: string
  monto: number
  resumen: ResumenDeMoneda
  catalogo: Moneda[]
  clase: string
}) {
  return (
    <div className="total">
      <span className="etiqueta">{etiqueta}</span>
      <strong className={clase}>{comoMonto(monto, resumen.moneda, catalogo)}</strong>
    </div>
  )
}
