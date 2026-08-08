/**
 * Fechas en formato "yyyy-MM-dd", que es lo que entienden tanto <input type="date">
 * como el DateOnly del backend. Se arman con la hora local a proposito: `toISOString()`
 * pasa por UTC y, segun la zona horaria, devuelve el dia anterior.
 */

import type { CodigoMoneda, Moneda } from '../api/cliente'

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

/**
 * RF-27: el símbolo va siempre, porque dos montos iguales en monedas distintas serían
 * indistinguibles. No hay conversión: cada monto se muestra en la suya (RF-29).
 *
 * El símbolo y la cantidad de decimales salen del catálogo (`GET /monedas`), no de una
 * tabla hardcodeada: por eso sumar una moneda no toca este archivo. Si el código no está
 * en el catálogo se muestra el código tal cual, que es más honesto que inventar un símbolo.
 */
export function comoMonto(monto: number, codigo: CodigoMoneda, catalogo: Moneda[]): string {
  const moneda = catalogo.find((m) => m.codigo === codigo)
  const decimales = moneda?.decimales ?? 2

  const formato = new Intl.NumberFormat('es-AR', {
    minimumFractionDigits: decimales,
    maximumFractionDigits: decimales,
  })

  return `${moneda?.simbolo ?? codigo} ${formato.format(monto)}`
}
