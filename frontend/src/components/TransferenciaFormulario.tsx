import { ArrowDown } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { ApiError } from '../api/cliente'
import { useCuentas, useGuardarTransferencia } from '../api/consultas'
import type { Transferencia } from '../api/tipos'
import { formatMonto, hoyIso, montoParaEditar, parsearMonto } from '../lib/formato'
import { Modal } from './Modal'
import { Alerta, Boton, Campo, Select } from './ui'

interface Props {
  abierto: boolean
  onCerrar: () => void
  /** Si se pasa, se edita esa transferencia; si no, se crea una nueva. */
  transferencia?: Transferencia | null
}

export function TransferenciaFormulario({ abierto, onCerrar, transferencia }: Props) {
  return (
    <Modal titulo={transferencia ? 'Editar transferencia' : 'Transferir entre cuentas'} abierto={abierto} onCerrar={onCerrar}>
      <Formulario transferencia={transferencia ?? null} onListo={onCerrar} />
    </Modal>
  )
}

function Formulario({ transferencia, onListo }: { transferencia: Transferencia | null; onListo: () => void }) {
  const { data: cuentas = [], isLoading } = useCuentas()
  const guardar = useGuardarTransferencia()

  const [origenId, setOrigenId] = useState(transferencia?.origen.id ?? '')
  const [destinoId, setDestinoId] = useState(transferencia?.destino.id ?? '')
  const [monto, setMonto] = useState(transferencia ? montoParaEditar(transferencia.monto) : '')
  const [fecha, setFecha] = useState(transferencia?.fecha ?? hoyIso())
  const [nota, setNota] = useState(transferencia?.nota ?? '')
  const [error, setError] = useState<ApiError | null>(null)

  const enviar = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)

    const valor = parsearMonto(monto)
    if (Number.isNaN(valor)) {
      setError(new ApiError(400, '', { monto: ['Escribe un monto válido, por ejemplo 1500 o 1.500,50.'] }))
      return
    }

    try {
      await guardar.mutateAsync({
        id: transferencia?.id,
        datos: { cuentaOrigenId: origenId, cuentaDestinoId: destinoId, monto: valor, fecha, nota: nota.trim() || null },
      })
      onListo()
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(0, 'Ocurrió un error inesperado.'))
    }
  }

  const errorGeneral = error && Object.keys(error.errores).length === 0 ? error.message : null

  const opciones = (
    <>
      <option value="">{isLoading ? 'Cargando…' : 'Elige una cuenta'}</option>
      {cuentas.map((c) => (
        <option key={c.id} value={c.id}>
          {c.nombre} ({formatMonto(c.saldo)})
        </option>
      ))}
    </>
  )

  return (
    <form onSubmit={enviar} className="space-y-4" noValidate>
      {!isLoading && cuentas.length < 2 && (
        <p className="rounded-lg bg-slate-50 px-3 py-2 text-sm text-slate-600">
          Para transferir necesitas al menos dos cuentas. Crea otra desde la página de cuentas.
        </p>
      )}

      <div>
        <Select
          etiqueta="Desde"
          value={origenId}
          onChange={(e) => setOrigenId(e.target.value)}
          error={error?.campo('cuentaOrigenId')}
          disabled={isLoading}
          required
        >
          {opciones}
        </Select>
        <ArrowDown className="mx-auto mt-2 -mb-1 size-4 text-slate-400" aria-hidden />
        <Select
          etiqueta="Hacia"
          value={destinoId}
          onChange={(e) => setDestinoId(e.target.value)}
          error={error?.campo('cuentaDestinoId')}
          disabled={isLoading}
          required
        >
          {opciones}
        </Select>
      </div>

      <Campo
        etiqueta="Monto"
        inputMode="decimal"
        autoComplete="off"
        placeholder="0,00"
        value={monto}
        onChange={(e) => setMonto(e.target.value)}
        error={error?.campo('monto')}
        required
      />

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
        placeholder="Ej: retiro del cajero, pago de la tarjeta"
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
