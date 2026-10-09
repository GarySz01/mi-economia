import { useState, type FormEvent } from 'react'
import { ApiError } from '../api/cliente'
import { useGuardarCategoria } from '../api/consultas'
import type { Categoria, TipoMovimiento } from '../api/tipos'
import { COLORES, ICONOS } from '../lib/iconos'
import { IconoCategoria } from './IconoCategoria'
import { Modal } from './Modal'
import { SelectorColor } from './SelectorColor'
import { Alerta, Boton, Campo, Segmentado } from './ui'

interface Props {
  abierto: boolean
  onCerrar: () => void
  /** Si se pasa, se edita esa categoría; si no, se crea una nueva del tipo indicado. */
  categoria?: Categoria | null
  tipoInicial?: TipoMovimiento
}

export function CategoriaFormulario({ abierto, onCerrar, categoria, tipoInicial = 'Egreso' }: Props) {
  return (
    <Modal titulo={categoria ? 'Editar categoría' : 'Nueva categoría'} abierto={abierto} onCerrar={onCerrar}>
      <Formulario categoria={categoria ?? null} tipoInicial={tipoInicial} onListo={onCerrar} />
    </Modal>
  )
}

function Formulario({
  categoria,
  tipoInicial,
  onListo,
}: {
  categoria: Categoria | null
  tipoInicial: TipoMovimiento
  onListo: () => void
}) {
  const guardar = useGuardarCategoria()
  const [nombre, setNombre] = useState(categoria?.nombre ?? '')
  const [tipo, setTipo] = useState<TipoMovimiento>(categoria?.tipo ?? tipoInicial)
  const [color, setColor] = useState(categoria?.color ?? COLORES[0])
  const [icono, setIcono] = useState(categoria?.icono ?? 'tag')
  const [error, setError] = useState<ApiError | null>(null)

  const enviar = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)
    try {
      await guardar.mutateAsync({ id: categoria?.id, datos: { nombre, tipo, color, icono } })
      onListo()
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(0, 'Ocurrió un error inesperado.'))
    }
  }

  const errorGeneral = error && Object.keys(error.errores).length === 0 ? error.message : null

  return (
    <form onSubmit={enviar} className="space-y-4" noValidate>
      <div className="flex items-center gap-3">
        <IconoCategoria icono={icono} color={color} />
        <Campo
          className="flex-1"
          etiqueta="Nombre"
          maxLength={50}
          value={nombre}
          onChange={(e) => setNombre(e.target.value)}
          error={error?.campo('nombre')}
          autoFocus
          required
        />
      </div>

      {categoria ? (
        <p className="text-xs text-slate-500">
          Categoría de {categoria.tipo === 'Egreso' ? 'gastos' : 'ingresos'}. El tipo no se puede cambiar.
        </p>
      ) : (
        <Segmentado
          etiqueta="Tipo de categoría"
          valor={tipo}
          onChange={setTipo}
          opciones={[
            { valor: 'Egreso', texto: 'Gasto' },
            { valor: 'Ingreso', texto: 'Ingreso' },
          ]}
        />
      )}

      <SelectorColor valor={color} onChange={setColor} />

      <fieldset>
        <legend className="mb-2 text-sm font-medium text-slate-700">Ícono</legend>
        <div className="grid max-h-40 grid-cols-8 gap-1 overflow-y-auto rounded-lg border border-slate-200 p-2">
          {Object.entries(ICONOS).map(([nombreIcono, Icono]) => (
            <button
              key={nombreIcono}
              type="button"
              onClick={() => setIcono(nombreIcono)}
              className={`flex aspect-square items-center justify-center rounded-md transition ${
                icono === nombreIcono ? 'text-white' : 'text-slate-500 hover:bg-slate-100'
              }`}
              style={icono === nombreIcono ? { backgroundColor: color } : undefined}
              aria-label={nombreIcono}
              aria-pressed={icono === nombreIcono}
            >
              <Icono className="size-5" aria-hidden />
            </button>
          ))}
        </div>
      </fieldset>

      <Alerta>{errorGeneral ?? error?.campo('color') ?? error?.campo('icono') ?? error?.campo('tipo')}</Alerta>

      <div className="flex justify-end gap-2 pt-1">
        <Boton variante="secundario" onClick={onListo}>
          Cancelar
        </Boton>
        <Boton type="submit" cargando={guardar.isPending}>
          {categoria ? 'Guardar cambios' : 'Crear categoría'}
        </Boton>
      </div>
    </form>
  )
}
