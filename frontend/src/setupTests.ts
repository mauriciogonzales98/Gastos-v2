import '@testing-library/jest-dom/vitest'

/**
 * jsdom 30 conoce el elemento `<dialog>` pero no implementa su comportamiento: no trae
 * `showModal` ni `close`, no mueve el foco al abrir y el Escape no dispara `cancel`. Sin
 * esto, cualquier test que abra el modal de edicion muere con "showModal is not a
 * function".
 *
 * Es un relleno minimo y a proposito: abre, cierra, lleva el foco adentro y avisa del
 * Escape, que es lo que los tests observan. El fondo inerte y la trampa de foco los pone
 * el navegador de verdad y no se pueden verificar aca.
 */
if (!HTMLDialogElement.prototype.showModal) {
  const alPresionarEscape = new WeakMap<HTMLDialogElement, (evento: KeyboardEvent) => void>()

  HTMLDialogElement.prototype.showModal = function abrir(this: HTMLDialogElement) {
    this.open = true

    // El Escape lo maneja el navegador para el dialogo modal de mas arriba, no un
    // listener sobre el elemento: por eso va sobre el documento y no sobre `this`.
    const manejador = (evento: KeyboardEvent) => {
      if (evento.key !== 'Escape' || !this.open) {
        return
      }

      // El navegador cierra el dialogo salvo que se cancele el evento `cancel`.
      if (this.dispatchEvent(new Event('cancel', { cancelable: true }))) {
        this.close()
      }
    }

    alPresionarEscape.set(this, manejador)
    document.addEventListener('keydown', manejador)

    // `showModal` mueve el foco adentro del dialogo; sin esto quedaria en el `body` y el
    // teclado del test no llegaria a ningun lado.
    this.focus()
  }

  HTMLDialogElement.prototype.close = function cerrar(this: HTMLDialogElement) {
    const manejador = alPresionarEscape.get(this)
    if (manejador) {
      document.removeEventListener('keydown', manejador)
      alPresionarEscape.delete(this)
    }

    this.open = false
    this.dispatchEvent(new Event('close'))
  }
}
