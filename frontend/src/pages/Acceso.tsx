import { useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { ApiError } from '../api/cliente'
import { useAuth } from '../auth/contexto'
import { Logo } from '../components/Layout'
import { Alerta, Boton, Campo } from '../components/ui'

/** Pantalla de inicio de sesión o de registro, según el modo. */
export function Acceso({ modo }: { modo: 'login' | 'registro' }) {
  const { login, registro } = useAuth()
  const esRegistro = modo === 'registro'

  const [nombre, setNombre] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<ApiError | null>(null)
  const [cargando, setCargando] = useState(false)

  const enviar = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)
    setCargando(true)
    try {
      if (esRegistro) await registro(nombre, email, password)
      else await login(email, password)
      // Al guardarse la sesión, el router redirige solo al panel.
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(0, 'Ocurrió un error inesperado.'))
      setCargando(false)
    }
  }

  const errorGeneral = error && Object.keys(error.errores).length === 0 ? error.message : null

  return (
    <div className="flex min-h-dvh flex-col items-center justify-center px-4 py-10">
      <div className="mb-6">
        <Logo />
      </div>
      <div className="w-full max-w-sm rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
        <h1 className="text-xl font-semibold text-slate-900">{esRegistro ? 'Crea tu cuenta' : 'Inicia sesión'}</h1>
        <p className="mt-1 text-sm text-slate-500">
          {esRegistro ? 'Empieza a ordenar tus ingresos y gastos.' : 'Qué bueno verte de nuevo.'}
        </p>

        <form onSubmit={enviar} className="mt-6 space-y-4" noValidate>
          {esRegistro && (
            <Campo
              etiqueta="Nombre"
              autoComplete="name"
              value={nombre}
              onChange={(e) => setNombre(e.target.value)}
              error={error?.campo('nombre')}
              autoFocus
              required
            />
          )}
          <Campo
            etiqueta="Email"
            type="email"
            autoComplete="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            error={error?.campo('email')}
            autoFocus={!esRegistro}
            required
          />
          <Campo
            etiqueta="Contraseña"
            type="password"
            autoComplete={esRegistro ? 'new-password' : 'current-password'}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            error={error?.campo('password')}
            ayuda={esRegistro ? 'Al menos 8 caracteres y un número.' : undefined}
            required
          />
          <Alerta>{errorGeneral}</Alerta>
          <Boton type="submit" cargando={cargando} className="w-full">
            {esRegistro ? 'Crear cuenta' : 'Entrar'}
          </Boton>
        </form>
      </div>

      <p className="mt-6 text-sm text-slate-600">
        {esRegistro ? '¿Ya tienes cuenta? ' : '¿No tienes cuenta? '}
        <Link to={esRegistro ? '/login' : '/registro'} className="font-medium text-emerald-700 hover:underline">
          {esRegistro ? 'Inicia sesión' : 'Regístrate'}
        </Link>
      </p>
    </div>
  )
}
