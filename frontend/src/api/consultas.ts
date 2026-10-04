import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { categoriasApi, movimientosApi, resumenApi } from './endpoints'
import type { CategoriaInput, MovimientoInput, MovimientosFiltro } from './tipos'

// Claves de caché. Cambiar un movimiento afecta al listado y al resumen;
// cambiar una categoría afecta además a cómo se muestran los movimientos.
const claves = {
  categorias: ['categorias'] as const,
  movimientos: ['movimientos'] as const,
  resumen: ['resumen'] as const,
}

export function useCategorias() {
  return useQuery({ queryKey: claves.categorias, queryFn: () => categoriasApi.listar() })
}

export function useMovimientos(filtro: MovimientosFiltro) {
  return useQuery({
    queryKey: [...claves.movimientos, filtro],
    queryFn: () => movimientosApi.listar(filtro),
    // Al cambiar de página o filtro se siguen mostrando los datos anteriores hasta que lleguen los nuevos.
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
    onSuccess: () => invalidar(claves.movimientos, claves.resumen),
  })
}

export function useEliminarMovimiento() {
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: (id: string) => movimientosApi.eliminar(id),
    onSuccess: () => invalidar(claves.movimientos, claves.resumen),
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
