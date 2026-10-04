import { LoaderCircle } from 'lucide-react'
import {
  useId,
  type ButtonHTMLAttributes,
  type InputHTMLAttributes,
  type ReactNode,
  type SelectHTMLAttributes,
} from 'react'

type Variante = 'primario' | 'secundario' | 'peligro' | 'fantasma'

const estilosVariante: Record<Variante, string> = {
  primario: 'bg-emerald-600 text-white hover:bg-emerald-700 focus-visible:outline-emerald-600',
  secundario: 'bg-white text-slate-700 ring-1 ring-slate-300 hover:bg-slate-50 focus-visible:outline-slate-400',
  peligro: 'bg-rose-600 text-white hover:bg-rose-700 focus-visible:outline-rose-600',
  fantasma: 'text-slate-600 hover:bg-slate-100 hover:text-slate-900 focus-visible:outline-slate-400',
}

interface BotonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variante?: Variante
  cargando?: boolean
}

export function Boton({ variante = 'primario', cargando, disabled, className = '', children, ...resto }: BotonProps) {
  return (
    <button
      type="button"
      disabled={disabled || cargando}
      className={`inline-flex items-center justify-center gap-2 rounded-lg px-4 py-2 text-sm font-medium transition focus-visible:outline-2 focus-visible:outline-offset-2 disabled:cursor-not-allowed disabled:opacity-60 ${estilosVariante[variante]} ${className}`}
      {...resto}
    >
      {cargando && <LoaderCircle className="size-4 animate-spin" aria-hidden />}
      {children}
    </button>
  )
}

interface CampoProps extends InputHTMLAttributes<HTMLInputElement> {
  etiqueta: string
  error?: string
  ayuda?: string
}

export function Campo({ etiqueta, error, ayuda, className = '', ...resto }: CampoProps) {
  const id = useId()
  return (
    <div className={className}>
      <label htmlFor={id} className="mb-1 block text-sm font-medium text-slate-700">
        {etiqueta}
      </label>
      <input
        id={id}
        aria-invalid={!!error}
        aria-describedby={error || ayuda ? `${id}-msg` : undefined}
        className={`block w-full rounded-lg border bg-white px-3 py-2 text-sm shadow-xs outline-none transition focus:ring-2 ${
          error
            ? 'border-rose-400 focus:border-rose-500 focus:ring-rose-200'
            : 'border-slate-300 focus:border-emerald-500 focus:ring-emerald-200'
        }`}
        {...resto}
      />
      {(error || ayuda) && (
        <p id={`${id}-msg`} className={`mt-1 text-xs ${error ? 'text-rose-600' : 'text-slate-500'}`}>
          {error ?? ayuda}
        </p>
      )}
    </div>
  )
}

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  etiqueta: string
  error?: string
}

export function Select({ etiqueta, error, className = '', children, ...resto }: SelectProps) {
  const id = useId()
  return (
    <div className={className}>
      <label htmlFor={id} className="mb-1 block text-sm font-medium text-slate-700">
        {etiqueta}
      </label>
      <select
        id={id}
        aria-invalid={!!error}
        className={`block w-full rounded-lg border bg-white px-3 py-2 text-sm shadow-xs outline-none transition focus:ring-2 ${
          error
            ? 'border-rose-400 focus:border-rose-500 focus:ring-rose-200'
            : 'border-slate-300 focus:border-emerald-500 focus:ring-emerald-200'
        }`}
        {...resto}
      >
        {children}
      </select>
      {error && <p className="mt-1 text-xs text-rose-600">{error}</p>}
    </div>
  )
}

export function Alerta({ children }: { children: ReactNode }) {
  if (!children) return null
  return (
    <div role="alert" className="rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700">
      {children}
    </div>
  )
}

export function Tarjeta({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <section className={`rounded-xl border border-slate-200 bg-white p-4 shadow-xs sm:p-5 ${className}`}>{children}</section>
}

export function Cargando({ texto = 'Cargando…' }: { texto?: string }) {
  return (
    <div className="flex items-center justify-center gap-2 py-10 text-sm text-slate-500">
      <LoaderCircle className="size-5 animate-spin" aria-hidden />
      {texto}
    </div>
  )
}

export function Vacio({ titulo, children }: { titulo: string; children?: ReactNode }) {
  return (
    <div className="flex flex-col items-center gap-3 py-10 text-center">
      <p className="text-sm text-slate-500">{titulo}</p>
      {children}
    </div>
  )
}

/** Selector de dos o más opciones como botones segmentados (ej. Egreso / Ingreso). */
export function Segmentado<T extends string>({
  opciones,
  valor,
  onChange,
  etiqueta,
}: {
  opciones: { valor: T; texto: string }[]
  valor: T
  onChange: (valor: T) => void
  etiqueta: string
}) {
  return (
    <div role="radiogroup" aria-label={etiqueta} className="inline-flex rounded-lg bg-slate-100 p-1">
      {opciones.map((o) => (
        <button
          key={o.valor}
          type="button"
          role="radio"
          aria-checked={valor === o.valor}
          onClick={() => onChange(o.valor)}
          className={`rounded-md px-3 py-1.5 text-sm font-medium transition ${
            valor === o.valor ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-500 hover:text-slate-700'
          }`}
        >
          {o.texto}
        </button>
      ))}
    </div>
  )
}
