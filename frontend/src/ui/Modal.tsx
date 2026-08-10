import { useEffect, useRef, type ReactNode } from 'react'

type Props = {
  /** Nombre accesible del dialogo. El titulo visible lo pone el contenido. */
  titulo: string
  onCerrar: () => void
  children: ReactNode
}

/**
 * Ventana modal sobre la pantalla principal, con `<dialog>` nativo.
 *
 * Se usa el elemento nativo y no un div propio porque trae gratis lo que habria que
 * escribir a mano en cualquier otra version: el fondo inerte, la trampa de foco, el
 * cierre con Escape y la capa superior sin pelear con z-index.
 */
export function Modal({ titulo, onCerrar, children }: Props) {
  const dialogo = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    const elemento = dialogo.current
    if (!elemento) {
      return
    }

    // `showModal` es lo que activa el fondo inerte y la trampa de foco; poner `open` a
    // secas dejaria el dialogo visible pero con el resto de la pantalla navegable.
    elemento.showModal()

    return () => {
      if (elemento.open) {
        elemento.close()
      }
    }
  }, [])

  return (
    <dialog
      ref={dialogo}
      className="modal"
      // aria-label y no un <h2> propio: el titulo visible ya lo pone el contenido y dos
      // encabezados diciendo lo mismo solo hacen ruido en un lector de pantalla.
      aria-label={titulo}
      // El Escape del navegador cierra el dialogo sin avisarle a React: sin esto el
      // estado seguiria en modo edicion y el modal no volveria a abrirse.
      onCancel={(evento) => {
        evento.preventDefault()
        onCerrar()
      }}
      // Un click sobre el fondo llega al propio <dialog>; los de adentro, al contenido.
      onClick={(evento) => {
        if (evento.target === dialogo.current) {
          onCerrar()
        }
      }}
    >
      <div className="contenido">
        <button type="button" className="cerrar" onClick={onCerrar}>
          Cerrar
        </button>
        {children}
      </div>
    </dialog>
  )
}
