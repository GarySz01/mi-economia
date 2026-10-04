import { Tag } from 'lucide-react'
import { COLOR_POR_DEFECTO, ICONOS } from '../lib/iconos'

interface Props {
  icono: string | null
  color: string | null
  /** Tamaño del círculo: "sm" (32px) o "md" (40px). */
  tamanio?: 'sm' | 'md'
}

/** Ícono de la categoría dentro de un círculo de su color. */
export function IconoCategoria({ icono, color, tamanio = 'md' }: Props) {
  const Icono = (icono && ICONOS[icono]) || Tag
  const c = color ?? COLOR_POR_DEFECTO
  const clases = tamanio === 'sm' ? 'size-8' : 'size-10'
  return (
    <span
      className={`${clases} inline-flex shrink-0 items-center justify-center rounded-full`}
      style={{ backgroundColor: `${c}1F`, color: c }}
    >
      <Icono className={tamanio === 'sm' ? 'size-4' : 'size-5'} aria-hidden />
    </span>
  )
}
