import { useState, type FormEvent } from 'react'
import { ApiError } from '../api/cliente'
import { useGuardarSubcategoria } from '../api/consultas'
import type { Categoria, Subcategoria } from '../api/tipos'
import { IconoCategoria } from './IconoCategoria'
import { Modal } from './Modal'
import { Alerta, Boton, Campo } from './ui'

interface Props {
  abierto: boolean
  onCerrar: () => void
  /** Categoría a la que pertenece (o va a pertenecer) la subcategoría. */
  categoria: Categoria | null
  /** Si se pasa, se edita esa subcategoría; si no, se crea una nueva. */
  subcategoria?: Subcategoria | null
}

export function SubcategoriaFormulario({ abierto, onCerrar, categoria, subcategoria }: Props) {
  return (
    <Modal titulo={subcategoria ? 'Editar subcategoría' : 'Nueva subcategoría'} abierto={abierto} onCerrar={onCerrar}>
      {categoria && <Formulario categoria={categoria} subcategoria={subcategoria ?? null} onListo={onCerrar} />}
    </Modal>
  )
}

function Formulario({
  categoria,
  subcategoria,
  onListo,
}: {
  categoria: Categoria
  subcategoria: Subcategoria | null
  onListo: () => void
}) {
  const guardar = useGuardarSubcategoria()
  const [nombre, setNombre] = useState(subcategoria?.nombre ?? '')
  const [error, setError] = useState<ApiError | null>(null)

  const enviar = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)
    try {
      await guardar.mutateAsync({ categoriaId: categoria.id, id: subcategoria?.id, nombre })
      onListo()
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(0, 'Ocurrió un error inesperado.'))
    }
  }

  const errorGeneral = error && Object.keys(error.errores).length === 0 ? error.message : null

  return (
    <form onSubmit={enviar} className="space-y-4" noValidate>
      <div className="flex items-center gap-2 text-sm text-slate-600">
        <IconoCategoria icono={categoria.icono} color={categoria.color} tamanio="sm" />
        <span>
          Dentro de <strong className="text-slate-800">{categoria.nombre}</strong>
        </span>
      </div>

      <Campo
        etiqueta="Nombre"
        maxLength={50}
        placeholder="Ej: Netflix"
        value={nombre}
        onChange={(e) => setNombre(e.target.value)}
        error={error?.campo('nombre')}
        autoFocus
        required
      />

      <Alerta>{errorGeneral}</Alerta>

      <div className="flex justify-end gap-2 pt-1">
        <Boton variante="secundario" onClick={onListo}>
          Cancelar
        </Boton>
        <Boton type="submit" cargando={guardar.isPending}>
          {subcategoria ? 'Guardar cambios' : 'Agregar'}
        </Boton>
      </div>
    </form>
  )
}
