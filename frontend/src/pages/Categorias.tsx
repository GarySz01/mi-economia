import { Pencil, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useCategorias, useEliminarCategoria } from '../api/consultas'
import type { Categoria, TipoMovimiento } from '../api/tipos'
import { CategoriaFormulario } from '../components/CategoriaFormulario'
import { Confirmar } from '../components/Confirmar'
import { Alerta, Cargando, Tarjeta, Vacio } from '../components/ui'
import { IconoCategoria } from '../components/IconoCategoria'

export function Categorias() {
  const { data: categorias = [], isLoading, isError, error } = useCategorias()
  const eliminar = useEliminarCategoria()

  const [formulario, setFormulario] = useState<{
    abierto: boolean
    categoria: Categoria | null
    tipo: TipoMovimiento
  }>({ abierto: false, categoria: null, tipo: 'Egreso' })
  const [aEliminar, setAEliminar] = useState<Categoria | null>(null)

  const nueva = (tipo: TipoMovimiento) => setFormulario({ abierto: true, categoria: null, tipo })
  const editar = (categoria: Categoria) => setFormulario({ abierto: true, categoria, tipo: categoria.tipo })

  return (
    <div className="space-y-5">
      <h1 className="text-2xl font-semibold text-slate-900">Categorías</h1>
      {isError && <Alerta>{error.message}</Alerta>}

      {isLoading ? (
        <Cargando />
      ) : (
        <div className="grid gap-4 md:grid-cols-2">
          {(['Egreso', 'Ingreso'] as const).map((tipo) => {
            const lista = categorias
              .filter((c) => c.tipo === tipo)
              .sort((a, b) => a.nombre.localeCompare(b.nombre, 'es'))
            return (
              <Tarjeta key={tipo}>
                <div className="mb-2 flex items-center justify-between">
                  <h2 className="font-semibold text-slate-900">{tipo === 'Egreso' ? 'Gastos' : 'Ingresos'}</h2>
                  <button
                    type="button"
                    onClick={() => nueva(tipo)}
                    className="flex items-center gap-1 rounded-lg px-2 py-1 text-sm font-medium text-emerald-700 hover:bg-emerald-50"
                  >
                    <Plus className="size-4" aria-hidden />
                    Nueva
                  </button>
                </div>
                {lista.length === 0 ? (
                  <Vacio titulo="No hay categorías." />
                ) : (
                  <ul className="divide-y divide-slate-100">
                    {lista.map((c) => (
                      <li key={c.id} className="flex items-center gap-3 py-2.5">
                        <IconoCategoria icono={c.icono} color={c.color} tamanio="sm" />
                        <span className="flex-1 truncate text-sm font-medium text-slate-700">{c.nombre}</span>
                        <button
                          type="button"
                          onClick={() => editar(c)}
                          className="rounded-md p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
                          aria-label={`Editar ${c.nombre}`}
                        >
                          <Pencil className="size-4" />
                        </button>
                        <button
                          type="button"
                          onClick={() => setAEliminar(c)}
                          className="rounded-md p-1.5 text-slate-400 hover:bg-rose-50 hover:text-rose-600"
                          aria-label={`Eliminar ${c.nombre}`}
                        >
                          <Trash2 className="size-4" />
                        </button>
                      </li>
                    ))}
                  </ul>
                )}
              </Tarjeta>
            )
          })}
        </div>
      )}

      <CategoriaFormulario
        abierto={formulario.abierto}
        categoria={formulario.categoria}
        tipoInicial={formulario.tipo}
        onCerrar={() => setFormulario((f) => ({ ...f, abierto: false, categoria: null }))}
      />

      <Confirmar
        titulo="Eliminar categoría"
        abierto={aEliminar !== null}
        onCerrar={() => setAEliminar(null)}
        onConfirmar={() => eliminar.mutateAsync(aEliminar!.id)}
      >
        ¿Seguro que quieres eliminar la categoría <strong className="text-slate-800">{aEliminar?.nombre}</strong>?
        Si tiene movimientos no se podrá eliminar.
      </Confirmar>
    </div>
  )
}
