import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from '../App'
import type { Dashboard } from '../api/cliente'
import { backendConSesion, type BackendFalso } from '../pruebas/backendFalso'
import { mesActual } from '../utiles/fechas'

afterEach(() => {
  vi.unstubAllGlobals()
})

function dashboard(parcial: Partial<Dashboard> = {}): Dashboard {
  return {
    desde: mesActual().desde,
    hasta: mesActual().hasta,
    monedas: [
      {
        moneda: 'ARS',
        totalIngresos: 90000,
        totalGastos: 1500,
        balance: 88500,
        gastosPorCategoria: [
          { categoriaId: 'cat-comida', categoriaNombre: 'Comida', total: 1000 },
          { categoriaId: 'cat-ocio', categoriaNombre: 'Ocio', total: 500 },
        ],
      },
      {
        moneda: 'USD',
        totalIngresos: 500,
        totalGastos: 120,
        balance: 380,
        gastosPorCategoria: [
          { categoriaId: 'cat-comida', categoriaNombre: 'Comida', total: 120 },
        ],
      },
    ],
    ...parcial,
  }
}

function backendConDashboard(datos: Dashboard = dashboard()): BackendFalso {
  return backendConSesion().responder('GET /dashboard', { estado: 200, cuerpo: datos })
}

/**
 * Abre la pestaña del dashboard y espera a que esté armado. El bloque de una moneda sólo
 * aparece cuando llegaron las dos cosas que necesita: el catálogo y los totales.
 */
async function abrirApp(backend: BackendFalso) {
  render(<App />)
  await userEvent.click(await screen.findByRole('tab', { name: 'Dashboard' }))
  await screen.findByRole('article', { name: 'Dashboard en Pesos' })
  return backend
}

function bloqueDe(nombre: string) {
  return screen.getByRole('article', { name: `Dashboard en ${nombre}` })
}

describe('dashboard (RF-19 a RF-21)', () => {
  it('AC-27: muestra el total de cada categoría', async () => {
    await abrirApp(backendConDashboard())

    const pesos = bloqueDe('Pesos')
    expect(within(pesos).getByText('Comida')).toBeInTheDocument()
    expect(within(pesos).getByText('$ 1.000,00')).toBeInTheDocument()
    expect(within(pesos).getByText('Ocio')).toBeInTheDocument()
    expect(within(pesos).getByText('$ 500,00')).toBeInTheDocument()
  })

  it('AC-28: muestra ingresos, gastos y balance', async () => {
    await abrirApp(backendConDashboard())

    const pesos = bloqueDe('Pesos')
    expect(within(pesos).getByText('$ 90.000,00')).toBeInTheDocument()
    expect(within(pesos).getByText('$ 1.500,00')).toBeInTheDocument()
    expect(within(pesos).getByText('$ 88.500,00')).toBeInTheDocument()
  })

  it('AC-41 y AC-42: cada moneda tiene su bloque y ninguno mezcla montos', async () => {
    await abrirApp(backendConDashboard())

    const pesos = bloqueDe('Pesos')
    const dolares = bloqueDe('Dolares')

    // Misma categoría, montos distintos, cada uno en su bloque y con su símbolo.
    expect(within(pesos).getByText('$ 1.000,00')).toBeInTheDocument()
    // Aparece dos veces en el bloque: como total de gastos y como total de la categoría.
    expect(within(dolares).getAllByText('US$ 120,00').length).toBeGreaterThan(0)

    // El balance de cada moneda es el suyo, no la suma cruzada.
    expect(within(pesos).getByText('$ 88.500,00')).toBeInTheDocument()
    expect(within(dolares).getByText('US$ 380,00')).toBeInTheDocument()
    expect(screen.queryByText('$ 88.880,00')).not.toBeInTheDocument()
  })

  it('AC-29: al cambiar el rango, el dashboard se vuelve a pedir con esas fechas', async () => {
    const backend = await abrirApp(backendConDashboard())

    await userEvent.clear(screen.getByLabelText('Desde (dashboard)'))
    await userEvent.type(screen.getByLabelText('Desde (dashboard)'), '2026-03-01')

    await waitFor(() => {
      expect(backend.pedidosA('GET /dashboard').at(-1)!.parametros.get('desde')).toBe('2026-03-01')
    })
  })

  it('AC-46: al filtrar por moneda, el dashboard se pide solo con esa', async () => {
    const backend = await abrirApp(backendConDashboard())

    // Sin filtro no se manda ninguna: se ven las dos.
    expect(backend.pedidosA('GET /dashboard')[0].parametros.get('moneda')).toBeNull()

    await userEvent.selectOptions(screen.getByLabelText('Moneda (dashboard)'), 'USD')

    await waitFor(() => {
      expect(backend.pedidosA('GET /dashboard').at(-1)!.parametros.get('moneda')).toBe('USD')
    })
  })

  it('AC-31: sin movimientos, todo en cero y sin ningún error', async () => {
    await abrirApp(
      backendConDashboard(
        dashboard({
          monedas: [
            {
              moneda: 'ARS',
              totalIngresos: 0,
              totalGastos: 0,
              balance: 0,
              gastosPorCategoria: [],
            },
          ],
        }),
      ),
    )

    const pesos = bloqueDe('Pesos')
    expect(within(pesos).getAllByText('$ 0,00')).toHaveLength(3)
    expect(within(pesos).getByText(/Sin gastos en este período/)).toBeInTheDocument()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })
})

describe('resumen del mes (RF-22)', () => {
  // El resumen vive en la pestaña de movimientos, que es la pantalla de trabajo diario.
  async function verResumen() {
    await userEvent.click(screen.getByRole('tab', { name: 'Movimientos' }))
    return screen.findByRole('region', { name: /Resumen en Pesos/ })
  }

  it('AC-30: sale del mismo endpoint pedido con el mes actual', async () => {
    const backend = await abrirApp(backendConDashboard())

    const resumen = await verResumen()
    expect(within(resumen).getByText('$ 90.000,00')).toBeInTheDocument()
    expect(within(resumen).getByText('$ 1.500,00')).toBeInTheDocument()

    // Uno de los pedidos es exactamente el del mes actual, sin filtro de moneda.
    const { desde, hasta } = mesActual()
    expect(
      backend
        .pedidosA('GET /dashboard')
        .some(
          (p) =>
            p.parametros.get('desde') === desde &&
            p.parametros.get('hasta') === hasta &&
            p.parametros.get('moneda') === null,
        ),
    ).toBe(true)
  })

  it('el resumen del mes no cambia cuando se filtra el dashboard', async () => {
    const backend = await abrirApp(backendConDashboard())

    await userEvent.selectOptions(screen.getByLabelText('Moneda (dashboard)'), 'USD')

    await waitFor(() => {
      expect(backend.pedidosA('GET /dashboard').at(-1)!.parametros.get('moneda')).toBe('USD')
    })

    // El resumen sigue mostrando las dos monedas del mes actual.
    await verResumen()
    expect(screen.getByRole('region', { name: /Resumen en Dolares/ })).toBeInTheDocument()
  })

  it('cambiar de pestaña no vuelve a pedir nada: los datos viven en la pantalla', async () => {
    const backend = await abrirApp(backendConDashboard())
    const pedidos = backend.pedidos.length

    await userEvent.click(screen.getByRole('tab', { name: 'Movimientos' }))
    await userEvent.click(screen.getByRole('tab', { name: 'Dashboard' }))

    expect(backend.pedidos).toHaveLength(pedidos)
  })
})
