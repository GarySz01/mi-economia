import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { categoriasApi, cuentasApi, movimientosApi, resumenApi, transferenciasApi } from './endpoints'
import type {
  CategoriaInput,
  CuentaInput,
  MovimientoInput,
  MovimientosFiltro,
  TransferenciaInput,
  TransferenciasFiltro,
} from './tipos'

// Claves de caché. Un movimiento afecta al listado, al resumen y al saldo de su cuenta;
// una transferencia, a su listado y a los saldos. Cambiar una categoría o una cuenta
// afecta además a cómo se muestran los movimientos y las transferencias.
const claves = {
  categorias: ['categorias'] as const,
  cuentas: ['cuentas'] as const,
  movimientos: ['movimientos'] as const,
  transferencias: ['transferencias'] as const,
  resumen: ['resumen'] as const,
}

export function useCategorias() {
  return useQuery({ queryKey: claves.categorias, queryFn: () => categoriasApi.listar() })
}

export function useCuentas() {
  return useQuery({ queryKey: claves.cuentas, queryFn: () => cuentasApi.listar() })
}

export function useMovimientos(filtro: MovimientosFiltro) {
  return useQuery({
    queryKey: [...claves.movimientos, filtro],
    queryFn: () => movimientosApi.listar(filtro),
    // Al cambiar de página o filtro se siguen mostrando los datos anteriores hasta que lleguen los nuevos.
    placeholderData: keepPreviousData,
  })
}

export function useTransferencias(filtro: TransferenciasFiltro) {
  return useQuery({
    queryKey: [...claves.transferencias, filtro],
    queryFn: () => transferenciasApi.listar(filtro),
    placeholderData: keepPreviousData,
  })
}

export function useResumen(desde: string, hasta: string) {
  return useQuery({
    queryKey: [...claves.resumen, 'periodo', desde, hasta],
    queryFn: () => resumenApi.obtener(desde, hasta),
    placeholderData: keepPreviousData,
  })
}

export function useResumenMensual(hasta: string, meses = 6) {
  return useQuery({
    queryKey: [...claves.resumen, 'mensual', hasta, meses],
    queryFn: () => resumenApi.mensual(hasta, meses),
    placeholderData: keepPreviousData,
  })
}

function useInvalidar() {
  const queryClient = useQueryClient()
  return (...listas: (readonly string[])[]) =>
    Promise.all(listas.map((queryKey) => queryClient.invalidateQueries({ queryKey })))
}

export function useGuardarMovimiento() {
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: ({ id, datos }: { id?: string; datos: MovimientoInput }) =>
      id ? movimientosApi.actualizar(id, datos) : movimientosApi.crear(datos),
    onSuccess: () => invalidar(claves.movimientos, claves.resumen, claves.cuentas),
  })
}

export function useEliminarMovimiento() {
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: (id: string) => movimientosApi.eliminar(id),
    onSuccess: () => invalidar(claves.movimientos, claves.resumen, claves.cuentas),
  })
}

export function useGuardarCategoria() {
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: ({ id, datos }: { id?: string; datos: CategoriaInput }) =>
      id ? categoriasApi.actualizar(id, datos) : categoriasApi.crear(datos),
    onSuccess: () => invalidar(claves.categorias, claves.movimientos, claves.resumen),
  })
}

export function useEliminarCategoria() {
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: (id: string) => categoriasApi.eliminar(id),
    onSuccess: () => invalidar(claves.categorias),
  })
}

export function useGuardarCuenta() {
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: ({ id, datos }: { id?: string; datos: CuentaInput }) =>
      id ? cuentasApi.actualizar(id, datos) : cuentasApi.crear(datos),
    onSuccess: () => invalidar(claves.cuentas, claves.movimientos, claves.transferencias),
  })
}

export function useEliminarCuenta() {
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: (id: string) => cuentasApi.eliminar(id),
    onSuccess: () => invalidar(claves.cuentas),
  })
}

export function useGuardarTransferencia() {
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: ({ id, datos }: { id?: string; datos: TransferenciaInput }) =>
      id ? transferenciasApi.actualizar(id, datos) : transferenciasApi.crear(datos),
    onSuccess: () => invalidar(claves.transferencias, claves.cuentas),
  })
}

export function useEliminarTransferencia() {
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: (id: string) => transferenciasApi.eliminar(id),
    onSuccess: () => invalidar(claves.transferencias, claves.cuentas),
  })
}
