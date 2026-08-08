/**
 * Fechas en formato "yyyy-MM-dd", que es lo que entienden tanto <input type="date">
 * como el DateOnly del backend. Se arman con la hora local a proposito: `toISOString()`
 * pasa por UTC y, segun la zona horaria, devuelve el dia anterior.
 */

export function comoIso(fecha: Date): string {
  const mes = `${fecha.getMonth() + 1}`.padStart(2, '0')
  const dia = `${fecha.getDate()}`.padStart(2, '0')
  return `${fecha.getFullYear()}-${mes}-${dia}`
}

/** RF-12: el valor por defecto del campo fecha del formulario. */
export function hoy(): string {
  return comoIso(new Date())
}

/** RF-18: el rango por defecto de los filtros. */
export function mesActual(): { desde: string; hasta: string } {
  const ahora = new Date()
  return {
    desde: comoIso(new Date(ahora.getFullYear(), ahora.getMonth(), 1)),
    hasta: comoIso(new Date(ahora.getFullYear(), ahora.getMonth() + 1, 0)),
  }
}

/** "2026-08-08" -> "08/08/2026", sin pasar por Date para no reintroducir la zona horaria. */
export function comoTexto(fechaIso: string): string {
  const [anio, mes, dia] = fechaIso.split('-')
  return `${dia}/${mes}/${anio}`
}

const formatoMoneda = new Intl.NumberFormat('es-AR', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

/** Moneda unica, dos decimales: el PRD deja fuera de alcance la conversion de divisas. */
export function comoMonto(monto: number): string {
  return `$ ${formatoMoneda.format(monto)}`
}
