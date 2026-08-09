import { useEffect, useState, type FormEvent } from 'react'
import {
  api,
  ErrorApi,
  type Categoria,
  type CodigoMoneda,
  type Moneda,
  type Movimiento,
  type TipoCategoria,
} from '../api/cliente'
import { hoy } from '../utiles/fechas'

/** RF-33. Tiene que coincidir con `Movimiento.LargoMaximoDescripcion` del backend. */
const LARGO_MAXIMO_DESCRIPCION = 120

type Props = {
  categorias: Categoria[]
  monedas: Moneda[]
  /** Cuando no es null, el formulario edita ese movimiento en vez de crear uno (RF-14). */
  enEdicion: Movimiento | null
  onGuardado: () => void
  onCancelar: () => void
}

/** RF-10 a RF-14: carga y edicion de un gasto o un ingreso. */
export function FormularioMovimiento({
  categorias,
  monedas,
  enEdicion,
  onGuardado,
  onCancelar,
}: Props) {
  // RF-25 / AC-38: la predeterminada la decide el catálogo, no el frontend.
  const codigoPorDefecto = monedas.find((m) => m.esPredeterminada)?.codigo ?? ''
  const [tipo, setTipo] = useState<TipoCategoria>('Gasto')
  const [monto, setMonto] = useState('')
  const [moneda, setMoneda] = useState<CodigoMoneda>('')
  const [categoriaId, setCategoriaId] = useState('')
  const [fecha, setFecha] = useState(hoy)
  // RF-33: nota opcional. En el estado es siempre string; el null lo arma el envío.
  const [descripcion, setDescripcion] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)

  // AC-10: el selector solo ofrece categorias del tipo que se esta cargando.
  const categoriasDelTipo = categorias.filter((c) => c.tipo === tipo)

  useEffect(() => {
    if (enEdicion) {
      setTipo(enEdicion.tipo)
      setMonto(String(enEdicion.monto))
      setMoneda(enEdicion.moneda)
      setCategoriaId(enEdicion.categoriaId)
      setFecha(enEdicion.fecha)
      setDescripcion(enEdicion.descripcion ?? '')
    } else {
      setMonto('')
      setCategoriaId('')
      setFecha(hoy())
      setDescripcion('')
    }
    setError(null)
  }, [enEdicion])

  // El catálogo llega después del primer render, así que la moneda por defecto se
  // aplica cuando está disponible y sólo mientras no haya una elegida.
  useEffect(() => {
    setMoneda((actual) => actual || codigoPorDefecto)
  }, [codigoPorDefecto])

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

    // RF-33 / AC-52. Se valida acá además del backend para que el motivo se vea sin
    // esperar el viaje al servidor. El límite real lo sigue imponiendo la API.
    const nota = descripcion.trim()
    if (nota.length > LARGO_MAXIMO_DESCRIPCION) {
      setError(`La nota admite hasta ${LARGO_MAXIMO_DESCRIPCION} caracteres.`)
      return
    }

    setEnviando(true)
    try {
      // AC-51: sin nota se manda null, no cadena vacía.
      const datos = { monto: montoNumerico, moneda, fecha, categoriaId, descripcion: nota || null }
      if (enEdicion) {
        await api.movimientos.modificar(enEdicion.id, datos)
      } else {
        await api.movimientos.crear(datos)
        // Alta: monto y nota se limpian para poder cargar el siguiente sin borrar a mano.
        // Al editar no hace falta, porque salir de edicion ya vuelve a dejar el
        // formulario en blanco (efecto sobre `enEdicion`).
        setMonto('')
        setDescripcion('')
      }
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
          <label htmlFor="moneda">Moneda</label>
          <select id="moneda" value={moneda} onChange={(evento) => setMoneda(evento.target.value)}>
            {monedas.map((opcion) => (
              <option key={opcion.codigo} value={opcion.codigo}>
                {opcion.nombre}
              </option>
            ))}
          </select>
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

        {/*
          RF-33: va al final y dice "opcional" en el propio label, porque el camino rapido
          de carga es monto + categoria y la nota no tiene que frenarlo.
        */}
        <div className="campo campo-ancho">
          <label htmlFor="descripcion">Nota (opcional)</label>
          <input
            id="descripcion"
            type="text"
            placeholder="Ej: alquiler agosto"
            value={descripcion}
            onChange={(evento) => setDescripcion(evento.target.value)}
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
