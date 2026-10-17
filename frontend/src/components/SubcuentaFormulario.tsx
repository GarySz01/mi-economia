import { useState, type FormEvent } from 'react'
import { ApiError } from '../api/cliente'
import { useGuardarSubcuenta } from '../api/consultas'
import type { Cuenta, Moneda, Subcuenta } from '../api/tipos'
import { montoParaEditar, parsearMontoConSigno } from '../lib/formato'
import { MONEDA_POR_DEFECTO, MONEDAS } from '../lib/monedas'
import { IconoCuenta } from './IconoCuenta'
import { Modal } from './Modal'
import { Alerta, Boton, Campo, Select } from './ui'

interface Props {
  abierto: boolean
  onCerrar: () => void
  /** Cuenta a la que pertenece (o va a pertenecer) la subcuenta. */
  cuenta: Cuenta | null
  /** Si se pasa, se edita esa subcuenta; si no, se crea una nueva. */
  subcuenta?: Subcuenta | null
}

export function SubcuentaFormulario({ abierto, onCerrar, cuenta, subcuenta }: Props) {
  return (
    <Modal titulo={subcuenta ? 'Editar subcuenta' : 'Nueva subcuenta'} abierto={abierto} onCerrar={onCerrar}>
      {cuenta && <Formulario cuenta={cuenta} subcuenta={subcuenta ?? null} onListo={onCerrar} />}
    </Modal>
  )
}

function Formulario({ cuenta, subcuenta, onListo }: { cuenta: Cuenta; subcuenta: Subcuenta | null; onListo: () => void }) {
  const guardar = useGuardarSubcuenta()
  const [nombre, setNombre] = useState(subcuenta?.nombre ?? '')
  // Una subcuenta nueva arranca con la moneda de la primera de la cuenta.
  const [moneda, setMoneda] = useState<Moneda>(subcuenta?.moneda ?? cuenta.subcuentas[0]?.moneda ?? MONEDA_POR_DEFECTO)
  const [saldoInicial, setSaldoInicial] = useState(subcuenta ? montoParaEditar(subcuenta.saldoInicial) : '')
  const [error, setError] = useState<ApiError | null>(null)

  const enviar = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)

    const saldo = parsearMontoConSigno(saldoInicial)
    if (Number.isNaN(saldo)) {
      setError(new ApiError(400, '', { saldoInicial: ['Escribe un monto válido, por ejemplo 1500, 1.500,50 o -2000.'] }))
      return
    }

    try {
      await guardar.mutateAsync({ cuentaId: cuenta.id, id: subcuenta?.id, datos: { nombre, moneda, saldoInicial: saldo } })
      onListo()
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(0, 'Ocurrió un error inesperado.'))
    }
  }

  const errorGeneral = error && Object.keys(error.errores).length === 0 ? error.message : null

  return (
    <form onSubmit={enviar} className="space-y-4" noValidate>
      <div className="flex items-center gap-2 text-sm text-slate-600">
        <IconoCuenta cuenta={cuenta} tamanio="sm" />
        <span>
          Dentro de <strong className="text-slate-800">{cuenta.nombre}</strong>
        </span>
      </div>

      <Campo
        etiqueta="Nombre"
        maxLength={50}
        placeholder="Ej: Día a día, Ahorro viaje, Plan de inversión"
        value={nombre}
        onChange={(e) => setNombre(e.target.value)}
        error={error?.campo('nombre')}
        autoFocus
        required
      />

      <Select
        etiqueta="Moneda"
        value={moneda}
        onChange={(e) => setMoneda(e.target.value as Moneda)}
        error={error?.campo('moneda')}
      >
        {MONEDAS.map((m) => (
          <option key={m.moneda} value={m.moneda}>
            {m.nombre} ({m.simbolo})
          </option>
        ))}
      </Select>

      <Campo
        etiqueta="Saldo inicial"
        inputMode="decimal"
        autoComplete="off"
        placeholder="0,00"
        value={saldoInicial}
        onChange={(e) => setSaldoInicial(e.target.value)}
        error={error?.campo('saldoInicial')}
        ayuda="Cuánto dinero tiene esta subcuenta antes de cargar movimientos en la app."
      />

      <Alerta>{errorGeneral}</Alerta>

      <div className="flex justify-end gap-2 pt-1">
        <Boton variante="secundario" onClick={onListo}>
          Cancelar
        </Boton>
        <Boton type="submit" cargando={guardar.isPending}>
          {subcuenta ? 'Guardar cambios' : 'Agregar'}
        </Boton>
      </div>
    </form>
  )
}
