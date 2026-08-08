import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, ErrorApi, type Credenciales, type Usuario } from '../api/cliente'
import { ContextoAutenticacion, type EstadoAutenticacion } from './contexto'

/**
 * Mantiene quien tiene la sesion abierta. La verdad vive en la cookie httpOnly del
 * backend, que JavaScript no puede leer: al arrancar se le pregunta a /auth/me y ese
 * resultado es el que decide si se muestra la aplicacion o la pantalla de login.
 */
export function ProveedorAutenticacion({ children }: { children: ReactNode }) {
  const [usuario, setUsuario] = useState<Usuario | null>(null)
  const [cargando, setCargando] = useState(true)

  useEffect(() => {
    let cancelado = false

    api
      .sesionActual()
      .then((sesion) => {
        if (!cancelado) setUsuario(sesion)
      })
      .catch((error: unknown) => {
        // Un 401 aca es lo normal: significa que no hay sesion todavia.
        if (!cancelado && !(error instanceof ErrorApi && error.estado === 401)) {
          console.error('No se pudo consultar la sesion actual.', error)
        }
      })
      .finally(() => {
        if (!cancelado) setCargando(false)
      })

    return () => {
      cancelado = true
    }
  }, [])

  const registrar = useCallback(async (credenciales: Credenciales) => {
    setUsuario(await api.registrar(credenciales))
  }, [])

  const iniciarSesion = useCallback(async (credenciales: Credenciales) => {
    setUsuario(await api.iniciarSesion(credenciales))
  }, [])

  const cerrarSesion = useCallback(async () => {
    await api.cerrarSesion()
    setUsuario(null)
  }, [])

  const estado = useMemo<EstadoAutenticacion>(
    () => ({ usuario, cargando, registrar, iniciarSesion, cerrarSesion }),
    [usuario, cargando, registrar, iniciarSesion, cerrarSesion],
  )

  return <ContextoAutenticacion value={estado}>{children}</ContextoAutenticacion>
}
