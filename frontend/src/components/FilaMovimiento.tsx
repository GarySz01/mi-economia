import { Pencil, Trash2 } from 'lucide-react'
import type { Movimiento } from '../api/tipos'
import { formatFecha, formatMonto } from '../lib/formato'
import { IconoCategoria } from './IconoCategoria'

interface Props {
  movimiento: Movimiento
  onEditar: (m: Movimiento) => void
  onEliminar?: (m: Movimiento) => void
  /** Ocultar la fecha cuando la lista ya está agrupada por día. */
  sinFecha?: boolean
}

export function FilaMovimiento({ movimiento: m, onEditar, onEliminar, sinFecha }: Props) {
  const esIngreso = m.categoria.tipo === 'Ingreso'
  return (
    <li className="group flex items-center gap-3 py-3">
      <button
        type="button"
        onClick={() => onEditar(m)}
        className="flex min-w-0 flex-1 items-center gap-3 rounded-lg text-left focus-visible:outline-2 focus-visible:outline-emerald-600"
      >
        <IconoCategoria icono={m.categoria.icono} color={m.categoria.color} />
        <span className="min-w-0 flex-1">
          <span className="block truncate text-sm font-medium text-slate-800">{m.nota || m.categoria.nombre}</span>
          <span className="block truncate text-xs text-slate-500">
            {m.nota ? m.categoria.nombre : null}
            {m.nota && !sinFecha ? ' · ' : null}
            {sinFecha ? null : formatFecha(m.fecha)}
          </span>
        </span>
        <span className={`shrink-0 text-sm font-semibold tabular-nums ${esIngreso ? 'text-emerald-600' : 'text-slate-800'}`}>
          {esIngreso ? '+' : '-'}
          {formatMonto(m.monto)}
        </span>
      </button>
      <div className="flex shrink-0 gap-0.5 sm:opacity-0 sm:transition sm:group-focus-within:opacity-100 sm:group-hover:opacity-100">
        <button
          type="button"
          onClick={() => onEditar(m)}
          className="hidden rounded-md p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-700 sm:block"
          aria-label="Editar"
        >
          <Pencil className="size-4" />
        </button>
        {onEliminar && (
          <button
            type="button"
            onClick={() => onEliminar(m)}
            className="rounded-md p-1.5 text-slate-400 hover:bg-rose-50 hover:text-rose-600"
            aria-label="Eliminar"
          >
            <Trash2 className="size-4" />
          </button>
        )}
      </div>
    </li>
  )
}
