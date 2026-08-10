import { useId, useRef, type KeyboardEvent, type ReactNode } from 'react'

export type Pestana = {
  id: string
  etiqueta: string
  contenido: ReactNode
}

type Props = {
  pestanas: Pestana[]
  activa: string
  onCambiar: (id: string) => void
}

/**
 * Navegacion por pestanas del interior de la aplicacion.
 *
 * Solo se monta el panel activo: los datos viven en la pantalla que la contiene, asi que
 * cambiar de pestana no vuelve a pedir nada ni pierde los filtros.
 */
export function Pestanas({ pestanas, activa, onCambiar }: Props) {
  const prefijo = useId()
  const lista = useRef<HTMLDivElement>(null)
  const actual = pestanas.find((pestana) => pestana.id === activa) ?? pestanas[0]

  const idPestana = (id: string) => `${prefijo}-tab-${id}`
  const idPanel = (id: string) => `${prefijo}-panel-${id}`

  /**
   * Flechas, Inicio y Fin, como pide el patron de tabs: con `tabIndex` -1 en las
   * inactivas, el Tab solo entra y sale del grupo y el movimiento interno va por flechas.
   */
  function alPresionarTecla(evento: KeyboardEvent<HTMLDivElement>) {
    const posicion = pestanas.findIndex((pestana) => pestana.id === actual.id)

    const destino = {
      ArrowRight: (posicion + 1) % pestanas.length,
      ArrowLeft: (posicion - 1 + pestanas.length) % pestanas.length,
      Home: 0,
      End: pestanas.length - 1,
    }[evento.key]

    if (destino === undefined) {
      return
    }

    evento.preventDefault()
    const siguiente = pestanas[destino]
    onCambiar(siguiente.id)
    lista.current?.querySelector<HTMLButtonElement>(`#${CSS.escape(idPestana(siguiente.id))}`)?.focus()
  }

  return (
    <div className="pestanas">
      <div className="tira" role="tablist" ref={lista} onKeyDown={alPresionarTecla}>
        {pestanas.map((pestana) => {
          const seleccionada = pestana.id === actual.id

          return (
            <button
              key={pestana.id}
              type="button"
              role="tab"
              id={idPestana(pestana.id)}
              aria-selected={seleccionada}
              aria-controls={idPanel(pestana.id)}
              tabIndex={seleccionada ? 0 : -1}
              className={seleccionada ? 'activo' : ''}
              onClick={() => onCambiar(pestana.id)}
            >
              {pestana.etiqueta}
            </button>
          )
        })}
      </div>

      <div
        role="tabpanel"
        id={idPanel(actual.id)}
        aria-labelledby={idPestana(actual.id)}
        tabIndex={0}
      >
        {actual.contenido}
      </div>
    </div>
  )
}
