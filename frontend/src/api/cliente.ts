/**
 * Error devuelto por la API. Los mensajes ya vienen en español desde el backend
 * (ProblemDetails), así que se pueden mostrar tal cual.
 */
export class ApiError extends Error {
  readonly status: number
  /** Errores por campo, con el nombre del campo en camelCase (ej. "monto"). */
  readonly errores: Record<string, string[]>

  constructor(status: number, mensaje: string, errores: Record<string, string[]> = {}) {
    super(mensaje)
    this.name = 'ApiError'
    this.status = status
    this.errores = errores
  }

  /** Primer error de un campo, o undefined. */
  campo(nombre: string): string | undefined {
    return this.errores[nombre]?.[0]
  }
}

const CLAVE_TOKEN = 'mi-economia.token'

let token: string | null = leerToken()
let alExpirarSesion: () => void = () => {}

function leerToken(): string | null {
  try {
    return localStorage.getItem(CLAVE_TOKEN)
  } catch {
    return null
  }
}

export function setToken(nuevo: string | null) {
  token = nuevo
  try {
    if (nuevo) localStorage.setItem(CLAVE_TOKEN, nuevo)
    else localStorage.removeItem(CLAVE_TOKEN)
  } catch {
    // Sin localStorage (modo privado estricto) la sesión dura solo mientras la pestaña esté abierta.
  }
}

export function getToken() {
  return token
}

/** Se llama cuando la API responde 401 con una sesión activa (token vencido o inválido). */
export function onSesionExpirada(callback: () => void) {
  alExpirarSesion = callback
}

const aCamelCase = (s: string) => s.charAt(0).toLowerCase() + s.slice(1)

export async function api<T>(ruta: string, opciones: { method?: string; body?: unknown } = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (opciones.body !== undefined) headers['Content-Type'] = 'application/json'
  if (token) headers.Authorization = `Bearer ${token}`

  let respuesta: Response
  try {
    respuesta = await fetch(`/api${ruta}`, {
      method: opciones.method ?? 'GET',
      headers,
      body: opciones.body === undefined ? undefined : JSON.stringify(opciones.body),
    })
  } catch {
    throw new ApiError(0, 'No se pudo conectar con el servidor. Revisa tu conexión.')
  }

  if (respuesta.status === 401 && token) {
    alExpirarSesion()
  }

  if (!respuesta.ok) {
    const problema = await respuesta.json().catch(() => null)
    const errores: Record<string, string[]> = {}
    for (const [campo, mensajes] of Object.entries(problema?.errors ?? {})) {
      errores[aCamelCase(campo)] = mensajes as string[]
    }
    throw new ApiError(respuesta.status, problema?.title ?? mensajePorEstado(respuesta.status), errores)
  }

  if (respuesta.status === 204) return undefined as T
  return respuesta.json() as Promise<T>
}

function mensajePorEstado(status: number) {
  switch (status) {
    case 401:
      return 'Necesitas iniciar sesión.'
    case 429:
      return 'Demasiados intentos. Espera un minuto y vuelve a probar.'
    default:
      return status >= 500 ? 'Ocurrió un error en el servidor. Intenta de nuevo.' : 'La solicitud no es válida.'
  }
}

/** Arma un query string ignorando los valores vacíos. */
export function query(params: object) {
  const qs = new URLSearchParams()
  for (const [clave, valor] of Object.entries(params)) {
    if (valor !== undefined && valor !== null && valor !== '') qs.set(clave, String(valor))
  }
  const texto = qs.toString()
  return texto ? `?${texto}` : ''
}
