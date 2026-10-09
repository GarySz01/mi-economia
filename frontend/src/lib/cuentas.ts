import type { TipoCuenta } from '../api/tipos'

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
