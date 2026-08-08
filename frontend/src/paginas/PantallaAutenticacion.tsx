import { useState, type FormEvent } from 'react'
import { ErrorApi } from '../api/cliente'
import { useAutenticacion } from '../auth/contexto'

type Modo = 'login' | 'registro'

const LARGO_MINIMO_CONTRASENA = 8

/** Pantalla unica de inicio de sesion (RF-02) y alta de cuenta (RF-01). */
export function PantallaAutenticacion() {
  const { iniciarSesion, registrar } = useAutenticacion()

  const [modo, setModo] = useState<Modo>('login')
  const [email, setEmail] = useState('')
  const [contrasena, setContrasena] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)

  const esRegistro = modo === 'registro'

  function cambiarModo(nuevo: Modo) {
    setModo(nuevo)
    setError(null)
  }

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    setError(null)
    setEnviando(true)

    try {
      await (esRegistro ? registrar({ email, contrasena }) : iniciarSesion({ email, contrasena }))
    } catch (fallo: unknown) {
      setError(
        fallo instanceof ErrorApi
          ? fallo.message
          : 'No se pudo conectar con el servidor. Intentalo de nuevo.',
      )
    } finally {
      setEnviando(false)
    }
  }

  return (
    <main className="pantalla-auth">
      <form className="tarjeta" onSubmit={enviar}>
        <h1>Gestion de Gastos</h1>

        <div className="modos" role="tablist">
          <button
            type="button"
            role="tab"
            aria-selected={!esRegistro}
            className={!esRegistro ? 'activo' : ''}
            onClick={() => cambiarModo('login')}
          >
            Iniciar sesion
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={esRegistro}
            className={esRegistro ? 'activo' : ''}
            onClick={() => cambiarModo('registro')}
          >
            Crear cuenta
          </button>
        </div>

        <label htmlFor="email">Email</label>
        <input
          id="email"
          name="email"
          type="email"
          autoComplete="email"
          required
          value={email}
          onChange={(evento) => setEmail(evento.target.value)}
        />

        <label htmlFor="contrasena">Contrasena</label>
        <input
          id="contrasena"
          name="contrasena"
          type="password"
          autoComplete={esRegistro ? 'new-password' : 'current-password'}
          required
          minLength={esRegistro ? LARGO_MINIMO_CONTRASENA : undefined}
          value={contrasena}
          onChange={(evento) => setContrasena(evento.target.value)}
        />
        {esRegistro && (
          <p className="ayuda">Al menos {LARGO_MINIMO_CONTRASENA} caracteres.</p>
        )}

        {error && (
          <p className="error" role="alert">
            {error}
          </p>
        )}

        <button type="submit" className="principal" disabled={enviando}>
          {esRegistro ? 'Crear cuenta' : 'Entrar'}
        </button>
      </form>
    </main>
  )
}
