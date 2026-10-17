// Tipos que reflejan los DTOs de la API (backend/src/MiEconomia.Api).

export type TipoMovimiento = 'Ingreso' | 'Egreso'

export interface Usuario {
  id: string
  nombre: string
  email: string
}

export interface AuthResponse {
  token: string
  expiraEn: string
  usuario: Usuario
}

export interface Categoria {
  id: string
  nombre: string
  tipo: TipoMovimiento
  color: string | null
  icono: string | null
}

export interface Subcategoria {
  id: string
  nombre: string
}

/** Lo que devuelven los endpoints de categorías. En movimientos y en el resumen viene sin subcategorías. */
export interface CategoriaConSubcategorias extends Categoria {
  /** Ordenadas por nombre. */
  subcategorias: Subcategoria[]
}

export interface CategoriaInput {
  nombre: string
  tipo: TipoMovimiento
  color: string | null
  icono: string | null
}

export type TipoCuenta = 'Efectivo' | 'CuentaBancaria' | 'TarjetaCredito' | 'BilleteraVirtual' | 'Inversion' | 'Otra'

/** Datos de la cuenta que vienen dentro de un movimiento o una transferencia (sin saldo). */
export interface CuentaBasica {
  id: string
  nombre: string
  tipo: TipoCuenta
  color: string | null
}

export interface Cuenta extends CuentaBasica {
  /** Puede ser negativo (por ejemplo, la deuda de una tarjeta). */
  saldoInicial: number
  /** Saldo actual: inicial + ingresos − gastos ± transferencias. */
  saldo: number
}

export interface CuentaInput {
  nombre: string
  tipo: TipoCuenta
  saldoInicial: number
  color: string | null
}

export interface Transferencia {
  id: string
  monto: number
  /** Formato yyyy-MM-dd */
  fecha: string
  nota: string | null
  origen: CuentaBasica
  destino: CuentaBasica
}

export interface TransferenciaInput {
  cuentaOrigenId: string
  cuentaDestinoId: string
  monto: number
  fecha: string
  nota: string | null
}

export interface TransferenciasFiltro {
  desde?: string
  hasta?: string
  /** Transferencias en las que participa la cuenta, como origen o destino. */
  cuentaId?: string
  pagina?: number
  tamanio?: number
}

export interface Movimiento {
  id: string
  monto: number
  /** Formato yyyy-MM-dd */
  fecha: string
  nota: string | null
  categoria: Categoria
  cuenta: CuentaBasica
  subcategoria: Subcategoria | null
}

export interface MovimientoInput {
  categoriaId: string
  /** Opcional; tiene que ser de la categoría elegida. */
  subcategoriaId: string | null
  cuentaId: string
  monto: number
  fecha: string
  nota: string | null
}

export interface MovimientosFiltro {
  desde?: string
  hasta?: string
  categoriaId?: string
  subcategoriaId?: string
  cuentaId?: string
  tipo?: TipoMovimiento
  pagina?: number
  tamanio?: number
}

export interface Pagina<T> {
  items: T[]
  total: number
  pagina: number
  tamanio: number
}

export interface TotalPorCategoria {
  categoria: Categoria
  total: number
  cantidad: number
  porcentaje: number
}

export interface Resumen {
  desde: string
  hasta: string
  totalIngresos: number
  totalEgresos: number
  balance: number
  cantidadMovimientos: number
  porCategoria: TotalPorCategoria[]
}

export interface TotalMensual {
  anio: number
  mes: number
  ingresos: number
  egresos: number
  balance: number
}
