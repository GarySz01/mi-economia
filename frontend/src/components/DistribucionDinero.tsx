import { Link } from 'react-router'
import { useCuentas } from '../api/consultas'
import type { Cuenta, Moneda } from '../api/tipos'
import { formatMonto } from '../lib/formato'
import { COLOR_POR_DEFECTO } from '../lib/iconos'
import { MONEDAS, sumarPorMoneda } from '../lib/monedas'
import { IconoCuenta } from './IconoCuenta'
import { Tarjeta } from './ui'

/**
 * Dónde está el dinero hoy: para cada moneda, cuánto hay en cada cuenta y, dentro de ella, en cada subcuenta.
 * No depende del mes elegido. Los porcentajes son sobre el total positivo de la moneda (las deudas no restan).
 */
export function DistribucionDinero() {
  const { data: cuentas } = useCuentas()
  if (!cuentas?.length) return null

  const monedas = sumarPorMoneda(cuentas.flatMap((c) => c.subcuentas))

  return (
    <Tarjeta>
      <div className="mb-3 flex flex-wrap items-baseline justify-between gap-2">
        <h2 className="font-semibold text-slate-900">¿Dónde está tu dinero?</h2>
        <Link to="/cuentas" className="text-sm font-medium text-emerald-700 hover:underline">
          Ver cuentas
        </Link>
      </div>
      <div className="grid gap-6 lg:grid-cols-2">
        {monedas.map((m) => (
          <PorMoneda key={m.moneda} cuentas={cuentas} moneda={m.moneda} total={m.saldo} />
        ))}
      </div>
    </Tarjeta>
  )
}

function PorMoneda({ cuentas, moneda, total }: { cuentas: Cuenta[]; moneda: Moneda; total: number }) {
  const filas = cuentas
    .map((c) => {
      const subcuentas = c.subcuentas.filter((s) => s.moneda === moneda).sort((a, b) => b.saldo - a.saldo)
      return { cuenta: c, subcuentas, saldo: subcuentas.reduce((suma, s) => suma + s.saldo, 0) }
    })
    .filter((f) => f.subcuentas.length > 0)
    .sort((a, b) => b.saldo - a.saldo)
  const positivo = filas.flatMap((f) => f.subcuentas).reduce((suma, s) => suma + Math.max(s.saldo, 0), 0)
  const porcentaje = (saldo: number) => (positivo > 0 && saldo > 0 ? (saldo * 100) / positivo : 0)

  return (
    <section>
      <div className="mb-2 flex items-baseline justify-between gap-2 border-b border-slate-100 pb-1">
        <h3 className="text-sm font-medium text-slate-500">{MONEDAS.find((x) => x.moneda === moneda)?.nombre ?? moneda}</h3>
        <span className={`text-base font-semibold tabular-nums ${total < 0 ? 'text-rose-600' : 'text-slate-900'}`}>
          {formatMonto(total, moneda)}
        </span>
      </div>
      <ul className="space-y-3">
        {filas.map(({ cuenta, subcuentas, saldo }) => (
          <li key={cuenta.id}>
            <div className="flex items-center gap-3">
              <IconoCuenta cuenta={cuenta} tamanio="sm" />
              <div className="min-w-0 flex-1">
                <div className="flex items-baseline justify-between gap-2 text-sm">
                  <span className="truncate font-medium text-slate-700">{cuenta.nombre}</span>
                  <span className={`shrink-0 tabular-nums ${saldo < 0 ? 'text-rose-600' : 'text-slate-900'}`}>
                    {formatMonto(saldo, moneda)}
                  </span>
                </div>
                <Barra porcentaje={porcentaje(saldo)} color={cuenta.color ?? COLOR_POR_DEFECTO} />
              </div>
            </div>
            {/* Con una sola subcuenta, el detalle sería igual a la cuenta. */}
            {cuenta.subcuentas.length > 1 && (
              <ul className="mt-1.5 ml-11 space-y-1">
                {subcuentas.map((s) => (
                  <li key={s.id} className="flex items-baseline justify-between gap-2 text-xs">
                    <span className="truncate text-slate-500">{s.nombre}</span>
                    <span className="shrink-0 tabular-nums">
                      <span className={s.saldo < 0 ? 'text-rose-600' : 'text-slate-700'}>{formatMonto(s.saldo, moneda)}</span>
                      <span className="ml-2 inline-block w-10 text-right text-slate-400">
                        {s.saldo > 0 ? `${Math.round(porcentaje(s.saldo))}%` : ''}
                      </span>
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </li>
        ))}
      </ul>
    </section>
  )
}

function Barra({ porcentaje, color }: { porcentaje: number; color: string }) {
  return (
    <div className="mt-1 flex items-center gap-2">
      <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-slate-100">
        <div className="h-full rounded-full" style={{ width: `${porcentaje}%`, backgroundColor: color }} />
      </div>
      <span className="w-10 text-right text-xs tabular-nums text-slate-500">
        {porcentaje > 0 ? `${Math.round(porcentaje)}%` : ''}
      </span>
    </div>
  )
}
