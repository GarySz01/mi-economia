import { useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { getToken, onSesionExpirada, setToken } from '../api/cliente'
import { authApi } from '../api/endpoints'
import type { AuthResponse, Usuario } from '../api/tipos'
import { AuthContext } from './contexto'

const CLAVE_SESION = 'mi-economia.sesion'

interface SesionGuardada {
  usuario: Usuario
  expiraEn: string
}

/** Recupera la sesión guardada, si existe y el token no venció. */
function leerSesion(): Usuario | null {
  try {
    const guardada = localStorage.getItem(CLAVE_SESION)
    if (!guardada || !getToken()) return null
    const sesion = JSON.parse(guardada) as SesionGuardada
    if (new Date(sesion.expiraEn) <= new Date()) return null
    return sesion.usuario
  } catch {
    return null
  }
}

function guardarSesion(respuesta: AuthResponse | null) {
  setToken(respuesta?.token ?? null)
  try {
    if (respuesta) {
      const sesion: SesionGuardada = { usuario: respuesta.usuario, expiraEn: respuesta.expiraEn }
      localStorage.setItem(CLAVE_SESION, JSON.stringify(sesion))
    } else {
      localStorage.removeItem(CLAVE_SESION)
    }
  } catch {
    // Sin localStorage la sesión vive solo en memoria.
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [usuario, setUsuario] = useState<Usuario | null>(() => {
    const recuperado = leerSesion()
    if (!recuperado) guardarSesion(null)
    return recuperado
  })

  const logout = useCallback(() => {
    guardarSesion(null)
    setUsuario(null)
    // Borra los datos en caché para que el próximo usuario no vea los del anterior.
    queryClient.clear()
  }, [queryClient])

  useEffect(() => onSesionExpirada(logout), [logout])

  const iniciar = useCallback((respuesta: AuthResponse) => {
    guardarSesion(respuesta)
    setUsuario(respuesta.usuario)
  }, [])

  const valor = useMemo(
    () => ({
      usuario,
      logout,
      login: async (email: string, password: string) => iniciar(await authApi.login(email, password)),
      registro: async (nombre: string, email: string, password: string) =>
        iniciar(await authApi.registro(nombre, email, password)),
    }),
    [usuario, logout, iniciar],
  )

  return <AuthContext.Provider value={valor}>{children}</AuthContext.Provider>
}
