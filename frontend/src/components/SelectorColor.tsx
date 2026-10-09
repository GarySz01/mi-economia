import { Check } from 'lucide-react'
import { COLORES } from '../lib/iconos'

export function SelectorColor({ valor, onChange }: { valor: string; onChange: (color: string) => void }) {
  return (
    <fieldset>
      <legend className="mb-2 text-sm font-medium text-slate-700">Color</legend>
      <div className="flex flex-wrap gap-2">
        {COLORES.map((c) => (
          <button
            key={c}
            type="button"
            onClick={() => onChange(c)}
            className="flex size-8 items-center justify-center rounded-full ring-offset-2 transition hover:scale-110 focus-visible:ring-2 focus-visible:ring-slate-400"
            style={{ backgroundColor: c }}
            aria-label={`Color ${c}`}
            aria-pressed={valor === c}
          >
            {valor === c && <Check className="size-4 text-white" aria-hidden />}
          </button>
        ))}
      </div>
    </fieldset>
  )
}
