import { ArrowDownCircle, ArrowUpCircle, Plus, Scale } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { Bar, BarChart, CartesianGrid, Cell, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { useCuentas, useMovimientos, useResumen, useResumenMensual } from '../api/consultas'
import type { Moneda, Movimiento, TotalMensual, TotalPorCategoria } from '../api/tipos'
import { useAuth } from '../auth/contexto'
import { FilaMovimiento } from '../components/FilaMovimiento'
import { MovimientoFormulario } from '../components/MovimientoFormulario'
import { SelectorMes } from '../components/SelectorMes'
import { useMesSeleccionado } from '../hooks/useMesSeleccionado'
import { Alerta, Boton, Cargando, Segmentado, Tarjeta, Vacio } from '../components/ui'
import { formatMonto, formatMontoCorto, nombreMesCorto, rangoDelMes } from '../lib/formato'
import { IconoCategoria } from '../components/IconoCategoria'
import { DistribucionDinero } from '../components/DistribucionDinero'
import { COLOR_POR_DEFECTO } from '../lib/iconos'
import { MONEDA_POR_DEFECTO, simboloMoneda, sumarPorMoneda } from '../lib/monedas'

export function Panel() {
  const { usuario } = useAuth()
  const [mes, setMes] = useMesSeleccionado()
  const { desde, hasta } = rangoDelMes(mes)

  // El resumen es de una moneda: si hay varias en uso, se elige cuál ver.
  const { data: cuentas = [] } = useCuentas()
  const monedas = sumarPorMoneda(cuentas.flatMap((c) => c.subcuentas)).map((s) => s.moneda)
  const [monedaElegida, setMoneda] = useState<Moneda | null>(null)
  const moneda = monedaElegida && monedas.includes(monedaElegida) ? monedaElegida : (monedas[0] ?? MONEDA_POR_DEFECTO)

  const resumen = useResumen(desde, hasta, moneda)
  const mensual = useResumenMensual(hasta, moneda)
  const ultimos = useMovimientos({ desde, hasta, tamanio: 5 })

  const [formulario, setFormulario] = useState<{ abierto: boolean; movimiento: Movimiento | null }>({
    abierto: false,
    movimiento: null,
  })

  const datos = resumen.data
  const egresos = datos?.porCategoria.filter((c) => c.categoria.tipo === 'Egreso') ?? []
  const ingresos = datos?.porCategoria.filter((c) => c.categoria.tipo === 'Ingreso') ?? []

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold text-slate-900">Hola, {usuario?.nombre.split(' ')[0]}</h1>
        <div className="flex flex-wrap items-center gap-2">
          {monedas.length > 1 && (
            <Segmentado
              etiqueta="Moneda del resumen"
              valor={moneda}
              onChange={setMoneda}
              opciones={monedas.map((m) => ({ valor: m, texto: simboloMoneda(m) }))}
            />
          )}
          <SelectorMes mes={mes} onChange={setMes} />
          <Boton onClick={() => setFormulario({ abierto: true, movimiento: null })}>
            <Plus className="size-4" aria-hidden />
            Nuevo movimiento
          </Boton>
        </div>
      </div>

      {resumen.isError && <Alerta>{resumen.error.message}</Alerta>}

      <div className="grid gap-3 sm:grid-cols-3">
        <Indicador titulo="Ingresos" valor={datos?.totalIngresos} moneda={moneda} Icono={ArrowUpCircle} color="text-emerald-600" />
        <Indicador titulo="Gastos" valor={datos?.totalEgresos} moneda={moneda} Icono={ArrowDownCircle} color="text-rose-600" />
        <Indicador
          titulo="Balance"
          valor={datos?.balance}
          moneda={moneda}
          Icono={Scale}
          color={(datos?.balance ?? 0) < 0 ? 'text-rose-600' : 'text-slate-900'}
        />
      </div>

      <DistribucionDinero />

      {resumen.isLoading ? (
        <Cargando />
      ) : datos && datos.cantidadMovimientos === 0 ? (
        <Tarjeta>
          <Vacio titulo="Todavía no hay movimientos en este mes.">
            <Boton onClick={() => setFormulario({ abierto: true, movimiento: null })}>
              <Plus className="size-4" aria-hidden />
              Registrar el primero
            </Boton>
          </Vacio>
        </Tarjeta>
      ) : (
        <div className="grid gap-4 lg:grid-cols-2">
          <Tarjeta>
            <h2 className="mb-2 font-semibold text-slate-900">Gastos por categoría</h2>
            {egresos.length === 0 ? (
              <Vacio titulo="Sin gastos este mes." />
            ) : (
              <>
                <GraficoTorta datos={egresos} total={datos?.totalEgresos ?? 0} moneda={moneda} />
                <ListaCategorias datos={egresos} moneda={moneda} />
              </>
            )}
          </Tarjeta>

          <div className="space-y-4">
            <Tarjeta>
              <h2 className="mb-2 font-semibold text-slate-900">Ingresos por categoría</h2>
              {ingresos.length === 0 ? <Vacio titulo="Sin ingresos este mes." /> : <ListaCategorias datos={ingresos} moneda={moneda} />}
            </Tarjeta>

            <Tarjeta>
              <div className="mb-1 flex items-center justify-between">
                <h2 className="font-semibold text-slate-900">Últimos movimientos</h2>
                <Link to={`/movimientos${mes ? `?mes=${mes}` : ''}`} className="text-sm font-medium text-emerald-700 hover:underline">
                  Ver todos
                </Link>
              </div>
              <ul className="divide-y divide-slate-100">
                {ultimos.data?.items.map((m) => (
                  <FilaMovimiento key={m.id} movimiento={m} onEditar={(movimiento) => setFormulario({ abierto: true, movimiento })} />
                ))}
              </ul>
            </Tarjeta>
          </div>
        </div>
      )}

      <Tarjeta>
        <h2 className="mb-4 font-semibold text-slate-900">Últimos 6 meses</h2>
        {mensual.data ? <GraficoMensual datos={mensual.data} moneda={moneda} /> : <Cargando />}
      </Tarjeta>

      <MovimientoFormulario
        abierto={formulario.abierto}
        movimiento={formulario.movimiento}
        onCerrar={() => setFormulario({ abierto: false, movimiento: null })}
      />
    </div>
  )
}

function Indicador({
  titulo,
  valor,
  moneda,
  Icono,
  color,
}: {
  titulo: string
  valor: number | undefined
  moneda: Moneda
  Icono: typeof Scale
  color: string
}) {
  return (
    <Tarjeta className="flex items-center gap-3">
      <Icono className={`size-8 shrink-0 ${color}`} strokeWidth={1.5} aria-hidden />
      <div className="min-w-0">
        <p className="text-sm text-slate-500">{titulo}</p>
        <p className={`truncate text-xl font-semibold tabular-nums ${color}`}>
          {valor === undefined ? '—' : formatMonto(valor, moneda)}
        </p>
      </div>
    </Tarjeta>
  )
}

function GraficoTorta({ datos, total, moneda }: { datos: TotalPorCategoria[]; total: number; moneda: Moneda }) {
  return (
    <div className="relative h-52">
      <ResponsiveContainer>
        <PieChart>
          <Pie
            data={datos}
            dataKey="total"
            nameKey="categoria.nombre"
            innerRadius="62%"
            outerRadius="90%"
            paddingAngle={datos.length > 1 ? 2 : 0}
            stroke="none"
            isAnimationActive={false}
          >
            {datos.map((d) => (
              <Cell key={d.categoria.id} fill={d.categoria.color ?? COLOR_POR_DEFECTO} />
            ))}
          </Pie>
          <Tooltip formatter={(valor) => formatMonto(Number(valor), moneda)} />
        </PieChart>
      </ResponsiveContainer>
      <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
        <span className="text-xs text-slate-500">Total</span>
        <span className="text-base font-semibold tabular-nums text-slate-900">{formatMonto(total, moneda)}</span>
      </div>
    </div>
  )
}

function ListaCategorias({ datos, moneda }: { datos: TotalPorCategoria[]; moneda: Moneda }) {
  return (
    <ul className="mt-2 space-y-3">
      {datos.map((d) => (
        <li key={d.categoria.id} className="flex items-center gap-3">
          <IconoCategoria icono={d.categoria.icono} color={d.categoria.color} tamanio="sm" />
          <div className="min-w-0 flex-1">
            <div className="flex items-baseline justify-between gap-2 text-sm">
              <span className="truncate font-medium text-slate-700">{d.categoria.nombre}</span>
              <span className="shrink-0 tabular-nums text-slate-900">{formatMonto(d.total, moneda)}</span>
            </div>
            <div className="mt-1 flex items-center gap-2">
              <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-slate-100">
                <div
                  className="h-full rounded-full"
                  style={{ width: `${d.porcentaje}%`, backgroundColor: d.categoria.color ?? COLOR_POR_DEFECTO }}
                />
              </div>
              <span className="w-12 text-right text-xs tabular-nums text-slate-500">
                {d.porcentaje.toLocaleString('es')}%
              </span>
            </div>
          </div>
        </li>
      ))}
    </ul>
  )
}

function GraficoMensual({ datos, moneda }: { datos: TotalMensual[]; moneda: Moneda }) {
  const filas = datos.map((d) => ({ ...d, nombre: nombreMesCorto(d.anio, d.mes) }))
  return (
    <div className="h-64">
      <ResponsiveContainer>
        <BarChart data={filas} margin={{ left: 8, right: 8 }}>
          <CartesianGrid vertical={false} stroke="#e2e8f0" />
          <XAxis dataKey="nombre" tickLine={false} axisLine={false} tick={{ fill: '#64748b', fontSize: 12 }} />
          <YAxis
            tickFormatter={(valor: number) => formatMontoCorto(valor, moneda)}
            tickLine={false}
            axisLine={false}
            width={70}
            tick={{ fill: '#64748b', fontSize: 12 }}
          />
          <Tooltip formatter={(valor) => formatMonto(Number(valor), moneda)} cursor={{ fill: '#f1f5f9' }} />
          <Bar dataKey="ingresos" name="Ingresos" fill="#059669" radius={[4, 4, 0, 0]} maxBarSize={32} />
          <Bar dataKey="egresos" name="Gastos" fill="#e11d48" radius={[4, 4, 0, 0]} maxBarSize={32} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}
