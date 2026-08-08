import type { Dashboard, Moneda } from '../api/cliente'
import { comoMonto } from '../utiles/fechas'

type Props = {
  resumen: Dashboard | null
  monedas: Moneda[]
}

const NOMBRES_DE_MES = [
  'enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio',
  'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre',
]

/**
 * RF-22: lo ingresado y lo gastado en el mes actual, discriminado por moneda.
 *
 * Sale del mismo endpoint que el dashboard, pedido con el mes actual. Por construcción no
 * puede diferir de lo que muestra el dashboard filtrado por este mes (AC-30): si se
 * calculara aparte, el día que cambie una regla de agregación quedarían en desacuerdo.
 */
export function ResumenDelMes({ resumen, monedas }: Props) {
  const mes = NOMBRES_DE_MES[new Date().getMonth()]

  return (
    <section className="resumen-mes">
      <h2>Resumen de {mes}</h2>

      <div className="monedas">
        {resumen?.monedas.map((porMoneda) => {
          const nombre =
            monedas.find((m) => m.codigo === porMoneda.moneda)?.nombre ?? porMoneda.moneda

          return (
            // <section> y no <div>: con aria-label queda expuesta como región y el
            // bloque de cada moneda es navegable por separado.
            <section className="moneda" key={porMoneda.moneda} aria-label={`Resumen en ${nombre}`}>
              <span className="nombre">{nombre}</span>
              <div className="cifras">
                <span>
                  Ingresado{' '}
                  <strong className="ingreso">
                    {comoMonto(porMoneda.totalIngresos, porMoneda.moneda, monedas)}
                  </strong>
                </span>
                <span>
                  Gastado{' '}
                  <strong className="gasto">
                    {comoMonto(porMoneda.totalGastos, porMoneda.moneda, monedas)}
                  </strong>
                </span>
              </div>
            </section>
          )
        })}
      </div>
    </section>
  )
}
