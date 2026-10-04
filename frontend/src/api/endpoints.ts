import { api, query } from './cliente'
import type {
  AuthResponse,
  Categoria,
  CategoriaInput,
  Movimiento,
  MovimientoInput,
  MovimientosFiltro,
  Pagina,
  Resumen,
  TipoMovimiento,
  TotalMensual,
  Usuario,
} from './tipos'

export const authApi = {
  login: (email: string, password: string) =>
    api<AuthResponse>('/auth/login', { method: 'POST', body: { email, password } }),
  registro: (nombre: string, email: string, password: string) =>
    api<AuthResponse>('/auth/registro', { method: 'POST', body: { nombre, email, password } }),
  yo: () => api<Usuario>('/auth/yo'),
}

export const categoriasApi = {
  listar: (tipo?: TipoMovimiento) => api<Categoria[]>(`/categorias${query({ tipo })}`),
  crear: (datos: CategoriaInput) => api<Categoria>('/categorias', { method: 'POST', body: datos }),
  actualizar: (id: string, datos: Omit<CategoriaInput, 'tipo'>) =>
    api<Categoria>(`/categorias/${id}`, { method: 'PUT', body: datos }),
  eliminar: (id: string) => api<void>(`/categorias/${id}`, { method: 'DELETE' }),
}

export const movimientosApi = {
  listar: (filtro: MovimientosFiltro) => api<Pagina<Movimiento>>(`/movimientos${query(filtro)}`),
  crear: (datos: MovimientoInput) => api<Movimiento>('/movimientos', { method: 'POST', body: datos }),
  actualizar: (id: string, datos: MovimientoInput) =>
    api<Movimiento>(`/movimientos/${id}`, { method: 'PUT', body: datos }),
  eliminar: (id: string) => api<void>(`/movimientos/${id}`, { method: 'DELETE' }),
}

export const resumenApi = {
  obtener: (desde: string, hasta: string) => api<Resumen>(`/resumen${query({ desde, hasta })}`),
  mensual: (hasta: string, meses = 6) => api<TotalMensual[]>(`/resumen/mensual${query({ hasta, meses })}`),
}
