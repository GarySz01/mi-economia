import { useState, type FormEvent } from 'react'
import { ApiError } from '../api/cliente'
import { useGuardarCuenta } from '../api/consultas'
import type { Cuenta, Moneda, TipoCuenta } from '../api/tipos'
import { TIPOS_CUENTA } from '../lib/cuentas'
import { parsearMontoConSigno } from '../lib/formato'
import { COLORES } from '../lib/iconos'
import { MONEDA_POR_DEFECTO, MONEDAS } from '../lib/monedas'
import { IconoCuenta } from './IconoCuenta'
import { Modal } from './Modal'
import { SelectorColor } from './SelectorColor'
import { Alerta, Boton, Campo, Select } from './ui'

interface Props {
  abierto: boolean
  onCerrar: () => void
  /** Si se pasa, se edita esa cuenta; si no, se crea una nueva. */
  cuenta?: Cuenta | null
}

export function CuentaFormulario({ abierto, onCerrar, cuenta }: Props) {
  return (
    <Modal titulo={cuenta ? 'Editar cuenta' : 'Nueva cuenta'} abierto={abierto} onCerrar={onCerrar}>
      <Formulario cuenta={cuenta ?? null} onListo={onCerrar} />
    </Modal>
  )
}

function Formulario({ cuenta, onListo }: { cuenta: Cuenta | null; onListo: () => void }) {
  const guardar = useGuardarCuenta()
  const [nombre, setNombre] = useState(cuenta?.nombre ?? '')
  const [tipo, setTipo] = useState<TipoCuenta>(cuenta?.tipo ?? 'CuentaBancaria')
  // La moneda y el saldo inicial son los de la primera subcuenta; solo se piden al crear.
  const [moneda, setMoneda] = useState<Moneda>(MONEDA_POR_DEFECTO)
  const [saldoInicial, setSaldoInicial] = useState('')
  const [color, setColor] = useState(cuenta?.color ?? COLORES[4])
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
      await guardar.mutateAsync({ id: cuenta?.id, datos: { nombre, tipo, color, moneda, saldoInicial: saldo } })
      onListo()
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(0, 'Ocurrió un error inesperado.'))
    }
  }

  const errorGeneral = error && Object.keys(error.errores).length === 0 ? error.message : null

  return (
    <form onSubmit={enviar} className="space-y-4" noValidate>
      <div className="flex items-center gap-3">
        <IconoCuenta cuenta={{ id: '', nombre, tipo, color }} />
        <Campo
          className="flex-1"
          etiqueta="Nombre"
          maxLength={50}
          placeholder="Ej: Itaú, Visa, Mercado Pago"
          value={nombre}
          onChange={(e) => setNombre(e.target.value)}
          error={error?.campo('nombre')}
          autoFocus
          required
        />
      </div>

      <Select etiqueta="Tipo" value={tipo} onChange={(e) => setTipo(e.target.value as TipoCuenta)} error={error?.campo('tipo')}>
        {TIPOS_CUENTA.map((t) => (
          <option key={t.tipo} value={t.tipo}>
            {t.texto}
          </option>
        ))}
      </Select>

      {cuenta ? (
        <p className="text-xs text-slate-500">
          Los saldos y las monedas se editan en cada subcuenta, desde la tarjeta de la cuenta.
        </p>
      ) : (
        <>
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
            ayuda={
              tipo === 'TarjetaCredito'
                ? 'Lo que debes en la tarjeta, en negativo (por ejemplo -20.000).'
                : 'Queda en la subcuenta "Disponible". Después puedes agregar más subcuentas (ahorro, inversión, dólares…).'
            }
          />
        </>
      )}

      <SelectorColor valor={color} onChange={setColor} />

      <Alerta>{errorGeneral ?? error?.campo('color')}</Alerta>

      <div className="flex justify-end gap-2 pt-1">
        <Boton variante="secundario" onClick={onListo}>
          Cancelar
        </Boton>
        <Boton type="submit" cargando={guardar.isPending}>
          {cuenta ? 'Guardar cambios' : 'Crear cuenta'}
        </Boton>
      </div>
    </form>
  )
}
