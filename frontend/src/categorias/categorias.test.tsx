import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from '../App'
import { backendConSesion, type BackendFalso } from '../pruebas/backendFalso'

afterEach(() => {
  vi.unstubAllGlobals()
})

async function abrirPanel(backend: BackendFalso) {
  render(<App />)
  await userEvent.click(await screen.findByRole('button', { name: /Mis categorías/ }))
  return backend
}

describe('ABM de categorías (RF-07 a RF-09)', () => {
  it('AC-11: solo las categorías propias se pueden renombrar o eliminar', async () => {
    await abrirPanel(backendConSesion())

    // "Mascotas" es la unica propia del catalogo de prueba.
    expect(screen.getByRole('button', { name: 'Renombrar Mascotas' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Eliminar Mascotas' })).toBeInTheDocument()

    // Las predefinidas no ofrecen ninguna accion.
    expect(screen.queryByRole('button', { name: 'Renombrar Comida' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Eliminar Comida' })).not.toBeInTheDocument()
  })

  it('AC-12: al crear una categoría propia se manda el alta y se recarga el catálogo', async () => {
    const backend = await abrirPanel(
      backendConSesion().responder('POST /categorias', { estado: 201, cuerpo: {} }),
    )

    await userEvent.type(screen.getByLabelText('Nombre'), 'Regalos')
    await userEvent.selectOptions(screen.getByLabelText('Tipo'), 'Ingreso')
    await userEvent.click(screen.getByRole('button', { name: 'Crear categoría' }))

    await waitFor(() => expect(backend.pedidosA('POST /categorias')).toHaveLength(1))
    expect(backend.pedidosA('POST /categorias')[0].cuerpo).toEqual({
      nombre: 'Regalos',
      tipo: 'Ingreso',
    })
    // El selector del formulario y el listado se rearman con el catálogo nuevo (AC-13).
    expect(backend.pedidosA('GET /categorias').length).toBeGreaterThan(1)
    expect(backend.pedidosA('GET /movimientos').length).toBeGreaterThan(1)
  })

  it('AC-13: renombrar una categoría propia manda el PUT y recarga', async () => {
    const backend = await abrirPanel(
      backendConSesion().responder('PUT /categorias/:id', { estado: 200, cuerpo: {} }),
    )

    await userEvent.click(screen.getByRole('button', { name: 'Renombrar Mascotas' }))
    const campo = screen.getByLabelText('Nuevo nombre de Mascotas')
    await userEvent.clear(campo)
    await userEvent.type(campo, 'Veterinaria')
    await userEvent.click(screen.getByRole('button', { name: 'Guardar' }))

    await waitFor(() => expect(backend.pedidosA('PUT /categorias/:id')).toHaveLength(1))
    const pedido = backend.pedidosA('PUT /categorias/:id')[0]
    expect(pedido.ruta).toBe('/categorias/cat-mascotas')
    expect(pedido.cuerpo).toEqual({ nombre: 'Veterinaria' })
  })

  it('AC-14: eliminar una categoría propia manda el DELETE y recarga el listado', async () => {
    const backend = await abrirPanel(
      backendConSesion().responder('DELETE /categorias/:id', { estado: 204 }),
    )

    await userEvent.click(screen.getByRole('button', { name: 'Eliminar Mascotas' }))

    await waitFor(() => expect(backend.pedidosA('DELETE /categorias/:id')).toHaveLength(1))
    expect(backend.pedidosA('DELETE /categorias/:id')[0].ruta).toBe('/categorias/cat-mascotas')
    // Los movimientos se recargan: los que usaban esa categoría siguen mostrando su nombre.
    expect(backend.pedidosA('GET /movimientos').length).toBeGreaterThan(1)
  })

  it('si el backend rechaza la operación, se muestra su mensaje', async () => {
    const backend = await abrirPanel(
      backendConSesion().responder('POST /categorias', {
        estado: 400,
        cuerpo: { errors: { nombre: ['El nombre es obligatorio.'] } },
      }),
    )

    await userEvent.type(screen.getByLabelText('Nombre'), 'x')
    await userEvent.click(screen.getByRole('button', { name: 'Crear categoría' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('El nombre es obligatorio.')
    expect(backend.pedidosA('POST /categorias')).toHaveLength(1)
  })
})
