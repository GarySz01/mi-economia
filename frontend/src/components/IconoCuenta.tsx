import type { CuentaBasica } from '../api/tipos'
import { datosTipoCuenta } from '../lib/cuentas'
import { IconoCategoria } from './IconoCategoria'

/** Ícono del tipo de cuenta dentro de un círculo del color de la cuenta. */
export function IconoCuenta({ cuenta, tamanio }: { cuenta: CuentaBasica; tamanio?: 'sm' | 'md' }) {
  return <IconoCategoria icono={datosTipoCuenta(cuenta.tipo).icono} color={cuenta.color} tamanio={tamanio} />
}
