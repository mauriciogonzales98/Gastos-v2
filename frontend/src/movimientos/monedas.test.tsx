import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from '../App'
import { backendConSesion, movimiento, type BackendFalso } from '../pruebas/backendFalso'

afterEach(() => {
  vi.unstubAllGlobals()
})

async function abrirApp(backend: BackendFalso) {
  render(<App />)
  await screen.findByRole('heading', { name: 'Nuevo movimiento' })
  await screen.findByRole('option', { name: 'Comida' })
  return backend
}

/**
 * RF-24 a RF-29.
 * AC-41, AC-42, AC-43 y AC-46 son del dashboard y se cubren en la feature 3.
 */
describe('monedas', () => {
  it('AC-38: el formulario propone pesos y el alta se manda en pesos', async () => {
    const backend = backendConSesion().responder('POST /movimientos', {
      estado: 201,
      cuerpo: movimiento(),
    })
    await abrirApp(backend)

    expect(screen.getByLabelText('Moneda')).toHaveValue('ARS')

    await userEvent.type(screen.getByLabelText('Monto'), '1500')
    await userEvent.selectOptions(screen.getByLabelText('Categoría'), 'cat-comida')
    await userEvent.click(screen.getByRole('button', { name: 'Agregar' }))

    await waitFor(() => expect(backend.pedidosA('POST /movimientos')).toHaveLength(1))
    expect(backend.pedidosA('POST /movimientos')[0].cuerpo).toMatchObject({ moneda: 'ARS' })
  })

  it('AC-37: al elegir dólares, el alta se manda en dólares', async () => {
    const backend = backendConSesion().responder('POST /movimientos', {
      estado: 201,
      cuerpo: movimiento({ moneda: 'USD' }),
    })
    await abrirApp(backend)

    await userEvent.type(screen.getByLabelText('Monto'), '150')
    await userEvent.selectOptions(screen.getByLabelText('Moneda'), 'USD')
    await userEvent.selectOptions(screen.getByLabelText('Categoría'), 'cat-comida')
    await userEvent.click(screen.getByRole('button', { name: 'Agregar' }))

    await waitFor(() => expect(backend.pedidosA('POST /movimientos')).toHaveLength(1))
    expect(backend.pedidosA('POST /movimientos')[0].cuerpo).toMatchObject({ moneda: 'USD' })
  })

  it('AC-44: dos montos iguales en monedas distintas se distinguen en el listado', async () => {
    await abrirApp(
      backendConSesion([
        movimiento({ id: 'm1', monto: 1000, moneda: 'ARS' }),
        movimiento({ id: 'm2', monto: 1000, moneda: 'USD' }),
      ]),
    )

    const filas = (await screen.findAllByRole('row')).slice(1)
    expect(filas[0]).toHaveTextContent('$ 1.000,00')
    expect(filas[1]).toHaveTextContent('US$ 1.000,00')
  })

  it('AC-45: al filtrar por dólares, el listado se pide solo con esa moneda', async () => {
    const backend = await abrirApp(backendConSesion([movimiento()]))

    // Sin filtro no se manda ninguna moneda: se ven las dos.
    expect(backend.pedidosA('GET /movimientos')[0].parametros.get('moneda')).toBeNull()

    await userEvent.selectOptions(screen.getByLabelText('Filtrar por moneda'), 'USD')

    await waitFor(() => {
      expect(backend.pedidosA('GET /movimientos').at(-1)!.parametros.get('moneda')).toBe('USD')
    })
  })

  it('AC-47: al editar, el formulario trae la moneda del movimiento y permite cambiarla', async () => {
    const backend = backendConSesion([
      movimiento({ id: 'mov-9', monto: 200, moneda: 'ARS' }),
    ]).responder('PUT /movimientos/:id', { estado: 200, cuerpo: movimiento({ moneda: 'USD' }) })
    await abrirApp(backend)

    await userEvent.click(await screen.findByRole('button', { name: 'Editar' }))

    expect(screen.getByLabelText('Moneda')).toHaveValue('ARS')

    await userEvent.selectOptions(screen.getByLabelText('Moneda'), 'USD')
    await userEvent.click(screen.getByRole('button', { name: 'Guardar cambios' }))

    await waitFor(() => expect(backend.pedidosA('PUT /movimientos/:id')).toHaveLength(1))
    expect(backend.pedidosA('PUT /movimientos/:id')[0].cuerpo).toMatchObject({
      moneda: 'USD',
    })
  })

  it('AC-39: si el backend rechaza la moneda, se muestra su mensaje', async () => {
    const backend = backendConSesion().responder('POST /movimientos', {
      estado: 400,
      cuerpo: { errors: { moneda: ['La moneda tiene que ser una de: ARS, USD.'] } },
    })
    await abrirApp(backend)

    await userEvent.type(screen.getByLabelText('Monto'), '150')
    await userEvent.selectOptions(screen.getByLabelText('Categoría'), 'cat-comida')
    await userEvent.click(screen.getByRole('button', { name: 'Agregar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'La moneda tiene que ser una de: ARS, USD.',
    )
  })
})
