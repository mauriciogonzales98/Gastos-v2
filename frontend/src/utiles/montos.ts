/**
 * Formato de monto mientras se escribe: "." separa miles y "," separa decimales, que es
 * el criterio de es-AR y el mismo que usa `comoMonto` para los montos ya guardados.
 *
 * El campo del formulario no puede ser `<input type="number">`: el navegador considera
 * invalido cualquier texto con separadores y devuelve `value` vacio, con lo que no hay
 * forma de mostrar los miles. Es un `type="text"` y el parseo lo hace este archivo.
 */

/** El backend guarda `decimal(18,2)`: mas decimales los rechaza (AC-18). */
const MAXIMO_DECIMALES = 2

type Partes = {
  /** Solo digitos, sin separadores de miles. */
  entero: string
  /** Solo digitos, ya recortado a `MAXIMO_DECIMALES`. `null` si no se escribio la coma. */
  decimal: string | null
}

/**
 * Parte el texto tipeado en la parte entera y la decimal.
 *
 * El ultimo separador cuenta como coma decimal si es una "," o si lo siguen a lo sumo dos
 * digitos. Un "." seguido de exactamente tres digitos es separador de miles, que es
 * ademas el que inserta `formatearMonto`: por eso "1.234" se sigue leyendo como mil
 * doscientos treinta y cuatro mientras se escribe, y "1.23" como uno con veintitres.
 *
 * La consecuencia buscada es que el "." del teclado numerico sirva igual que la ",":
 * al escribir de izquierda a derecha, cuando se tipea el separador todavia no hay tres
 * digitos despues, asi que se toma como decimal.
 */
function partir(texto: string): Partes {
  const limpio = texto.replace(/[^\d.,]/g, '')
  const ultimo = Math.max(limpio.lastIndexOf(','), limpio.lastIndexOf('.'))
  const digitosDespues = limpio.length - ultimo - 1
  const esDecimal =
    ultimo >= 0 && (limpio[ultimo] === ',' || digitosDespues <= MAXIMO_DECIMALES)

  const crudoEntero = esDecimal ? limpio.slice(0, ultimo) : limpio

  return {
    entero: crudoEntero.replace(/\D/g, ''),
    decimal: esDecimal
      ? limpio
          .slice(ultimo + 1)
          .replace(/\D/g, '')
          .slice(0, MAXIMO_DECIMALES)
      : null,
  }
}

/** "1234567" -> "1.234.567". Descarta los ceros a la izquierda. */
function conSeparadorDeMiles(entero: string): string {
  const sinCerosAdelante = entero.replace(/^0+(?=\d)/, '')
  return sinCerosAdelante.replace(/\B(?=(\d{3})+(?!\d))/g, '.')
}

/**
 * Formatea lo que se esta escribiendo sin alterar lo que la persona todavia no termino:
 * conserva la coma recien tipeada ("1.234,") y los ceros a la derecha ("1,50"), que un
 * `Intl.NumberFormat` sobre el numero se comeria.
 */
export function formatearMonto(texto: string): string {
  const { entero, decimal } = partir(texto)

  if (!entero && decimal === null) {
    return ''
  }

  // Con la coma escrita pero sin parte entera se muestra el cero: ",5" queda "0,5".
  const parteEntera = conSeparadorDeMiles(entero) || '0'

  return decimal === null ? parteEntera : `${parteEntera},${decimal}`
}

/**
 * El numero que se le manda a la API. Devuelve `NaN` si no hay ningun digito, para que
 * el formulario lo rechace con el mismo mensaje que un texto invalido.
 */
export function aNumero(texto: string): number {
  const { entero, decimal } = partir(texto)

  if (!entero && !decimal) {
    return Number.NaN
  }

  return Number(`${entero || '0'}.${decimal || '0'}`)
}

/** El camino inverso, para cargar un movimiento existente en el formulario (RF-14). */
export function desdeNumero(monto: number): string {
  return formatearMonto(monto.toFixed(MAXIMO_DECIMALES).replace('.', ','))
}
