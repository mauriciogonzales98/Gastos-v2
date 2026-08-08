import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from '../App'

type RespuestaFalsa = { estado: number; cuerpo?: unknown }

/** Cola de respuestas por ruta: cada llamada consume la primera que quedo pendiente. */
let respuestas: Record<string, RespuestaFalsa[]>

function responder(ruta: string, ...secuencia: RespuestaFalsa[]) {
  respuestas[ruta] = secuencia
}

function fetchFalso(entrada: string | URL | Request): Promise<Response> {
  const url = typeof entrada === 'string' ? entrada : entrada.toString()
  const ruta = url.replace('/api', '')
  const pendientes = respuestas[ruta]

  if (!pendientes || pendientes.length === 0) {
    throw new Error(`El test no preparo una respuesta para ${ruta}`)
  }

  // La ultima respuesta preparada se repite si vuelven a pedir la misma ruta.
  const { estado, cuerpo } = pendientes.length > 1 ? pendientes.shift()! : pendientes[0]

  return Promise.resolve(
    new Response(estado === 204 ? null : JSON.stringify(cuerpo ?? {}), {
      status: estado,
      headers: { 'Content-Type': 'application/json' },
    }),
  )
}

const USUARIO = { id: '11111111-1111-1111-1111-111111111111', email: 'ana@ejemplo.com' }
const SIN_SESION = { estado: 401, cuerpo: { title: 'Unauthorized' } }

async function entrar(email = USUARIO.email, contrasena = 'unaClaveSegura1') {
  await userEvent.type(screen.getByLabelText('Email'), email)
  await userEvent.type(screen.getByLabelText('Contrasena'), contrasena)
  await userEvent.click(screen.getByRole('button', { name: 'Entrar' }))
}

beforeEach(() => {
  respuestas = {}
  vi.stubGlobal('fetch', vi.fn(fetchFalso))
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('guarda de rutas (RF-03, AC-05)', () => {
  it('sin sesion muestra el login y no la aplicacion', async () => {
    responder('/auth/me', SIN_SESION)

    render(<App />)

    expect(await screen.findByRole('button', { name: 'Entrar' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Cerrar sesion' })).not.toBeInTheDocument()
  })

  it('con sesion activa entra directo a la aplicacion', async () => {
    responder('/auth/me', { estado: 200, cuerpo: USUARIO })

    render(<App />)

    expect(await screen.findByText(USUARIO.email)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Entrar' })).not.toBeInTheDocument()
  })
})

describe('inicio de sesion (RF-02)', () => {
  it('AC-03: con credenciales correctas entra a la pantalla principal', async () => {
    responder('/auth/me', SIN_SESION)
    responder('/auth/login', { estado: 200, cuerpo: USUARIO })

    render(<App />)
    await screen.findByRole('button', { name: 'Entrar' })

    await entrar()

    expect(await screen.findByRole('button', { name: 'Cerrar sesion' })).toBeInTheDocument()
    expect(screen.getByText(USUARIO.email)).toBeInTheDocument()
  })

  it('AC-04: con credenciales incorrectas muestra el motivo y no entra', async () => {
    responder('/auth/me', SIN_SESION)
    responder('/auth/login', {
      estado: 401,
      cuerpo: { detail: 'El email o la contrasena no son correctos.' },
    })

    render(<App />)
    await screen.findByRole('button', { name: 'Entrar' })

    await entrar(USUARIO.email, 'claveEquivocada9')

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'El email o la contrasena no son correctos.',
    )
    expect(screen.queryByRole('button', { name: 'Cerrar sesion' })).not.toBeInTheDocument()
  })
})

describe('alta de cuenta (RF-01)', () => {
  it('AC-01: al crear la cuenta queda con la sesion iniciada', async () => {
    responder('/auth/me', SIN_SESION)
    responder('/auth/register', { estado: 201, cuerpo: USUARIO })

    render(<App />)
    await userEvent.click(await screen.findByRole('tab', { name: 'Crear cuenta' }))

    await userEvent.type(screen.getByLabelText('Email'), USUARIO.email)
    await userEvent.type(screen.getByLabelText('Contrasena'), 'unaClaveSegura1')
    await userEvent.click(screen.getByRole('button', { name: 'Crear cuenta' }))

    expect(await screen.findByRole('button', { name: 'Cerrar sesion' })).toBeInTheDocument()
  })

  it('AC-02: un email ya registrado muestra el rechazo y no entra', async () => {
    responder('/auth/me', SIN_SESION)
    responder('/auth/register', {
      estado: 409,
      cuerpo: { detail: 'Ya existe una cuenta con ese email.' },
    })

    render(<App />)
    await userEvent.click(await screen.findByRole('tab', { name: 'Crear cuenta' }))

    await userEvent.type(screen.getByLabelText('Email'), USUARIO.email)
    await userEvent.type(screen.getByLabelText('Contrasena'), 'unaClaveSegura1')
    await userEvent.click(screen.getByRole('button', { name: 'Crear cuenta' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Ya existe una cuenta con ese email.')
    expect(screen.queryByRole('button', { name: 'Cerrar sesion' })).not.toBeInTheDocument()
  })
})

describe('cierre de sesion (RF-05)', () => {
  it('AC-09: al cerrar sesion vuelve a la pantalla de login', async () => {
    responder('/auth/me', { estado: 200, cuerpo: USUARIO })
    responder('/auth/logout', { estado: 204 })

    render(<App />)
    await userEvent.click(await screen.findByRole('button', { name: 'Cerrar sesion' }))

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Entrar' })).toBeInTheDocument()
    })
    expect(screen.queryByText(USUARIO.email)).not.toBeInTheDocument()
  })
})
