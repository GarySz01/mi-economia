// Formatos de montos y fechas, y utilidades para trabajar con meses.
// Las fechas viajan como texto "yyyy-MM-dd" (DateOnly en la API) y se manejan
// siempre en hora local, sin pasar por UTC, para que no se corran de día.

const formatoMonto = new Intl.NumberFormat('es', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
  useGrouping: 'always',
})

export function formatMonto(valor: number) {
  const texto = formatoMonto.format(Math.abs(valor))
  return `${valor < 0 ? '-' : ''}$ ${texto}`
}

/** Versión corta para ejes de gráficos: 1.500.000 -> "$ 1,5 M". */
export function formatMontoCorto(valor: number) {
  return `$ ${new Intl.NumberFormat('es', { notation: 'compact', maximumFractionDigits: 1 }).format(valor)}`
}

const dos = (n: number) => String(n).padStart(2, '0')

export function aFechaIso(fecha: Date) {
  return `${fecha.getFullYear()}-${dos(fecha.getMonth() + 1)}-${dos(fecha.getDate())}`
}

export function hoyIso() {
  return aFechaIso(new Date())
}

function desdeIso(iso: string) {
  const [anio, mes, dia] = iso.split('-').map(Number)
  return new Date(anio, mes - 1, dia)
}

/** "2026-10-03" -> "3 oct 2026" */
export function formatFecha(iso: string) {
  return desdeIso(iso).toLocaleDateString('es', { day: 'numeric', month: 'short', year: 'numeric' })
}

/** "2026-10-03" -> "sábado, 3 de octubre" */
export function formatFechaLarga(iso: string) {
  const texto = desdeIso(iso).toLocaleDateString('es', { weekday: 'long', day: 'numeric', month: 'long' })
  return texto.charAt(0).toUpperCase() + texto.slice(1)
}

// ---- Meses, representados como "yyyy-MM" ----

export function mesActual() {
  return hoyIso().slice(0, 7)
}

export function esMesValido(mes: string | null): mes is string {
  return !!mes && /^\d{4}-(0[1-9]|1[0-2])$/.test(mes)
}

export function sumarMeses(mes: string, cantidad: number) {
  const [anio, m] = mes.split('-').map(Number)
  const fecha = new Date(anio, m - 1 + cantidad, 1)
  return `${fecha.getFullYear()}-${dos(fecha.getMonth() + 1)}`
}

/** Primer y último día del mes, en formato yyyy-MM-dd. */
export function rangoDelMes(mes: string) {
  const [anio, m] = mes.split('-').map(Number)
  const ultimoDia = new Date(anio, m, 0).getDate()
  return { desde: `${mes}-01`, hasta: `${mes}-${dos(ultimoDia)}` }
}

/** "2026-10" -> "Octubre 2026" */
export function nombreMes(mes: string) {
  const [anio, m] = mes.split('-').map(Number)
  const texto = new Date(anio, m - 1, 1).toLocaleDateString('es', { month: 'long', year: 'numeric' })
  return texto.charAt(0).toUpperCase() + texto.slice(1)
}

/** (2026, 10) -> "oct" */
export function nombreMesCorto(anio: number, mes: number) {
  return new Date(anio, mes - 1, 1).toLocaleDateString('es', { month: 'short' }).replace('.', '')
}

/** Convierte lo que escribe el usuario ("1.500,50", "1500.5", "1500") a número. Devuelve NaN si no es válido. */
export function parsearMonto(texto: string) {
  let limpio = texto.trim().replace(/\s|\$/g, '')
  if (limpio.includes(',')) {
    // Formato con coma decimal: los puntos son separadores de miles.
    limpio = limpio.replace(/\./g, '').replace(',', '.')
  } else if (/^\d{1,3}(\.\d{3})+$/.test(limpio)) {
    // "1.500" o "1.500.000": puntos de miles sin decimales.
    limpio = limpio.replace(/\./g, '')
  }
  return limpio === '' || !/^\d+(\.\d+)?$/.test(limpio) ? NaN : Number(limpio)
}
