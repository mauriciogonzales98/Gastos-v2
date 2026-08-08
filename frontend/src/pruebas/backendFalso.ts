import { vi } from 'vitest'
import type { Categoria, Moneda, Movimiento, Usuario } from '../api/cliente'

export type Pedido = {
  metodo: string
  ruta: string
  parametros: URLSearchParams
  cuerpo: Record<string, unknown> | null
}

type Respuesta = { estado: number; cuerpo?: unknown }

type Manejador = (pedido: Pedido) => Respuesta

/**
 * Backend falso para los tests: reemplaza `fetch` y despacha por metodo y ruta.
 * Guarda todos los pedidos, asi un test puede afirmar sobre lo que se le mando a la API
 * (por ejemplo, que el listado se pidio con el rango del mes actual).
 */
export class BackendFalso {
  readonly pedidos: Pedido[] = []

  private readonly manejadores = new Map<string, Manejador>()

  constructor() {
    vi.stubGlobal(
      'fetch',
      vi.fn((entrada: string | URL | Request, opciones: RequestInit = {}) => {
        const url = new URL(
          (typeof entrada === 'string' ? entrada : entrada.toString()).replace('/api', ''),
          'http://tests.local',
        )

        const pedido: Pedido = {
          metodo: (opciones.method ?? 'GET').toUpperCase(),
          ruta: url.pathname,
          parametros: url.searchParams,
          cuerpo: typeof opciones.body === 'string' ? JSON.parse(opciones.body) : null,
        }
        this.pedidos.push(pedido)

        const manejador =
          this.manejadores.get(`${pedido.metodo} ${pedido.ruta}`) ??
          this.manejadores.get(`${pedido.metodo} ${plantillaDe(pedido.ruta)}`)

        if (!manejador) {
          throw new Error(`El test no preparo una respuesta para ${pedido.metodo} ${pedido.ruta}`)
        }

        const { estado, cuerpo } = manejador(pedido)

        return Promise.resolve(
          new Response(estado === 204 ? null : JSON.stringify(cuerpo ?? {}), {
            status: estado,
            headers: { 'Content-Type': 'application/json' },
          }),
        )
      }),
    )
  }

  /** La clave es "METODO /ruta"; el ultimo segmento se puede escribir como ":id". */
  responder(clave: string, manejador: Manejador | Respuesta) {
    this.manejadores.set(clave, typeof manejador === 'function' ? manejador : () => manejador)
    return this
  }

  pedidosA(clave: string): Pedido[] {
    const [metodo, ruta] = clave.split(' ')
    return this.pedidos.filter(
      (p) => p.metodo === metodo && (p.ruta === ruta || plantillaDe(p.ruta) === ruta),
    )
  }
}

/** "/movimientos/abc-123" -> "/movimientos/:id" */
function plantillaDe(ruta: string): string {
  const partes = ruta.split('/')
  return partes.length > 2 ? `${partes.slice(0, -1).join('/')}/:id` : ruta
}

// --- Datos de ejemplo ---

export const USUARIO: Usuario = {
  id: '11111111-1111-1111-1111-111111111111',
  email: 'ana@ejemplo.com',
}

export const CATEGORIAS: Categoria[] = [
  { id: 'cat-comida', nombre: 'Comida', tipo: 'Gasto', esDelSistema: true },
  { id: 'cat-ocio', nombre: 'Ocio', tipo: 'Gasto', esDelSistema: true },
  { id: 'cat-sueldo', nombre: 'Sueldo', tipo: 'Ingreso', esDelSistema: true },
  { id: 'cat-mascotas', nombre: 'Mascotas', tipo: 'Gasto', esDelSistema: false },
]

export const MONEDAS: Moneda[] = [
  { codigo: 'ARS', nombre: 'Pesos', simbolo: '$', decimales: 2, esPredeterminada: true },
  { codigo: 'USD', nombre: 'Dolares', simbolo: 'US$', decimales: 2, esPredeterminada: false },
]

export function movimiento(parcial: Partial<Movimiento> = {}): Movimiento {
  return {
    id: 'mov-1',
    monto: 1500,
    moneda: 'ARS',
    fecha: '2026-08-05',
    categoriaId: 'cat-comida',
    categoriaNombre: 'Comida',
    tipo: 'Gasto',
    ...parcial,
  }
}

/** Arma un backend con la sesion abierta, el catalogo y el listado que se le pase. */
export function backendConSesion(movimientos: Movimiento[] = []): BackendFalso {
  return new BackendFalso()
    .responder('GET /auth/me', { estado: 200, cuerpo: USUARIO })
    .responder('GET /categorias', { estado: 200, cuerpo: CATEGORIAS })
    .responder('GET /monedas', { estado: 200, cuerpo: MONEDAS })
    .responder('GET /movimientos', { estado: 200, cuerpo: movimientos })
}
