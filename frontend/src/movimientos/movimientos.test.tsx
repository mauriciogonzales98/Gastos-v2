import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from '../App'
import { backendConSesion, movimiento, type BackendFalso } from '../pruebas/backendFalso'
import { hoy, mesActual } from '../utiles/fechas'

afterEach(() => {
  vi.unstubAllGlobals()
})

/**
 * Espera a que la pantalla principal termine de cargar. No alcanza con que aparezca el
 * formulario: las categorías y los movimientos llegan en efectos posteriores, y sin
 * esperarlos los tests corren contra una pantalla todavía vacía.
 */
async function abrirApp(backend: BackendFalso) {
  render(<App />)
  await screen.findByRole('heading', { name: 'Nuevo movimiento' })
  await screen.findByRole('option', { name: 'Comida' })
  return backend
}

async function cargarMovimiento(monto: string, categoria: string) {
  await userEvent.type(screen.getByLabelText('Monto'), monto)
  await userEvent.selectOptions(screen.getByLabelText('Categoría'), categoria)
  await userEvent.click(screen.getByRole('button', { name: 'Agregar' }))
}

describe('formulario de carga (RF-10 a RF-13)', () => {
  it('AC-10: el selector ofrece solo categorías del tipo que se está cargando', async () => {
    await abrirApp(backendConSesion())

    const selector = screen.getByLabelText('Categoría')
    expect(within(selector).getByRole('option', { name: 'Comida' })).toBeInTheDocument()
    expect(within(selector).queryByRole('option', { name: 'Sueldo' })).not.toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Ingreso' }))

    expect(within(selector).getByRole('option', { name: 'Sueldo' })).toBeInTheDocument()
    expect(within(selector).queryByRole('option', { name: 'Comida' })).not.toBeInTheDocument()
  })

  it('AC-17: el campo fecha viene con la fecha de hoy', async () => {
    await abrirApp(backendConSesion())

    expect(screen.getByLabelText('Fecha')).toHaveValue(hoy())
  })

  it('AC-15: al agregar un gasto se manda el alta y se recarga el listado', async () => {
    const backend = backendConSesion().responder('POST /movimientos', {
      estado: 201,
      cuerpo: movimiento(),
    })
    await abrirApp(backend)

    await cargarMovimiento('1500', 'cat-comida')

    await waitFor(() => expect(backend.pedidosA('POST /movimientos')).toHaveLength(1))
    expect(backend.pedidosA('POST /movimientos')[0].cuerpo).toEqual({
      monto: 1500,
      moneda: 'ARS',
      fecha: hoy(),
      categoriaId: 'cat-comida',
    })
    // Se vuelve a pedir el listado: el primero fue el de la carga inicial.
    expect(backend.pedidosA('GET /movimientos').length).toBeGreaterThan(1)
  })

  it('AC-18: sin categoría no se guarda nada y se muestra el motivo', async () => {
    const backend = await abrirApp(backendConSesion())

    await userEvent.type(screen.getByLabelText('Monto'), '1500')
    await userEvent.click(screen.getByRole('button', { name: 'Agregar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Elegí una categoría.')
    expect(backend.pedidosA('POST /movimientos')).toHaveLength(0)
  })

  it('AC-18: un monto en cero no se guarda y se muestra el motivo', async () => {
    const backend = await abrirApp(backendConSesion())

    await cargarMovimiento('0', 'cat-comida')

    expect(await screen.findByRole('alert')).toHaveTextContent('mayor a cero')
    expect(backend.pedidosA('POST /movimientos')).toHaveLength(0)
  })

  it('AC-18: si el backend rechaza el monto, se muestra su mensaje', async () => {
    const backend = backendConSesion().responder('POST /movimientos', {
      estado: 400,
      cuerpo: { errors: { monto: ['El monto admite hasta 2 decimales.'] } },
    })
    await abrirApp(backend)

    await cargarMovimiento('10.999', 'cat-comida')

    expect(await screen.findByRole('alert')).toHaveTextContent('El monto admite hasta 2 decimales.')
  })
})

describe('listado y filtros (RF-16 a RF-18)', () => {
  it('AC-25: al abrir, el listado se pide con el rango del mes actual', async () => {
    const backend = await abrirApp(backendConSesion())

    const { desde, hasta } = mesActual()
    const pedido = backend.pedidosA('GET /movimientos')[0]
    expect(pedido.parametros.get('desde')).toBe(desde)
    expect(pedido.parametros.get('hasta')).toBe(hasta)
    // AC-24: sin filtro de categoría, no se manda ninguno.
    expect(pedido.parametros.get('categoriaId')).toBeNull()
  })

  it('AC-22: se ven gastos e ingresos, con su signo', async () => {
    await abrirApp(
      backendConSesion([
        movimiento({ id: 'm1', monto: 1500, categoriaNombre: 'Comida', tipo: 'Gasto' }),
        movimiento({
          id: 'm2',
          monto: 90000,
          categoriaId: 'cat-sueldo',
          categoriaNombre: 'Sueldo',
          tipo: 'Ingreso',
        }),
      ]),
    )

    const filas = (await screen.findAllByRole('row')).slice(1)
    expect(filas).toHaveLength(2)
    expect(filas[0]).toHaveTextContent('Comida')
    expect(filas[0]).toHaveTextContent('-')
    expect(filas[1]).toHaveTextContent('Sueldo')
    expect(filas[1]).toHaveTextContent('+')
  })

  it('AC-23: al elegir una categoría, el listado se vuelve a pedir filtrado', async () => {
    const backend = await abrirApp(backendConSesion([movimiento()]))

    await userEvent.selectOptions(screen.getByLabelText('Filtrar por categoría'), 'cat-comida')

    await waitFor(() => {
      const ultimo = backend.pedidosA('GET /movimientos').at(-1)!
      expect(ultimo.parametros.get('categoriaId')).toBe('cat-comida')
    })
  })

  it('AC-26: al cambiar el rango, el listado se pide con las fechas elegidas', async () => {
    const backend = await abrirApp(backendConSesion([movimiento()]))

    await userEvent.clear(screen.getByLabelText('Desde'))
    await userEvent.type(screen.getByLabelText('Desde'), '2026-03-01')

    await waitFor(() => {
      expect(backend.pedidosA('GET /movimientos').at(-1)!.parametros.get('desde')).toBe('2026-03-01')
    })
  })

  it('sin movimientos en el período se avisa, sin error', async () => {
    await abrirApp(backendConSesion([]))

    expect(screen.getByText(/No hay movimientos en este período/)).toBeInTheDocument()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })
})

describe('edición y baja (RF-14, RF-15)', () => {
  it('AC-19: editar carga el movimiento en el formulario y guarda con PUT', async () => {
    const backend = backendConSesion([movimiento({ id: 'mov-9', monto: 1500 })]).responder(
      'PUT /movimientos/:id',
      { estado: 200, cuerpo: movimiento({ id: 'mov-9', monto: 2500 }) },
    )
    await abrirApp(backend)

    await userEvent.click(await screen.findByRole('button', { name: 'Editar' }))

    expect(screen.getByRole('heading', { name: 'Editar movimiento' })).toBeInTheDocument()
    expect(screen.getByLabelText('Monto')).toHaveValue(1500)

    await userEvent.clear(screen.getByLabelText('Monto'))
    await userEvent.type(screen.getByLabelText('Monto'), '2500')
    await userEvent.click(screen.getByRole('button', { name: 'Guardar cambios' }))

    await waitFor(() => expect(backend.pedidosA('PUT /movimientos/:id')).toHaveLength(1))
    const pedido = backend.pedidosA('PUT /movimientos/:id')[0]
    expect(pedido.ruta).toBe('/movimientos/mov-9')
    expect(pedido.cuerpo).toMatchObject({ monto: 2500 })
  })

  it('AC-21: eliminar manda el DELETE y recarga el listado', async () => {
    const backend = backendConSesion([movimiento({ id: 'mov-9' })]).responder(
      'DELETE /movimientos/:id',
      { estado: 204 },
    )
    await abrirApp(backend)

    await userEvent.click(await screen.findByRole('button', { name: 'Eliminar' }))

    await waitFor(() => expect(backend.pedidosA('DELETE /movimientos/:id')).toHaveLength(1))
    expect(backend.pedidosA('DELETE /movimientos/:id')[0].ruta).toBe('/movimientos/mov-9')
    expect(backend.pedidosA('GET /movimientos').length).toBeGreaterThan(1)
  })
})
