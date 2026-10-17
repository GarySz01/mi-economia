import type { Moneda, SaldoPorMoneda } from '../api/tipos'

/** Monedas en el orden en que se ofrecen. Tienen que coincidir con Monedas.cs. */
export const MONEDAS: { moneda: Moneda; nombre: string; simbolo: string }[] = [
  { moneda: 'UYU', nombre: 'Pesos uruguayos', simbolo: '$' },
  { moneda: 'USD', nombre: 'Dólares', simbolo: 'US$' },
  { moneda: 'EUR', nombre: 'Euros', simbolo: '€' },
  { moneda: 'ARS', nombre: 'Pesos argentinos', simbolo: 'AR$' },
  { moneda: 'BRL', nombre: 'Reales', simbolo: 'R$' },
  { moneda: 'UYI', nombre: 'Unidades indexadas', simbolo: 'UI' },
]

/** La que usa la API cuando no se indica otra. */
export const MONEDA_POR_DEFECTO: Moneda = 'UYU'

export function simboloMoneda(moneda: Moneda) {
  return MONEDAS.find((m) => m.moneda === moneda)?.simbolo ?? moneda
}

/** Ordena según MONEDAS (primero la por defecto), para mostrar los saldos siempre en el mismo orden. */
export function ordenarPorMoneda<T extends { moneda: Moneda }>(lista: T[]) {
  const posicion = (m: Moneda) => MONEDAS.findIndex((x) => x.moneda === m)
  return [...lista].sort((a, b) => posicion(a.moneda) - posicion(b.moneda))
}

/** Suma saldos agrupando por moneda (por ejemplo, los de todas las cuentas). Nunca mezcla monedas. */
export function sumarPorMoneda(saldos: SaldoPorMoneda[]): SaldoPorMoneda[] {
  const totales = new Map<Moneda, number>()
  for (const s of saldos) totales.set(s.moneda, (totales.get(s.moneda) ?? 0) + s.saldo)
  return ordenarPorMoneda([...totales].map(([moneda, saldo]) => ({ moneda, saldo })))
}
