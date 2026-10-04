import { createContext, useContext } from 'react'
import type { Usuario } from '../api/tipos'

export interface AuthContexto {
  usuario: Usuario | null
  login: (email: string, password: string) => Promise<void>
  registro: (nombre: string, email: string, password: string) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthContexto | null>(null)

export function useAuth() {
  const contexto = useContext(AuthContext)
  if (!contexto) throw new Error('useAuth debe usarse dentro de <AuthProvider>')
  return contexto
}
