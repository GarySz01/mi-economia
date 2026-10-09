import { ArrowLeftRight, ArrowRight, List, Pencil, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { useCuentas, useEliminarCuenta, useEliminarTransferencia, useTransferencias } from '../api/consultas'
import type { Cuenta, Transferencia } from '../api/tipos'
import { Confirmar } from '../components/Confirmar'
import { CuentaFormulario } from '../components/CuentaFormulario'
import { IconoCuenta } from '../components/IconoCuenta'
import { SelectorMes } from '../components/SelectorMes'
import { TransferenciaFormulario } from '../components/TransferenciaFormulario'
import { Alerta, Boton, Cargando, Tarjeta, Vacio } from '../components/ui'
import { useMesSeleccionado } from '../hooks/useMesSeleccionado'
import { datosTipoCuenta } from '../lib/cuentas'
import { formatFecha, formatMonto, rangoDelMes } from '../lib/formato'

export function Cuentas() {
  const { data: cuentas = [], isLoading, isError, error } = useCuentas()
  const eliminarCuenta = useEliminarCuenta()
  const eliminarTransferencia = useEliminarTransferencia()

  const [mes, setMes] = useMesSeleccionado()
  const { desde, hasta } = rangoDelMes(mes)
  const transferencias = useTransferencias({ desde, hasta, tamanio: 200 })

  const [formCuenta, setFormCuenta] = useState<{ abierto: boolean; cuenta: Cuenta | null }>({ abierto: false, cuenta: null })
  const [formTransferencia, setFormTransferencia] = useState<{ abierto: boolean; transferencia: Transferencia | null }>({
    abierto: false,
    transferencia: null,
  })
  const [cuentaAEliminar, setCuentaAEliminar] = useState<Cuenta | null>(null)
  const [transferenciaAEliminar, setTransferenciaAEliminar] = useState<Transferencia | null>(null)

  const total = cuentas.reduce((suma, c) => suma + c.saldo, 0)
  const deudas = cuentas.filter((c) => c.saldo < 0).reduce((suma, c) => suma + c.saldo, 0)

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold text-slate-900">Cuentas</h1>
        <div className="flex flex-wrap gap-2">
          <Boton
            variante="secundario"
            onClick={() => setFormTransferencia({ abierto: true, transferencia: null })}
            disabled={cuentas.length < 2}
            title={cuentas.length < 2 ? 'Necesitas al menos dos cuentas' : undefined}
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
            <p className={`text-3xl font-semibold tabular-nums ${total < 0 ? 'text-rose-600' : 'text-slate-900'}`}>
              {formatMonto(total)}
            </p>
            {deudas < 0 && (
              <p className="mt-1 text-sm text-slate-500">
                Disponible <span className="tabular-nums text-slate-700">{formatMonto(total - deudas)}</span> · Deudas{' '}
                <span className="tabular-nums text-rose-600">{formatMonto(deudas)}</span>
              </p>
            )}
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
                    <p className={`text-xl font-semibold tabular-nums ${c.saldo < 0 ? 'text-rose-600' : 'text-slate-900'}`}>
                      {formatMonto(c.saldo)}
                    </p>
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
        ¿Seguro que quieres eliminar la cuenta <strong className="text-slate-800">{cuentaAEliminar?.nombre}</strong>? Si
        tiene movimientos o transferencias no se podrá eliminar.
      </Confirmar>

      <Confirmar
        titulo="Eliminar transferencia"
        abierto={transferenciaAEliminar !== null}
        onCerrar={() => setTransferenciaAEliminar(null)}
        onConfirmar={() => eliminarTransferencia.mutateAsync(transferenciaAEliminar!.id)}
      >
        ¿Seguro que quieres eliminar la transferencia de{' '}
        <strong className="text-slate-800">
          {transferenciaAEliminar && formatMonto(transferenciaAEliminar.monto)}
        </strong>{' '}
        de {transferenciaAEliminar?.origen.nombre} a {transferenciaAEliminar?.destino.nombre}? Los saldos de las dos
        cuentas vuelven a como estaban.
      </Confirmar>
    </div>
  )
}

function FilaTransferencia({
  transferencia: t,
  onEditar,
  onEliminar,
}: {
  transferencia: Transferencia
  onEditar: () => void
  onEliminar: () => void
}) {
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
            <span className="truncate">{t.origen.nombre}</span>
            <ArrowRight className="size-3.5 shrink-0 text-slate-400" aria-label="a" />
            <span className="truncate">{t.destino.nombre}</span>
          </span>
          <span className="block truncate text-xs text-slate-500">
            {[t.nota, formatFecha(t.fecha)].filter(Boolean).join(' · ')}
          </span>
        </span>
        <span className="shrink-0 text-sm font-semibold tabular-nums text-slate-800">{formatMonto(t.monto)}</span>
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
