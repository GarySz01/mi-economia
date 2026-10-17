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

/** Código ISO 4217. Tiene que coincidir con Monedas.cs; los nombres y símbolos están en lib/monedas.ts. */
export type Moneda = 'UYU' | 'USD' | 'EUR' | 'ARS' | 'BRL' | 'UYI'

/** Datos de la subcuenta que vienen dentro de un movimiento o una transferencia (sin saldo). */
export interface SubcuentaBasica {
  id: string
  nombre: string
  moneda: Moneda
}

export interface Subcuenta extends SubcuentaBasica {
  /** Puede ser negativo (por ejemplo, la deuda de una tarjeta). */
  saldoInicial: number
  /** Saldo actual: inicial + ingresos − gastos ± transferencias. */
  saldo: number
}

export interface SubcuentaInput {
  nombre: string
  moneda: Moneda
  saldoInicial: number
}

export interface SaldoPorMoneda {
  moneda: Moneda
  saldo: number
}

export interface Cuenta extends CuentaBasica {
  /** Siempre al menos una, ordenadas por nombre. */
  subcuentas: Subcuenta[]
  /** Saldo total en cada moneda de sus subcuentas (los montos no se convierten). */
  saldos: SaldoPorMoneda[]
}

/** Al crear, la moneda y el saldo inicial son los de la primera subcuenta ("Disponible"). */
export interface CrearCuentaInput {
  nombre: string
  tipo: TipoCuenta
  color: string | null
  moneda: Moneda
  saldoInicial: number
}

/** Al editar no se tocan los saldos: se editan en cada subcuenta. */
export type ActualizarCuentaInput = Pick<CrearCuentaInput, 'nombre' | 'tipo' | 'color'>

export interface Transferencia {
  id: string
  /** Lo que sale del origen, en su moneda. */
  monto: number
  /** Lo que llega al destino, en su moneda. Igual a monto si las monedas son iguales. */
  montoDestino: number
  /** Formato yyyy-MM-dd */
  fecha: string
  nota: string | null
  origen: CuentaBasica
  destino: CuentaBasica
  subcuentaOrigen: SubcuentaBasica
  subcuentaDestino: SubcuentaBasica
}

export interface TransferenciaInput {
  cuentaOrigenId: string
  /** Obligatoria solo si la cuenta tiene más de una subcuenta (igual para el destino). */
  subcuentaOrigenId: string | null
  cuentaDestinoId: string
  subcuentaDestinoId: string | null
  monto: number
  /** Obligatorio solo si las subcuentas tienen distinta moneda. */
  montoDestino: number | null
  fecha: string
  nota: string | null
}

export interface TransferenciasFiltro {
  desde?: string
  hasta?: string
  /** Transferencias en las que participa la cuenta, como origen o destino. */
  cuentaId?: string
  subcuentaId?: string
  pagina?: number
  tamanio?: number
}

export interface Movimiento {
  id: string
  /** En la moneda de la subcuenta. */
  monto: number
  /** Formato yyyy-MM-dd */
  fecha: string
  nota: string | null
  categoria: Categoria
  cuenta: CuentaBasica
  subcuenta: SubcuentaBasica
  subcategoria: Subcategoria | null
}

export interface MovimientoInput {
  categoriaId: string
  /** Opcional; tiene que ser de la categoría elegida. */
  subcategoriaId: string | null
  cuentaId: string
  /** Obligatoria solo si la cuenta tiene más de una subcuenta. */
  subcuentaId: string | null
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
  subcuentaId?: string
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

/** Siempre de una sola moneda. */
export interface Resumen {
  desde: string
  hasta: string
  moneda: Moneda
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
