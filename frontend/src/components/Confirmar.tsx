import { useState, type ReactNode } from 'react'
import { ApiError } from '../api/cliente'
import { Modal } from './Modal'
import { Alerta, Boton } from './ui'

interface Props {
  titulo: string
  abierto: boolean
  onCerrar: () => void
  /** Acción a confirmar. Si falla, el mensaje de error se muestra dentro de la ventana. */
  onConfirmar: () => Promise<unknown>
  textoConfirmar?: string
  children: ReactNode
}

export function Confirmar({ titulo, abierto, onCerrar, onConfirmar, textoConfirmar = 'Eliminar', children }: Props) {
  const [cargando, setCargando] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const cerrar = () => {
    setError(null)
    onCerrar()
  }

  const confirmar = async () => {
    setCargando(true)
    setError(null)
    try {
      await onConfirmar()
      cerrar()
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Ocurrió un error inesperado.')
    } finally {
      setCargando(false)
    }
  }

  return (
    <Modal titulo={titulo} abierto={abierto} onCerrar={cerrar}>
      <div className="space-y-4">
        <div className="text-sm text-slate-600">{children}</div>
        <Alerta>{error}</Alerta>
        <div className="flex justify-end gap-2">
          <Boton variante="secundario" onClick={cerrar}>
            Cancelar
          </Boton>
          <Boton variante="peligro" onClick={confirmar} cargando={cargando}>
            {textoConfirmar}
          </Boton>
        </div>
      </div>
    </Modal>
  )
}
