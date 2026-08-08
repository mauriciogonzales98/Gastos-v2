import { ProveedorAutenticacion } from './auth/ProveedorAutenticacion'
import { Guarda } from './auth/Guarda'
import { PantallaPrincipal } from './paginas/PantallaPrincipal'
import './App.css'

function App() {
  return (
    <ProveedorAutenticacion>
      <Guarda>
        <PantallaPrincipal />
      </Guarda>
    </ProveedorAutenticacion>
  )
}

export default App
