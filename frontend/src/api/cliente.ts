// Cliente HTTP contra la API. Todo pasa por /api, que Vite redirige al backend (ver
// vite.config.ts), asi que los pedidos son del mismo origen y la cookie de sesion viaja sola.

const BASE = '/api'

export class ErrorApi extends Error {
  readonly estado: number

  constructor(estado: number, mensaje: string) {
    super(mensaje)
    this.name = 'ErrorApi'
    this.estado = estado
  }
}

type ProblemDetails = {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

async function mensajeDeError(respuesta: Response): Promise<string> {
  try {
    const problema = (await respuesta.json()) as ProblemDetails
    // Los 400 de validacion traen los motivos por campo; se muestran todos juntos.
    if (problema.errors) {
      const detalles = Object.values(problema.errors).flat()
      if (detalles.length > 0) return detalles.join(' ')
    }
    return problema.detail ?? problema.title ?? 'No se pudo completar la operacion.'
  } catch {
    return 'No se pudo completar la operacion.'
  }
}

async function pedir<T>(ruta: string, opciones: RequestInit = {}): Promise<T> {
  const respuesta = await fetch(`${BASE}${ruta}`, {
    ...opciones,
    credentials: 'same-origin',
    headers: opciones.body ? { 'Content-Type': 'application/json' } : undefined,
  })

  if (!respuesta.ok) {
    throw new ErrorApi(respuesta.status, await mensajeDeError(respuesta))
  }

  if (respuesta.status === 204) return undefined as T

  return (await respuesta.json()) as T
}

export type Usuario = {
  id: string
  email: string
}

export type Credenciales = {
  email: string
  contrasena: string
}

export type TipoCategoria = 'Gasto' | 'Ingreso'

export type Categoria = {
  id: string
  nombre: string
  tipo: TipoCategoria
  esDelSistema: boolean
}

export type Movimiento = {
  id: string
  monto: number
  /** ISO corto, "2026-08-08": es una fecha del usuario, sin hora ni zona. */
  fecha: string
  categoriaId: string
  categoriaNombre: string
  tipo: TipoCategoria
}

export type DatosMovimiento = {
  monto: number
  fecha: string
  categoriaId: string
}

export type FiltrosMovimientos = {
  desde: string
  hasta: string
  /** Vacio = todas las categorias (RF-17). */
  categoriaId: string
}

function consultaDeFiltros({ desde, hasta, categoriaId }: FiltrosMovimientos): string {
  const parametros = new URLSearchParams({ desde, hasta })
  if (categoriaId) parametros.set('categoriaId', categoriaId)
  return `?${parametros.toString()}`
}

export const api = {
  registrar: (credenciales: Credenciales) =>
    pedir<Usuario>('/auth/register', { method: 'POST', body: JSON.stringify(credenciales) }),

  iniciarSesion: (credenciales: Credenciales) =>
    pedir<Usuario>('/auth/login', { method: 'POST', body: JSON.stringify(credenciales) }),

  cerrarSesion: () => pedir<void>('/auth/logout', { method: 'POST' }),

  sesionActual: () => pedir<Usuario>('/auth/me'),

  categorias: {
    listar: () => pedir<Categoria[]>('/categorias'),

    crear: (nombre: string, tipo: TipoCategoria) =>
      pedir<Categoria>('/categorias', { method: 'POST', body: JSON.stringify({ nombre, tipo }) }),

    renombrar: (id: string, nombre: string) =>
      pedir<Categoria>(`/categorias/${id}`, { method: 'PUT', body: JSON.stringify({ nombre }) }),

    eliminar: (id: string) => pedir<void>(`/categorias/${id}`, { method: 'DELETE' }),
  },

  movimientos: {
    listar: (filtros: FiltrosMovimientos) =>
      pedir<Movimiento[]>(`/movimientos${consultaDeFiltros(filtros)}`),

    crear: (datos: DatosMovimiento) =>
      pedir<Movimiento>('/movimientos', { method: 'POST', body: JSON.stringify(datos) }),

    modificar: (id: string, datos: DatosMovimiento) =>
      pedir<Movimiento>(`/movimientos/${id}`, { method: 'PUT', body: JSON.stringify(datos) }),

    eliminar: (id: string) => pedir<void>(`/movimientos/${id}`, { method: 'DELETE' }),
  },
}
