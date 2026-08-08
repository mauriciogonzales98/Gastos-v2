import type { ReactNode } from 'react'
import { useAutenticacion } from './contexto'
import { PantallaAutenticacion } from '../paginas/PantallaAutenticacion'

/**
 * RF-03 / AC-05: sin sesion no se muestra ninguna pantalla de la aplicacion, se muestra
 * el login. Es solo la mitad visible de la proteccion: la que cuenta es la del backend,
 * que responde 401 a todo endpoint sin cookie valida.
 */
export function Guarda({ children }: { children: ReactNode }) {
  const { usuario, cargando } = useAutenticacion()

  if (cargando) {
    return <p className="cargando">Cargando...</p>
  }

  if (!usuario) {
    return <PantallaAutenticacion />
  }

  return <>{children}</>
}
