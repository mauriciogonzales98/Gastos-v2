import type { CodigoMoneda, Moneda, TotalPorCategoria } from '../api/cliente'
import { comoMonto } from '../utiles/fechas'

type Props = {
  gastos: TotalPorCategoria[]
  moneda: CodigoMoneda
  catalogo: Moneda[]
}

/**
 * RF-19: los gastos por categoría, representados gráficamente.
 *
 * Barras horizontales de una sola serie: el dato es una magnitud comparada entre
 * categorías, no identidades que haya que distinguir por color. Por eso van todas del
 * mismo tono y no hay leyenda — el color no codifica nada, la longitud sí. Cada barra
 * lleva su nombre y su monto como etiqueta directa, así el gráfico se lee sin pasar el
 * mouse y sirve igual de tabla para un lector de pantalla.
 */
export function GraficoGastos({ gastos, moneda, catalogo }: Props) {
  if (gastos.length === 0) {
    // AC-31: se avisa que no hay datos; no es un error.
    return <p className="vacio">Sin gastos en este per&iacute;odo.</p>
  }

  // La barra más larga marca la escala; el resto se mide contra ella.
  const mayor = Math.max(...gastos.map((gasto) => gasto.total))

  return (
    <ul className="grafico">
      {gastos.map((gasto) => (
        <li key={gasto.categoriaId}>
          <div className="etiquetas">
            <span className="nombre">{gasto.categoriaNombre}</span>
            <span className="valor">{comoMonto(gasto.total, moneda, catalogo)}</span>
          </div>
          <div className="riel">
            <div
              className="barra"
              style={{ width: `${mayor > 0 ? (gasto.total / mayor) * 100 : 0}%` }}
              title={`${gasto.categoriaNombre}: ${comoMonto(gasto.total, moneda, catalogo)}`}
            />
          </div>
        </li>
      ))}
    </ul>
  )
}
