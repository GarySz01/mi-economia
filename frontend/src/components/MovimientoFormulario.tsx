import { useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { ApiError } from '../api/cliente'
import { useCategorias, useCuentas, useGuardarMovimiento } from '../api/consultas'
import type { Movimiento, TipoMovimiento } from '../api/tipos'
import { hoyIso, montoParaEditar, parsearMonto } from '../lib/formato'
import { Modal } from './Modal'
import { Alerta, Boton, Campo, Segmentado, Select } from './ui'

interface Props {
  abierto: boolean
  onCerrar: () => void
  /** Si se pasa, se edita ese movimiento; si no, se crea uno nuevo. */
  movimiento?: Movimiento | null
  /** Cuenta elegida de entrada al crear (por ejemplo, la del filtro de la página). Si no, la primera. */
  cuentaInicial?: string
}

export function MovimientoFormulario({ abierto, onCerrar, movimiento, cuentaInicial }: Props) {
  return (
    <Modal titulo={movimiento ? 'Editar movimiento' : 'Nuevo movimiento'} abierto={abierto} onCerrar={onCerrar}>
      {/* El formulario se monta de nuevo cada vez que se abre, así arranca limpio. */}
      <Formulario movimiento={movimiento ?? null} cuentaInicial={cuentaInicial} onListo={onCerrar} />
    </Modal>
  )
}

function Formulario({
  movimiento,
  cuentaInicial,
  onListo,
}: {
  movimiento: Movimiento | null
  cuentaInicial?: string
  onListo: () => void
}) {
  const { data: categorias = [], isLoading } = useCategorias()
  const { data: cuentas = [], isLoading: cargandoCuentas } = useCuentas()
  const guardar = useGuardarMovimiento()

  const [tipo, setTipo] = useState<TipoMovimiento>(movimiento?.categoria.tipo ?? 'Egreso')
  const [categoriaId, setCategoriaId] = useState(movimiento?.categoria.id ?? '')
  // Vacío = todavía no se eligió: se usa la cuenta inicial o la primera (las cuentas pueden llegar después).
  const [cuentaElegida, setCuentaElegida] = useState(movimiento?.cuenta.id ?? cuentaInicial ?? '')
  const [monto, setMonto] = useState(movimiento ? montoParaEditar(movimiento.monto) : '')
  const [fecha, setFecha] = useState(movimiento?.fecha ?? hoyIso())
  const [nota, setNota] = useState(movimiento?.nota ?? '')
  const [error, setError] = useState<ApiError | null>(null)

  const disponibles = categorias.filter((c) => c.tipo === tipo)
  const cuentaId = cuentaElegida || (cuentas[0]?.id ?? '')

  const cambiarTipo = (nuevo: TipoMovimiento) => {
    setTipo(nuevo)
    setCategoriaId('')
  }

  const enviar = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)

    const valor = parsearMonto(monto)
    if (Number.isNaN(valor)) {
      setError(new ApiError(400, '', { monto: ['Escribe un monto válido, por ejemplo 1500 o 1.500,50.'] }))
      return
    }

    try {
      await guardar.mutateAsync({
        id: movimiento?.id,
        datos: { categoriaId, cuentaId, monto: valor, fecha, nota: nota.trim() || null },
      })
      onListo()
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(0, 'Ocurrió un error inesperado.'))
    }
  }

  // El mensaje general se muestra solo si no hay errores por campo que ya lo expliquen.
  const errorGeneral = error && Object.keys(error.errores).length === 0 ? error.message : null

  return (
    <form onSubmit={enviar} className="space-y-4" noValidate>
      <Segmentado
        etiqueta="Tipo de movimiento"
        valor={tipo}
        onChange={cambiarTipo}
        opciones={[
          { valor: 'Egreso', texto: 'Gasto' },
          { valor: 'Ingreso', texto: 'Ingreso' },
        ]}
      />

      <Campo
        etiqueta="Monto"
        inputMode="decimal"
        autoComplete="off"
        placeholder="0,00"
        value={monto}
        onChange={(e) => setMonto(e.target.value)}
        error={error?.campo('monto')}
        autoFocus
        required
      />

      <Select
        etiqueta="Categoría"
        value={categoriaId}
        onChange={(e) => setCategoriaId(e.target.value)}
        error={error?.campo('categoriaId')}
        disabled={isLoading}
        required
      >
        <option value="">{isLoading ? 'Cargando…' : 'Elige una categoría'}</option>
        {disponibles.map((c) => (
          <option key={c.id} value={c.id}>
            {c.nombre}
          </option>
        ))}
      </Select>
      {!isLoading && disponibles.length === 0 && (
        <p className="-mt-2 text-xs text-slate-500">
          No tienes categorías de {tipo === 'Egreso' ? 'gastos' : 'ingresos'}.{' '}
          <Link to="/categorias" className="font-medium text-emerald-700 hover:underline">
            Crear una
          </Link>
        </p>
      )}

      <Select
        etiqueta={tipo === 'Egreso' ? 'Pagado con' : 'Ingresó en'}
        value={cuentaId}
        onChange={(e) => setCuentaElegida(e.target.value)}
        error={error?.campo('cuentaId')}
        disabled={cargandoCuentas}
        required
      >
        <option value="">{cargandoCuentas ? 'Cargando…' : 'Elige una cuenta'}</option>
        {cuentas.map((c) => (
          <option key={c.id} value={c.id}>
            {c.nombre}
          </option>
        ))}
      </Select>
      {!cargandoCuentas && cuentas.length === 0 && (
        <p className="-mt-2 text-xs text-slate-500">
          No tienes cuentas.{' '}
          <Link to="/cuentas" className="font-medium text-emerald-700 hover:underline">
            Crear una
          </Link>
        </p>
      )}

      <Campo
        etiqueta="Fecha"
        type="date"
        value={fecha}
        onChange={(e) => setFecha(e.target.value)}
        error={error?.campo('fecha')}
        required
      />

      <Campo
        etiqueta="Nota (opcional)"
        maxLength={250}
        placeholder="Ej: compra del supermercado"
        value={nota}
        onChange={(e) => setNota(e.target.value)}
        error={error?.campo('nota')}
      />

      <Alerta>{errorGeneral}</Alerta>

      <div className="flex justify-end gap-2 pt-1">
        <Boton variante="secundario" onClick={onListo}>
          Cancelar
        </Boton>
        <Boton type="submit" cargando={guardar.isPending}>
          {movimiento ? 'Guardar cambios' : 'Agregar'}
        </Boton>
      </div>
    </form>
  )
}
