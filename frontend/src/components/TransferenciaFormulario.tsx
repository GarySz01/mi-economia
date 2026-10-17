import { ArrowDown } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { ApiError } from '../api/cliente'
import { useCuentas, useGuardarTransferencia } from '../api/consultas'
import type { Transferencia } from '../api/tipos'
import { subcuentaEfectiva } from '../lib/cuentas'
import { hoyIso, montoParaEditar, parsearMonto } from '../lib/formato'
import { simboloMoneda } from '../lib/monedas'
import { Modal } from './Modal'
import { SelectorCuenta } from './SelectorCuenta'
import { Alerta, Boton, Campo } from './ui'

interface Props {
  abierto: boolean
  onCerrar: () => void
  /** Si se pasa, se edita esa transferencia; si no, se crea una nueva. */
  transferencia?: Transferencia | null
}

export function TransferenciaFormulario({ abierto, onCerrar, transferencia }: Props) {
  return (
    <Modal titulo={transferencia ? 'Editar transferencia' : 'Transferir'} abierto={abierto} onCerrar={onCerrar}>
      <Formulario transferencia={transferencia ?? null} onListo={onCerrar} />
    </Modal>
  )
}

function Formulario({ transferencia, onListo }: { transferencia: Transferencia | null; onListo: () => void }) {
  const { data: cuentas = [], isLoading } = useCuentas()
  const guardar = useGuardarTransferencia()

  const [origen, setOrigen] = useState({
    cuentaId: transferencia?.origen.id ?? '',
    subcuentaId: transferencia?.subcuentaOrigen.id ?? '',
  })
  const [destino, setDestino] = useState({
    cuentaId: transferencia?.destino.id ?? '',
    subcuentaId: transferencia?.subcuentaDestino.id ?? '',
  })
  const [monto, setMonto] = useState(transferencia ? montoParaEditar(transferencia.monto) : '')
  const [montoDestino, setMontoDestino] = useState(transferencia ? montoParaEditar(transferencia.montoDestino) : '')
  const [fecha, setFecha] = useState(transferencia?.fecha ?? hoyIso())
  const [nota, setNota] = useState(transferencia?.nota ?? '')
  const [error, setError] = useState<ApiError | null>(null)

  const subOrigen = subcuentaEfectiva(cuentas.find((c) => c.id === origen.cuentaId), origen.subcuentaId)
  const subDestino = subcuentaEfectiva(cuentas.find((c) => c.id === destino.cuentaId), destino.subcuentaId)
  // Entre monedas distintas (por ejemplo, comprar dólares) hay que decir cuánto llega.
  const monedasDistintas = !!subOrigen && !!subDestino && subOrigen.moneda !== subDestino.moneda
  // Se puede transferir dentro de una misma cuenta si tiene más de una subcuenta.
  const hayDosSubcuentas = cuentas.reduce((suma, c) => suma + c.subcuentas.length, 0) >= 2

  const enviar = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)

    const valor = parsearMonto(monto)
    const valorDestino = monedasDistintas ? parsearMonto(montoDestino) : null
    if (Number.isNaN(valor) || Number.isNaN(valorDestino)) {
      const mensaje = 'Escribe un monto válido, por ejemplo 1500 o 1.500,50.'
      setError(
        new ApiError(400, '', {
          ...(Number.isNaN(valor) && { monto: [mensaje] }),
          ...(Number.isNaN(valorDestino) && { montoDestino: [mensaje] }),
        }),
      )
      return
    }

    try {
      await guardar.mutateAsync({
        id: transferencia?.id,
        datos: {
          cuentaOrigenId: origen.cuentaId,
          subcuentaOrigenId: subOrigen?.id ?? null,
          cuentaDestinoId: destino.cuentaId,
          subcuentaDestinoId: subDestino?.id ?? null,
          monto: valor,
          montoDestino: valorDestino,
          fecha,
          nota: nota.trim() || null,
        },
      })
      onListo()
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(0, 'Ocurrió un error inesperado.'))
    }
  }

  const errorGeneral = error && Object.keys(error.errores).length === 0 ? error.message : null

  return (
    <form onSubmit={enviar} className="space-y-4" noValidate>
      {!isLoading && !hayDosSubcuentas && (
        <p className="rounded-lg bg-slate-50 px-3 py-2 text-sm text-slate-600">
          Para transferir necesitas al menos dos cuentas, o una cuenta con dos subcuentas.
        </p>
      )}

      <div>
        <SelectorCuenta
          etiqueta="Desde"
          cuentas={cuentas}
          cargando={isLoading}
          cuentaId={origen.cuentaId}
          subcuentaId={origen.subcuentaId}
          onChange={(cuentaId, subcuentaId) => setOrigen({ cuentaId, subcuentaId })}
          errorCuenta={error?.campo('cuentaOrigenId')}
          errorSubcuenta={error?.campo('subcuentaOrigenId')}
          conSaldos
        />
        <ArrowDown className="mx-auto mt-2 -mb-1 size-4 text-slate-400" aria-hidden />
        <SelectorCuenta
          etiqueta="Hacia"
          cuentas={cuentas}
          cargando={isLoading}
          cuentaId={destino.cuentaId}
          subcuentaId={destino.subcuentaId}
          onChange={(cuentaId, subcuentaId) => setDestino({ cuentaId, subcuentaId })}
          errorCuenta={error?.campo('cuentaDestinoId')}
          errorSubcuenta={error?.campo('subcuentaDestinoId')}
          conSaldos
        />
      </div>

      <div className={monedasDistintas ? 'grid grid-cols-2 gap-3' : ''}>
        <Campo
          etiqueta={subOrigen ? `${monedasDistintas ? 'Sale' : 'Monto'} (${simboloMoneda(subOrigen.moneda)})` : 'Monto'}
          inputMode="decimal"
          autoComplete="off"
          placeholder="0,00"
          value={monto}
          onChange={(e) => setMonto(e.target.value)}
          error={error?.campo('monto')}
          required
        />
        {monedasDistintas && subDestino && (
          <Campo
            etiqueta={`Llega (${simboloMoneda(subDestino.moneda)})`}
            inputMode="decimal"
            autoComplete="off"
            placeholder="0,00"
            value={montoDestino}
            onChange={(e) => setMontoDestino(e.target.value)}
            error={error?.campo('montoDestino')}
            required
          />
        )}
      </div>

      <Campo
        etiqueta="Fecha"
        type="date"
        value={fecha}
        onChange={(e) => setFecha(e.target.value)}
        error={error?.campo('fecha')}
        required
      />

      <Campo
        etiqueta="Nota (opcional)"
        maxLength={250}
        placeholder="Ej: retiro del cajero, compra de dólares, ahorro del mes"
        value={nota}
        onChange={(e) => setNota(e.target.value)}
        error={error?.campo('nota')}
      />

      <Alerta>{errorGeneral}</Alerta>

      <div className="flex justify-end gap-2 pt-1">
        <Boton variante="secundario" onClick={onListo}>
          Cancelar
        </Boton>
        <Boton type="submit" cargando={guardar.isPending}>
          {transferencia ? 'Guardar cambios' : 'Transferir'}
        </Boton>
      </div>
    </form>
  )
}
