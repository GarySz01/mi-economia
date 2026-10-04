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

export interface CategoriaInput {
  nombre: string
  tipo: TipoMovimiento
  color: string | null
  icono: string | null
}

export interface Movimiento {
  id: string
  monto: number
  /** Formato yyyy-MM-dd */
  fecha: string
  nota: string | null
  categoria: Categoria
}

export interface MovimientoInput {
  categoriaId: string
  monto: number
  fecha: string
  nota: string | null
}

export interface MovimientosFiltro {
  desde?: string
  hasta?: string
  categoriaId?: string
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
