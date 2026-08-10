import { describe, expect, it } from 'vitest'
import { aNumero, desdeNumero, formatearMonto } from './montos'

describe('formato del monto mientras se escribe', () => {
  it('agrega el separador de miles a partir del cuarto digito', () => {
    expect(formatearMonto('1')).toBe('1')
    expect(formatearMonto('999')).toBe('999')
    expect(formatearMonto('1000')).toBe('1.000')
    expect(formatearMonto('1234567')).toBe('1.234.567')
  })

  it('reformatea lo que ya tenia separadores al seguir escribiendo', () => {
    // El caso real: el campo muestra "1.234" y se tipea un 5 al final.
    expect(formatearMonto('1.2345')).toBe('12.345')
    expect(formatearMonto('12.345')).toBe('12.345')
  })

  it('toma la coma como separador decimal', () => {
    expect(formatearMonto('1234,5')).toBe('1.234,5')
    expect(formatearMonto('1.234,56')).toBe('1.234,56')
  })

  it('toma como decimal el punto del teclado numerico', () => {
    expect(formatearMonto('1.5')).toBe('1,5')
    expect(formatearMonto('1.50')).toBe('1,50')
    expect(formatearMonto('0.99')).toBe('0,99')
  })

  it('conserva la coma recien tipeada y los ceros a la derecha', () => {
    expect(formatearMonto('1234,')).toBe('1.234,')
    expect(formatearMonto('1,50')).toBe('1,50')
    expect(formatearMonto('10,00')).toBe('10,00')
  })

  it('recorta a dos decimales, que es lo que acepta el backend', () => {
    expect(formatearMonto('1,999')).toBe('1,99')
  })

  it('descarta lo que no sea un numero', () => {
    expect(formatearMonto('')).toBe('')
    expect(formatearMonto('abc')).toBe('')
    expect(formatearMonto('$ 1.500')).toBe('1.500')
  })

  it('completa el cero cuando se empieza por la coma y saca los de la izquierda', () => {
    expect(formatearMonto(',5')).toBe('0,5')
    expect(formatearMonto('007')).toBe('7')
  })

  it('entiende un monto pegado en formato ingles', () => {
    expect(formatearMonto('1,234.56')).toBe('1.234,56')
  })
})

describe('conversion al numero que se le manda a la API', () => {
  it('convierte el texto formateado', () => {
    expect(aNumero('1.234,56')).toBe(1234.56)
    expect(aNumero('1.500')).toBe(1500)
    expect(aNumero('0,99')).toBe(0.99)
  })

  it('trata la coma sin decimales como el entero solo', () => {
    expect(aNumero('1.234,')).toBe(1234)
  })

  it('devuelve NaN si no hay ningun digito, para que el formulario lo rechace', () => {
    expect(aNumero('')).toBeNaN()
    expect(aNumero('abc')).toBeNaN()
    expect(aNumero(',')).toBeNaN()
  })

  it('devuelve cero cuando se escribio un cero, que el formulario rechaza por <= 0', () => {
    expect(aNumero('0')).toBe(0)
    expect(aNumero('0,00')).toBe(0)
  })
})

describe('carga de un movimiento existente en el formulario', () => {
  it('muestra el monto guardado ya formateado', () => {
    expect(desdeNumero(1500)).toBe('1.500,00')
    expect(desdeNumero(1234.5)).toBe('1.234,50')
    expect(desdeNumero(0.99)).toBe('0,99')
  })

  it('vuelve al mismo numero: formatear y parsear son inversos', () => {
    for (const monto of [1, 999.99, 1500, 1234.56, 1000000]) {
      expect(aNumero(desdeNumero(monto))).toBe(monto)
    }
  })
})
