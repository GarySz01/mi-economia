import type { Cuenta, Subcuenta, TipoCuenta } from '../api/tipos'

/**
 * La subcuenta en la que va el dinero: la elegida si es de la cuenta o, si la cuenta tiene una sola, esa.
 * Undefined si todavía hay que elegir (igual que la regla de la API).
 */
export function subcuentaEfectiva(cuenta: Cuenta | undefined, elegidaId: string): Subcuenta | undefined {
  if (!cuenta) return undefined
  return cuenta.subcuentas.find((s) => s.id === elegidaId) ?? (cuenta.subcuentas.length === 1 ? cuenta.subcuentas[0] : undefined)
}

/**
 * Tipos de cuenta en el orden en que se ofrecen, con su nombre para mostrar y su ícono.
 * Los íconos son claves del mapa ICONOS de lib/iconos.ts.
 */
export const TIPOS_CUENTA: { tipo: TipoCuenta; texto: string; icono: string }[] = [
  { tipo: 'Efectivo', texto: 'Efectivo', icono: 'wallet' },
  { tipo: 'CuentaBancaria', texto: 'Cuenta bancaria', icono: 'landmark' },
  { tipo: 'TarjetaCredito', texto: 'Tarjeta de crédito', icono: 'credit-card' },
  { tipo: 'BilleteraVirtual', texto: 'Billetera virtual', icono: 'smartphone' },
  { tipo: 'Inversion', texto: 'Inversión', icono: 'trending-up' },
  { tipo: 'Otra', texto: 'Otra', icono: 'piggy-bank' },
]

export function datosTipoCuenta(tipo: TipoCuenta) {
  return TIPOS_CUENTA.find((t) => t.tipo === tipo) ?? TIPOS_CUENTA[TIPOS_CUENTA.length - 1]
}
