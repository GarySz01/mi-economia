import { Plus } from 'lucide-react'
import { useState } from 'react'
import { useSearchParams } from 'react-router'
import { useCategorias, useCuentas, useEliminarMovimiento, useMovimientos } from '../api/consultas'
import type { Movimiento, TipoMovimiento } from '../api/tipos'
import { Confirmar } from '../components/Confirmar'
import { FilaMovimiento } from '../components/FilaMovimiento'
import { MovimientoFormulario } from '../components/MovimientoFormulario'
import { SelectorMes } from '../components/SelectorMes'
import { useMesSeleccionado } from '../hooks/useMesSeleccionado'
import { Alerta, Boton, Cargando, Segmentado, Tarjeta, Vacio } from '../components/ui'
import { formatFechaLarga, formatMonto, rangoDelMes } from '../lib/formato'

const TAMANIO_PAGINA = 20

type FiltroTipo = TipoMovimiento | 'Todos'

export function Movimientos() {
  const [mes, setMesUrl] = useMesSeleccionado()
  const [tipo, setTipo] = useState<FiltroTipo>('Todos')
  const [categoriaId, setCategoriaId] = useState('')
  const [pagina, setPagina] = useState(1)
  // La cuenta va en la URL (?cuenta=) para poder llegar desde la página de cuentas ya filtrado.
  const [params, setParams] = useSearchParams()
  const cuentaId = params.get('cuenta') ?? ''

  const { desde, hasta } = rangoDelMes(mes)
  const { data: categorias = [] } = useCategorias()
  const { data: cuentas = [] } = useCuentas()
  const { data, isLoading, isError, error, isPlaceholderData } = useMovimientos({
    desde,
    hasta,
    tipo: tipo === 'Todos' ? undefined : tipo,
    categoriaId: categoriaId || undefined,
    cuentaId: cuentaId || undefined,
    pagina,
    tamanio: TAMANIO_PAGINA,
  })
  const eliminar = useEliminarMovimiento()

  const [editando, setEditando] = useState<{ abierto: boolean; movimiento: Movimiento | null }>({
    abierto: false,
    movimiento: null,
  })
  const [aEliminar, setAEliminar] = useState<Movimiento | null>(null)

  // Cualquier cambio de filtro vuelve a la primera página.
  const setMes = (nuevo: string) => {
    setMesUrl(nuevo)
    setPagina(1)
  }
  const cambiarTipo = (nuevo: FiltroTipo) => {
    setTipo(nuevo)
    setCategoriaId('')
    setPagina(1)
  }
  const cambiarCuenta = (nueva: string) => {
    setParams(
      (anteriores) => {
        const siguientes = new URLSearchParams(anteriores)
        if (nueva) siguientes.set('cuenta', nueva)
        else siguientes.delete('cuenta')
        return siguientes
      },
      { replace: true },
    )
    setPagina(1)
  }

  const categoriasFiltro = tipo === 'Todos' ? categorias : categorias.filter((c) => c.tipo === tipo)
  const totalPaginas = data ? Math.max(1, Math.ceil(data.total / TAMANIO_PAGINA)) : 1
  const porDia = agruparPorDia(data?.items ?? [])

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold text-slate-900">Movimientos</h1>
        <div className="flex flex-wrap items-center gap-2">
          <SelectorMes mes={mes} onChange={setMes} />
          <Boton onClick={() => setEditando({ abierto: true, movimiento: null })}>
            <Plus className="size-4" aria-hidden />
            Nuevo
          </Boton>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <Segmentado
          etiqueta="Filtrar por tipo"
          valor={tipo}
          onChange={cambiarTipo}
          opciones={[
            { valor: 'Todos', texto: 'Todos' },
            { valor: 'Egreso', texto: 'Gastos' },
            { valor: 'Ingreso', texto: 'Ingresos' },
          ]}
        />
        <select
          aria-label="Filtrar por categoría"
          value={categoriaId}
          onChange={(e) => {
            setCategoriaId(e.target.value)
            setPagina(1)
          }}
          className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-200"
        >
          <option value="">Todas las categorías</option>
          {categoriasFiltro.map((c) => (
            <option key={c.id} value={c.id}>
              {c.nombre}
            </option>
          ))}
        </select>
        <select
          aria-label="Filtrar por cuenta"
          value={cuentaId}
          onChange={(e) => cambiarCuenta(e.target.value)}
          className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-200"
        >
          <option value="">Todas las cuentas</option>
          {cuentas.map((c) => (
            <option key={c.id} value={c.id}>
              {c.nombre}
            </option>
          ))}
        </select>
      </div>

      {isError && <Alerta>{error.message}</Alerta>}

      <Tarjeta className={isPlaceholderData ? 'opacity-60 transition-opacity' : ''}>
        {isLoading ? (
          <Cargando />
        ) : porDia.length === 0 ? (
          <Vacio titulo="No hay movimientos con estos filtros." />
        ) : (
          <div className="space-y-4">
            {porDia.map(({ fecha, movimientos, total }) => (
              <section key={fecha}>
                <div className="flex items-baseline justify-between border-b border-slate-100 pb-1 text-xs font-medium text-slate-500">
                  <h2>{formatFechaLarga(fecha)}</h2>
                  <span className="tabular-nums">{formatMonto(total)}</span>
                </div>
                <ul className="divide-y divide-slate-100">
                  {movimientos.map((m) => (
                    <FilaMovimiento
                      key={m.id}
                      movimiento={m}
                      sinFecha
                      onEditar={(movimiento) => setEditando({ abierto: true, movimiento })}
                      onEliminar={setAEliminar}
                    />
                  ))}
                </ul>
              </section>
            ))}
          </div>
        )}
      </Tarjeta>

      {data && totalPaginas > 1 && (
        <div className="flex items-center justify-between text-sm text-slate-600">
          <span>
            {data.total} movimientos · página {pagina} de {totalPaginas}
          </span>
          <div className="flex gap-2">
            <Boton variante="secundario" disabled={pagina === 1} onClick={() => setPagina((p) => p - 1)}>
              Anterior
            </Boton>
            <Boton variante="secundario" disabled={pagina >= totalPaginas} onClick={() => setPagina((p) => p + 1)}>
              Siguiente
            </Boton>
          </div>
        </div>
      )}

      <MovimientoFormulario
        abierto={editando.abierto}
        movimiento={editando.movimiento}
        cuentaInicial={cuentaId || undefined}
        onCerrar={() => setEditando({ abierto: false, movimiento: null })}
      />

      <Confirmar
        titulo="Eliminar movimiento"
        abierto={aEliminar !== null}
        onCerrar={() => setAEliminar(null)}
        onConfirmar={() => eliminar.mutateAsync(aEliminar!.id)}
      >
        ¿Seguro que quieres eliminar{' '}
        <strong className="text-slate-800">
          {aEliminar?.nota || aEliminar?.categoria.nombre} ({aEliminar && formatMonto(aEliminar.monto)})
        </strong>
        ? Esta acción no se puede deshacer.
      </Confirmar>
    </div>
  )
}

/**
 * Agrupa por fecha (la API ya los devuelve ordenados de más reciente a más antiguo).
 * El total del día es el neto: ingresos menos gastos.
 */
function agruparPorDia(movimientos: Movimiento[]) {
  const grupos: { fecha: string; movimientos: Movimiento[]; total: number }[] = []
  for (const m of movimientos) {
    let grupo = grupos.at(-1)
    if (!grupo || grupo.fecha !== m.fecha) {
      grupo = { fecha: m.fecha, movimientos: [], total: 0 }
      grupos.push(grupo)
    }
    grupo.movimientos.push(m)
    grupo.total += m.categoria.tipo === 'Ingreso' ? m.monto : -m.monto
  }
  return grupos
}
