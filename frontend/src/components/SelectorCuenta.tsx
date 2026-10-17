import type { Cuenta } from '../api/tipos'
import { formatMonto, formatSaldos } from '../lib/formato'
import { Select } from './ui'

interface Props {
  etiqueta: string
  cuentas: Cuenta[]
  cargando?: boolean
  cuentaId: string
  subcuentaId: string
  /** Al cambiar de cuenta, la subcuenta se vuelve a elegir. */
  onChange: (cuentaId: string, subcuentaId: string) => void
  errorCuenta?: string
  errorSubcuenta?: string
  /** Muestra el saldo de cada opción (útil al transferir). */
  conSaldos?: boolean
}

/**
 * Elige una cuenta y, si la cuenta tiene más de una, la subcuenta.
 * Con una sola subcuenta no se muestra nada más: la API usa esa.
 */
export function SelectorCuenta({
  etiqueta,
  cuentas,
  cargando,
  cuentaId,
  subcuentaId,
  onChange,
  errorCuenta,
  errorSubcuenta,
  conSaldos,
}: Props) {
  const cuenta = cuentas.find((c) => c.id === cuentaId)
  const subcuentas = cuenta?.subcuentas ?? []

  return (
    <div className="space-y-2">
      <Select
        etiqueta={etiqueta}
        value={cuentaId}
        onChange={(e) => onChange(e.target.value, '')}
        error={errorCuenta}
        disabled={cargando}
        required
      >
        <option value="">{cargando ? 'Cargando…' : 'Elige una cuenta'}</option>
        {cuentas.map((c) => (
          <option key={c.id} value={c.id}>
            {conSaldos ? `${c.nombre} (${formatSaldos(c.saldos)})` : c.nombre}
          </option>
        ))}
      </Select>

      {subcuentas.length > 1 && (
        <Select
          etiqueta={`Subcuenta de ${cuenta!.nombre}`}
          value={subcuentaId}
          onChange={(e) => onChange(cuentaId, e.target.value)}
          error={errorSubcuenta}
          required
        >
          <option value="">Elige una subcuenta</option>
          {subcuentas.map((s) => (
            <option key={s.id} value={s.id}>
              {s.nombre} ({formatMonto(s.saldo, s.moneda)})
            </option>
          ))}
        </Select>
      )}
    </div>
  )
}
