import { ArrowLeftRight, ArrowRight, CornerDownRight, List, ListPlus, Pencil, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import {
  useCuentas,
  useEliminarCuenta,
  useEliminarSubcuenta,
  useEliminarTransferencia,
  useTransferencias,
} from '../api/consultas'
import type { Cuenta, Subcuenta, Transferencia } from '../api/tipos'
import { Confirmar } from '../components/Confirmar'
import { CuentaFormulario } from '../components/CuentaFormulario'
import { IconoCuenta } from '../components/IconoCuenta'
import { SelectorMes } from '../components/SelectorMes'
import { SubcuentaFormulario } from '../components/SubcuentaFormulario'
import { TransferenciaFormulario } from '../components/TransferenciaFormulario'
import { Alerta, Boton, Cargando, Tarjeta, Vacio } from '../components/ui'
import { useMesSeleccionado } from '../hooks/useMesSeleccionado'
import { datosTipoCuenta } from '../lib/cuentas'
import { formatFecha, formatMonto, rangoDelMes } from '../lib/formato'
import { sumarPorMoneda } from '../lib/monedas'

export function Cuentas() {
  const { data: cuentas = [], isLoading, isError, error } = useCuentas()
  const eliminarCuenta = useEliminarCuenta()
  const eliminarSubcuenta = useEliminarSubcuenta()
  const eliminarTransferencia = useEliminarTransferencia()

  const [mes, setMes] = useMesSeleccionado()
  const { desde, hasta } = rangoDelMes(mes)
  const transferencias = useTransferencias({ desde, hasta, tamanio: 200 })

  const [formCuenta, setFormCuenta] = useState<{ abierto: boolean; cuenta: Cuenta | null }>({ abierto: false, cuenta: null })
  const [formSubcuenta, setFormSubcuenta] = useState<{
    abierto: boolean
    cuenta: Cuenta | null
    subcuenta: Subcuenta | null
  }>({ abierto: false, cuenta: null, subcuenta: null })
  const [formTransferencia, setFormTransferencia] = useState<{ abierto: boolean; transferencia: Transferencia | null }>({
    abierto: false,
    transferencia: null,
  })
  const [cuentaAEliminar, setCuentaAEliminar] = useState<Cuenta | null>(null)
  const [subcuentaAEliminar, setSubcuentaAEliminar] = useState<{ cuenta: Cuenta; subcuenta: Subcuenta } | null>(null)
  const [transferenciaAEliminar, setTransferenciaAEliminar] = useState<Transferencia | null>(null)

  // Los totales van por moneda: nunca se suman pesos con dólares.
  const subcuentas = cuentas.flatMap((c) => c.subcuentas)
  const totales = sumarPorMoneda(subcuentas)
  const deudas = sumarPorMoneda(subcuentas.filter((s) => s.saldo < 0))
  const puedeTransferir = subcuentas.length >= 2

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold text-slate-900">Cuentas</h1>
        <div className="flex flex-wrap gap-2">
          <Boton
            variante="secundario"
            onClick={() => setFormTransferencia({ abierto: true, transferencia: null })}
            disabled={!puedeTransferir}
            title={puedeTransferir ? undefined : 'Necesitas al menos dos cuentas o subcuentas'}
          >
            <ArrowLeftRight className="size-4" aria-hidden />
            Transferir
          </Boton>
          <Boton onClick={() => setFormCuenta({ abierto: true, cuenta: null })}>
            <Plus className="size-4" aria-hidden />
            Nueva cuenta
          </Boton>
        </div>
      </div>

      {isError && <Alerta>{error.message}</Alerta>}

      {isLoading ? (
        <Cargando />
      ) : (
        <>
          <Tarjeta>
            <p className="text-sm text-slate-500">Saldo total</p>
            <div className="flex flex-wrap gap-x-8 gap-y-1">
              {(totales.length ? totales : [{ moneda: 'UYU' as const, saldo: 0 }]).map((t) => {
                const deuda = deudas.find((d) => d.moneda === t.moneda)?.saldo ?? 0
                return (
                  <div key={t.moneda}>
                    <p className={`text-3xl font-semibold tabular-nums ${t.saldo < 0 ? 'text-rose-600' : 'text-slate-900'}`}>
                      {formatMonto(t.saldo, t.moneda)}
                    </p>
                    {deuda < 0 && (
                      <p className="mt-1 text-sm text-slate-500">
                        Disponible <span className="tabular-nums text-slate-700">{formatMonto(t.saldo - deuda, t.moneda)}</span>{' '}
                        · Deudas <span className="tabular-nums text-rose-600">{formatMonto(deuda, t.moneda)}</span>
                      </p>
                    )}
                  </div>
                )
              })}
            </div>
          </Tarjeta>

          {cuentas.length === 0 ? (
            <Tarjeta>
              <Vacio titulo="No tienes cuentas. Crea una para registrar desde dónde gastas.">
                <Boton onClick={() => setFormCuenta({ abierto: true, cuenta: null })}>
                  <Plus className="size-4" aria-hidden />
                  Crear cuenta
                </Boton>
              </Vacio>
            </Tarjeta>
          ) : (
            <ul className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {cuentas.map((c) => (
                <li key={c.id}>
                  <Tarjeta className="flex h-full flex-col gap-3">
                    <div className="flex items-center gap-3">
                      <IconoCuenta cuenta={c} />
                      <div className="min-w-0 flex-1">
                        <p className="truncate font-medium text-slate-800">{c.nombre}</p>
                        <p className="text-xs text-slate-500">{datosTipoCuenta(c.tipo).texto}</p>
                      </div>
                    </div>

                    <div>
                      {c.saldos.map((s) => (
                        <p
                          key={s.moneda}
                          className={`text-xl font-semibold tabular-nums ${s.saldo < 0 ? 'text-rose-600' : 'text-slate-900'}`}
                        >
                          {formatMonto(s.saldo, s.moneda)}
                        </p>
                      ))}
                    </div>

                    <ul className="space-y-1 border-t border-slate-100 pt-2">
                      {c.subcuentas.map((s) => (
                        <li key={s.id} className="flex items-center gap-2">
                          <CornerDownRight className="size-3.5 shrink-0 text-slate-300" aria-hidden />
                          <span className="min-w-0 flex-1 truncate text-sm text-slate-600">{s.nombre}</span>
                          <span className={`shrink-0 text-sm tabular-nums ${s.saldo < 0 ? 'text-rose-600' : 'text-slate-800'}`}>
                            {formatMonto(s.saldo, s.moneda)}
                          </span>
                          <button
                            type="button"
                            onClick={() => setFormSubcuenta({ abierto: true, cuenta: c, subcuenta: s })}
                            className="rounded-md p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
                            aria-label={`Editar ${s.nombre}`}
                          >
                            <Pencil className="size-3.5" />
                          </button>
                          {c.subcuentas.length > 1 && (
                            <button
                              type="button"
                              onClick={() => setSubcuentaAEliminar({ cuenta: c, subcuenta: s })}
                              className="rounded-md p-1 text-slate-400 hover:bg-rose-50 hover:text-rose-600"
                              aria-label={`Eliminar ${s.nombre}`}
                            >
                              <Trash2 className="size-3.5" />
                            </button>
                          )}
                        </li>
                      ))}
                    </ul>

                    <div className="mt-auto flex items-center gap-1 border-t border-slate-100 pt-2">
                      <Link
                        to={`/movimientos?cuenta=${c.id}${mes ? `&mes=${mes}` : ''}`}
                        className="mr-auto flex items-center gap-1.5 rounded-md px-2 py-1 text-sm font-medium text-emerald-700 hover:bg-emerald-50"
                      >
                        <List className="size-4" aria-hidden />
                        Movimientos
                      </Link>
                      <button
                        type="button"
                        onClick={() => setFormSubcuenta({ abierto: true, cuenta: c, subcuenta: null })}
                        className="rounded-md p-1.5 text-slate-400 hover:bg-emerald-50 hover:text-emerald-700"
                        aria-label={`Agregar subcuenta a ${c.nombre}`}
                        title="Agregar subcuenta"
                      >
                        <ListPlus className="size-4" />
                      </button>
                      <button
                        type="button"
                        onClick={() => setFormCuenta({ abierto: true, cuenta: c })}
                        className="rounded-md p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
                        aria-label={`Editar ${c.nombre}`}
                      >
                        <Pencil className="size-4" />
                      </button>
                      <button
                        type="button"
                        onClick={() => setCuentaAEliminar(c)}
                        className="rounded-md p-1.5 text-slate-400 hover:bg-rose-50 hover:text-rose-600"
                        aria-label={`Eliminar ${c.nombre}`}
                      >
                        <Trash2 className="size-4" />
                      </button>
                    </div>
                  </Tarjeta>
                </li>
              ))}
            </ul>
          )}
        </>
      )}

      <Tarjeta className={transferencias.isPlaceholderData ? 'opacity-60 transition-opacity' : ''}>
        <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
          <h2 className="font-semibold text-slate-900">Transferencias</h2>
          <SelectorMes mes={mes} onChange={setMes} />
        </div>
        {transferencias.isError && <Alerta>{transferencias.error.message}</Alerta>}
        {transferencias.isLoading ? (
          <Cargando />
        ) : !transferencias.data?.items.length ? (
          <Vacio titulo="No hay transferencias en este mes." />
        ) : (
          <ul className="divide-y divide-slate-100">
            {transferencias.data.items.map((t) => (
              <FilaTransferencia
                key={t.id}
                transferencia={t}
                cuentas={cuentas}
                onEditar={() => setFormTransferencia({ abierto: true, transferencia: t })}
                onEliminar={() => setTransferenciaAEliminar(t)}
              />
            ))}
          </ul>
        )}
      </Tarjeta>

      <CuentaFormulario
        abierto={formCuenta.abierto}
        cuenta={formCuenta.cuenta}
        onCerrar={() => setFormCuenta({ abierto: false, cuenta: null })}
      />

      <SubcuentaFormulario
        abierto={formSubcuenta.abierto}
        cuenta={formSubcuenta.cuenta}
        subcuenta={formSubcuenta.subcuenta}
        onCerrar={() => setFormSubcuenta((f) => ({ ...f, abierto: false }))}
      />

      <TransferenciaFormulario
        abierto={formTransferencia.abierto}
        transferencia={formTransferencia.transferencia}
        onCerrar={() => setFormTransferencia({ abierto: false, transferencia: null })}
      />

      <Confirmar
        titulo="Eliminar cuenta"
        abierto={cuentaAEliminar !== null}
        onCerrar={() => setCuentaAEliminar(null)}
        onConfirmar={() => eliminarCuenta.mutateAsync(cuentaAEliminar!.id)}
      >
        ¿Seguro que quieres eliminar la cuenta <strong className="text-slate-800">{cuentaAEliminar?.nombre}</strong> con
        sus subcuentas? Si tiene movimientos o transferencias no se podrá eliminar.
      </Confirmar>

      <Confirmar
        titulo="Eliminar subcuenta"
        abierto={subcuentaAEliminar !== null}
        onCerrar={() => setSubcuentaAEliminar(null)}
        onConfirmar={() =>
          eliminarSubcuenta.mutateAsync({ cuentaId: subcuentaAEliminar!.cuenta.id, id: subcuentaAEliminar!.subcuenta.id })
        }
      >
        ¿Seguro que quieres eliminar la subcuenta{' '}
        <strong className="text-slate-800">{subcuentaAEliminar?.subcuenta.nombre}</strong> de{' '}
        {subcuentaAEliminar?.cuenta.nombre}? Si tiene movimientos o transferencias no se podrá eliminar.
      </Confirmar>

      <Confirmar
        titulo="Eliminar transferencia"
        abierto={transferenciaAEliminar !== null}
        onCerrar={() => setTransferenciaAEliminar(null)}
        onConfirmar={() => eliminarTransferencia.mutateAsync(transferenciaAEliminar!.id)}
      >
        ¿Seguro que quieres eliminar la transferencia de{' '}
        <strong className="text-slate-800">
          {transferenciaAEliminar &&
            formatMonto(transferenciaAEliminar.monto, transferenciaAEliminar.subcuentaOrigen.moneda)}
        </strong>{' '}
        de {transferenciaAEliminar?.origen.nombre} a {transferenciaAEliminar?.destino.nombre}? Los saldos vuelven a como
        estaban.
      </Confirmar>
    </div>
  )
}

function FilaTransferencia({
  transferencia: t,
  cuentas,
  onEditar,
  onEliminar,
}: {
  transferencia: Transferencia
  cuentas: Cuenta[]
  onEditar: () => void
  onEliminar: () => void
}) {
  // La subcuenta se nombra si su cuenta tiene varias, o si es una transferencia dentro de la misma cuenta.
  const nombre = (cuentaId: string, cuentaNombre: string, subcuentaNombre: string) => {
    const varias = (cuentas.find((c) => c.id === cuentaId)?.subcuentas.length ?? 0) > 1
    return varias || t.origen.id === t.destino.id ? `${cuentaNombre} › ${subcuentaNombre}` : cuentaNombre
  }
  const cambioDeMoneda = t.subcuentaOrigen.moneda !== t.subcuentaDestino.moneda

  return (
    <li className="group flex items-center gap-3 py-3">
      <button
        type="button"
        onClick={onEditar}
        className="flex min-w-0 flex-1 items-center gap-3 rounded-lg text-left focus-visible:outline-2 focus-visible:outline-emerald-600"
      >
        <span className="inline-flex size-10 shrink-0 items-center justify-center rounded-full bg-slate-100 text-slate-500">
          <ArrowLeftRight className="size-5" aria-hidden />
        </span>
        <span className="min-w-0 flex-1">
          <span className="flex items-center gap-1 text-sm font-medium text-slate-800">
            <span className="truncate">{nombre(t.origen.id, t.origen.nombre, t.subcuentaOrigen.nombre)}</span>
            <ArrowRight className="size-3.5 shrink-0 text-slate-400" aria-label="a" />
            <span className="truncate">{nombre(t.destino.id, t.destino.nombre, t.subcuentaDestino.nombre)}</span>
          </span>
          <span className="block truncate text-xs text-slate-500">
            {[t.nota, formatFecha(t.fecha)].filter(Boolean).join(' · ')}
          </span>
        </span>
        <span className="shrink-0 text-right text-sm font-semibold tabular-nums text-slate-800">
          {formatMonto(t.monto, t.subcuentaOrigen.moneda)}
          {cambioDeMoneda && (
            <span className="block text-xs font-normal text-slate-500">
              → {formatMonto(t.montoDestino, t.subcuentaDestino.moneda)}
            </span>
          )}
        </span>
      </button>
      <button
        type="button"
        onClick={onEliminar}
        className="shrink-0 rounded-md p-1.5 text-slate-400 hover:bg-rose-50 hover:text-rose-600 sm:opacity-0 sm:transition sm:group-focus-within:opacity-100 sm:group-hover:opacity-100"
        aria-label="Eliminar"
      >
        <Trash2 className="size-4" />
      </button>
    </li>
  )
}
