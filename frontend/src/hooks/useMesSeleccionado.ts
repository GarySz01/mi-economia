import { useSearchParams } from 'react-router'
import { esMesValido, mesActual } from '../lib/formato'

/**
 * Mes seleccionado ("yyyy-MM"), guardado en la URL (?mes=2026-10) para que se mantenga
 * al navegar entre el panel y los movimientos, y al recargar la página.
 */
export function useMesSeleccionado() {
  const [params, setParams] = useSearchParams()
  const valor = params.get('mes')
  const mes = esMesValido(valor) ? valor : mesActual()

  const setMes = (nuevo: string) =>
    setParams(
      (anteriores) => {
        const siguientes = new URLSearchParams(anteriores)
        if (nuevo === mesActual()) siguientes.delete('mes')
        else siguientes.set('mes', nuevo)
        return siguientes
      },
      { replace: true },
    )

  return [mes, setMes] as const
}
