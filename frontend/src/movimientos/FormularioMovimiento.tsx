import { useEffect, useState, type FormEvent } from 'react'
import {
  api,
  ErrorApi,
  type Categoria,
  type Movimiento,
  type TipoCategoria,
} from '../api/cliente'
import { hoy } from '../utiles/fechas'

type Props = {
  categorias: Categoria[]
  /** Cuando no es null, el formulario edita ese movimiento en vez de crear uno (RF-14). */
  enEdicion: Movimiento | null
  onGuardado: () => void
  onCancelar: () => void
}

/** RF-10 a RF-14: carga y edicion de un gasto o un ingreso. */
export function FormularioMovimiento({ categorias, enEdicion, onGuardado, onCancelar }: Props) {
  const [tipo, setTipo] = useState<TipoCategoria>('Gasto')
  const [monto, setMonto] = useState('')
  const [categoriaId, setCategoriaId] = useState('')
  const [fecha, setFecha] = useState(hoy)
  const [error, setError] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)

  // AC-10: el selector solo ofrece categorias del tipo que se esta cargando.
  const categoriasDelTipo = categorias.filter((c) => c.tipo === tipo)

  useEffect(() => {
    if (enEdicion) {
      setTipo(enEdicion.tipo)
      setMonto(String(enEdicion.monto))
      setCategoriaId(enEdicion.categoriaId)
      setFecha(enEdicion.fecha)
    } else {
      setMonto('')
      setCategoriaId('')
      setFecha(hoy())
    }
    setError(null)
  }, [enEdicion])

  function cambiarTipo(nuevo: TipoCategoria) {
    setTipo(nuevo)
    // La categoria elegida es de otro tipo: se limpia en vez de quedar inconsistente.
    setCategoriaId('')
  }

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    setError(null)

    if (!categoriaId) {
      setError('Elegí una categoría.')
      return
    }

    const montoNumerico = Number(monto)
    if (!monto || Number.isNaN(montoNumerico) || montoNumerico <= 0) {
      setError('El monto tiene que ser un número mayor a cero.')
      return
    }

    setEnviando(true)
    try {
      const datos = { monto: montoNumerico, fecha, categoriaId }
      await (enEdicion
        ? api.movimientos.modificar(enEdicion.id, datos)
        : api.movimientos.crear(datos))
      onGuardado()
    } catch (fallo: unknown) {
      setError(
        fallo instanceof ErrorApi ? fallo.message : 'No se pudo guardar el movimiento.',
      )
    } finally {
      setEnviando(false)
    }
  }

  return (
    // noValidate: la validacion nativa del navegador cancela el submit en silencio y
    // AC-18 pide que el rechazo muestre el motivo. Los mensajes los da el formulario.
    <form className="formulario-movimiento" onSubmit={enviar} noValidate>
      <h2>{enEdicion ? 'Editar movimiento' : 'Nuevo movimiento'}</h2>

      <div className="tipos" role="group" aria-label="Tipo de movimiento">
        <button
          type="button"
          aria-pressed={tipo === 'Gasto'}
          className={tipo === 'Gasto' ? 'activo' : ''}
          onClick={() => cambiarTipo('Gasto')}
        >
          Gasto
        </button>
        <button
          type="button"
          aria-pressed={tipo === 'Ingreso'}
          className={tipo === 'Ingreso' ? 'activo' : ''}
          onClick={() => cambiarTipo('Ingreso')}
        >
          Ingreso
        </button>
      </div>

      <div className="campos">
        <div className="campo">
          <label htmlFor="monto">Monto</label>
          <input
            id="monto"
            type="number"
            inputMode="decimal"
            step="0.01"
            min="0.01"
            value={monto}
            onChange={(evento) => setMonto(evento.target.value)}
          />
        </div>

        <div className="campo">
          <label htmlFor="categoria">Categor&iacute;a</label>
          <select
            id="categoria"
            value={categoriaId}
            onChange={(evento) => setCategoriaId(evento.target.value)}
          >
            <option value="">Eleg&iacute; una</option>
            {categoriasDelTipo.map((categoria) => (
              <option key={categoria.id} value={categoria.id}>
                {categoria.nombre}
              </option>
            ))}
          </select>
        </div>

        <div className="campo">
          <label htmlFor="fecha">Fecha</label>
          <input
            id="fecha"
            type="date"
            value={fecha}
            onChange={(evento) => setFecha(evento.target.value)}
          />
        </div>
      </div>

      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}

      <div className="acciones">
        <button type="submit" className="principal" disabled={enviando}>
          {enEdicion ? 'Guardar cambios' : 'Agregar'}
        </button>
        {enEdicion && (
          <button type="button" onClick={onCancelar}>
            Cancelar
          </button>
        )}
      </div>
    </form>
  )
}
