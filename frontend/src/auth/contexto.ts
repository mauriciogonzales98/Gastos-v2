import { createContext, useContext } from 'react'
import type { Credenciales, Usuario } from '../api/cliente'

export type EstadoAutenticacion = {
  /** null = no hay sesion. Mientras `cargando` es true todavia no se sabe. */
  usuario: Usuario | null
  /** true hasta que se resuelve la consulta inicial a /auth/me. */
  cargando: boolean
  registrar: (credenciales: Credenciales) => Promise<void>
  iniciarSesion: (credenciales: Credenciales) => Promise<void>
  cerrarSesion: () => Promise<void>
}

export const ContextoAutenticacion = createContext<EstadoAutenticacion | null>(null)

export function useAutenticacion(): EstadoAutenticacion {
  const estado = useContext(ContextoAutenticacion)

  if (!estado) {
    throw new Error('useAutenticacion se tiene que usar dentro de <ProveedorAutenticacion>.')
  }

  return estado
}
