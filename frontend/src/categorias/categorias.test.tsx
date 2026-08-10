import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from '../App'
import { backendConSesion, type BackendFalso } from '../pruebas/backendFalso'

afterEach(() => {
  vi.unstubAllGlobals()
})

async function abrirPanel(backend: BackendFalso) {
  render(<App />)
  await userEvent.click(await screen.findByRole('tab', { name: 'Categorías' }))
  await screen.findByRole('heading', { name: /Mis categorías/ })
  return backend
}

/**
 * El renglon de una categoria. Los botones dicen solo la accion ("Renombrar", no
 * "Renombrar Mascotas"), asi que se los busca adentro de su fila y no por un nombre
 * inflado a proposito para el test.
 */
function filaDe(nombre: string): HTMLElement {
  const fila = screen
    .getAllByRole('listitem')
    .find((elemento) => elemento.textContent?.startsWith(nombre))

  if (!fila) {
    throw new Error(`No hay ninguna fila para la categoría "${nombre}".`)
  }

  return fila
}

describe('ABM de categorías (RF-07 a RF-09)', () => {
  it('AC-11: solo las categorías propias se pueden renombrar o eliminar', async () => {
    await abrirPanel(backendConSesion())

    // "Mascotas" es la unica propia del catalogo de prueba: las predefinidas no listan.
    expect(screen.getAllByRole('listitem')).toHaveLength(1)

    const mascotas = filaDe('Mascotas')
    expect(within(mascotas).getByRole('button', { name: 'Renombrar' })).toBeInTheDocument()
    expect(within(mascotas).getByRole('button', { name: 'Eliminar' })).toBeInTheDocument()

    // El nombre de la categoria acompaña al boton como descripcion, no como rotulo: el
    // boton dice lo que hace y de que fila es lo dice el `aria-describedby`.
    expect(within(mascotas).getByRole('button', { name: 'Eliminar' })).toHaveAccessibleDescription(
      /Mascotas/,
    )
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

    await userEvent.click(within(filaDe('Mascotas')).getByRole('button', { name: 'Renombrar' }))
    const campo = within(filaDe('Mascotas')).getByLabelText('Nuevo nombre')
    await userEvent.clear(campo)
    await userEvent.type(campo, 'Veterinaria')
    await userEvent.click(within(filaDe('Mascotas')).getByRole('button', { name: 'Guardar' }))

    await waitFor(() => expect(backend.pedidosA('PUT /categorias/:id')).toHaveLength(1))
    const pedido = backend.pedidosA('PUT /categorias/:id')[0]
    expect(pedido.ruta).toBe('/categorias/cat-mascotas')
    expect(pedido.cuerpo).toEqual({ nombre: 'Veterinaria' })
  })

  it('AC-14: eliminar una categoría propia manda el DELETE y recarga el listado', async () => {
    const backend = await abrirPanel(
      backendConSesion().responder('DELETE /categorias/:id', { estado: 204 }),
    )

    await userEvent.click(within(filaDe('Mascotas')).getByRole('button', { name: 'Eliminar' }))

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

describe('navegación por pestañas', () => {
  it('cada sección vive en su pestaña y solo se ve la activa', async () => {
    backendConSesion()
    render(<App />)
    await screen.findByRole('heading', { name: 'Nuevo movimiento' })

    // Al abrir, la pestaña de trabajo diario.
    expect(screen.getByRole('tab', { name: 'Movimientos' })).toHaveAttribute(
      'aria-selected',
      'true',
    )
    expect(screen.queryByRole('heading', { name: /Mis categorías/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Dashboard' })).not.toBeInTheDocument()

    await userEvent.click(screen.getByRole('tab', { name: 'Categorías' }))

    expect(await screen.findByRole('heading', { name: /Mis categorías/ })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Nuevo movimiento' })).not.toBeInTheDocument()

    await userEvent.click(screen.getByRole('tab', { name: 'Dashboard' }))

    expect(await screen.findByRole('heading', { name: 'Dashboard' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: /Mis categorías/ })).not.toBeInTheDocument()
  })

  it('se puede moverse entre pestañas con las flechas', async () => {
    backendConSesion()
    render(<App />)
    await screen.findByRole('heading', { name: 'Nuevo movimiento' })

    screen.getByRole('tab', { name: 'Movimientos' }).focus()
    await userEvent.keyboard('{ArrowRight}')

    expect(screen.getByRole('tab', { name: 'Dashboard' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tab', { name: 'Dashboard' })).toHaveFocus()

    // Da la vuelta: desde la última, la flecha derecha lleva a la primera.
    await userEvent.keyboard('{ArrowRight}{ArrowRight}')

    expect(screen.getByRole('tab', { name: 'Movimientos' })).toHaveAttribute(
      'aria-selected',
      'true',
    )
  })
})
