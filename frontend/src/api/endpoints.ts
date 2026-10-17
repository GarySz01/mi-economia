import { api, query } from './cliente'
import type {
  ActualizarCuentaInput,
  AuthResponse,
  CategoriaConSubcategorias,
  CategoriaInput,
  CrearCuentaInput,
  Cuenta,
  Moneda,
  Movimiento,
  MovimientoInput,
  MovimientosFiltro,
  Pagina,
  Resumen,
  Subcategoria,
  Subcuenta,
  SubcuentaInput,
  TipoMovimiento,
  TotalMensual,
  Transferencia,
  TransferenciaInput,
  TransferenciasFiltro,
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
  listar: (tipo?: TipoMovimiento) => api<CategoriaConSubcategorias[]>(`/categorias${query({ tipo })}`),
  crear: (datos: CategoriaInput) => api<CategoriaConSubcategorias>('/categorias', { method: 'POST', body: datos }),
  actualizar: (id: string, datos: Omit<CategoriaInput, 'tipo'>) =>
    api<CategoriaConSubcategorias>(`/categorias/${id}`, { method: 'PUT', body: datos }),
  eliminar: (id: string) => api<void>(`/categorias/${id}`, { method: 'DELETE' }),
}

// Se listan dentro de cada categoría (categoriasApi.listar).
export const subcategoriasApi = {
  crear: (categoriaId: string, nombre: string) =>
    api<Subcategoria>(`/categorias/${categoriaId}/subcategorias`, { method: 'POST', body: { nombre } }),
  actualizar: (categoriaId: string, id: string, nombre: string) =>
    api<Subcategoria>(`/categorias/${categoriaId}/subcategorias/${id}`, { method: 'PUT', body: { nombre } }),
  eliminar: (categoriaId: string, id: string) =>
    api<void>(`/categorias/${categoriaId}/subcategorias/${id}`, { method: 'DELETE' }),
}

export const movimientosApi = {
  listar: (filtro: MovimientosFiltro) => api<Pagina<Movimiento>>(`/movimientos${query(filtro)}`),
  crear: (datos: MovimientoInput) => api<Movimiento>('/movimientos', { method: 'POST', body: datos }),
  actualizar: (id: string, datos: MovimientoInput) =>
    api<Movimiento>(`/movimientos/${id}`, { method: 'PUT', body: datos }),
  eliminar: (id: string) => api<void>(`/movimientos/${id}`, { method: 'DELETE' }),
}

export const cuentasApi = {
  listar: () => api<Cuenta[]>('/cuentas'),
  crear: (datos: CrearCuentaInput) => api<Cuenta>('/cuentas', { method: 'POST', body: datos }),
  actualizar: (id: string, datos: ActualizarCuentaInput) =>
    api<Cuenta>(`/cuentas/${id}`, { method: 'PUT', body: datos }),
  eliminar: (id: string) => api<void>(`/cuentas/${id}`, { method: 'DELETE' }),
}

export const transferenciasApi = {
  listar: (filtro: TransferenciasFiltro) => api<Pagina<Transferencia>>(`/transferencias${query(filtro)}`),
  crear: (datos: TransferenciaInput) => api<Transferencia>('/transferencias', { method: 'POST', body: datos }),
  actualizar: (id: string, datos: TransferenciaInput) =>
    api<Transferencia>(`/transferencias/${id}`, { method: 'PUT', body: datos }),
  eliminar: (id: string) => api<void>(`/transferencias/${id}`, { method: 'DELETE' }),
}

export const resumenApi = {
  obtener: (desde: string, hasta: string, moneda: Moneda) => api<Resumen>(`/resumen${query({ desde, hasta, moneda })}`),
  mensual: (hasta: string, moneda: Moneda, meses = 6) =>
    api<TotalMensual[]>(`/resumen/mensual${query({ hasta, moneda, meses })}`),
}

// Se listan dentro de cada cuenta (cuentasApi.listar), con su saldo.
export const subcuentasApi = {
  crear: (cuentaId: string, datos: SubcuentaInput) =>
    api<Subcuenta>(`/cuentas/${cuentaId}/subcuentas`, { method: 'POST', body: datos }),
  actualizar: (cuentaId: string, id: string, datos: SubcuentaInput) =>
    api<Subcuenta>(`/cuentas/${cuentaId}/subcuentas/${id}`, { method: 'PUT', body: datos }),
  eliminar: (cuentaId: string, id: string) => api<void>(`/cuentas/${cuentaId}/subcuentas/${id}`, { method: 'DELETE' }),
}
