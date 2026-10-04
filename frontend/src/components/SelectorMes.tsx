import { ChevronLeft, ChevronRight } from 'lucide-react'
import { mesActual, nombreMes, sumarMeses } from '../lib/formato'

export function SelectorMes({ mes, onChange }: { mes: string; onChange: (mes: string) => void }) {
  const esActual = mes === mesActual()
  return (
    <div className="flex items-center gap-1">
      <button
        type="button"
        onClick={() => onChange(sumarMeses(mes, -1))}
        className="rounded-lg p-2 text-slate-500 hover:bg-slate-100 hover:text-slate-800"
        aria-label="Mes anterior"
      >
        <ChevronLeft className="size-5" />
      </button>
      <span className="min-w-36 text-center font-medium text-slate-800">{nombreMes(mes)}</span>
      <button
        type="button"
        onClick={() => onChange(sumarMeses(mes, 1))}
        className="rounded-lg p-2 text-slate-500 hover:bg-slate-100 hover:text-slate-800"
        aria-label="Mes siguiente"
      >
        <ChevronRight className="size-5" />
      </button>
      {!esActual && (
        <button
          type="button"
          onClick={() => onChange(mesActual())}
          className="ml-1 rounded-md px-2 py-1 text-xs font-medium text-emerald-700 hover:bg-emerald-50"
        >
          Hoy
        </button>
      )}
    </div>
  )
}
