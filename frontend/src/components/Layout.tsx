import { ArrowLeftRight, LayoutDashboard, LogOut, Tags, Wallet } from 'lucide-react'
import { NavLink, Outlet, useLocation } from 'react-router'
import { useAuth } from '../auth/contexto'

const enlaces = [
  { a: '/', texto: 'Panel', Icono: LayoutDashboard },
  { a: '/movimientos', texto: 'Movimientos', Icono: ArrowLeftRight },
  { a: '/cuentas', texto: 'Cuentas', Icono: Wallet },
  { a: '/categorias', texto: 'Categorías', Icono: Tags },
]

export function Logo() {
  return (
    <span className="flex items-center gap-2 font-semibold text-slate-900">
      <img src="/favicon.svg" alt="" className="size-7" />
      Mi Economía
    </span>
  )
}

export function Layout() {
  const { usuario, logout } = useAuth()
  const { search } = useLocation()
  // Mantiene el mes elegido (?mes=) al pasar del panel a movimientos y viceversa.
  const mes = new URLSearchParams(search).get('mes')
  const conMes = (ruta: string) => (mes && ruta !== '/categorias' ? `${ruta}?mes=${mes}` : ruta)

  return (
    <div className="min-h-dvh pb-20 sm:pb-0">
      <header className="sticky top-0 z-10 border-b border-slate-200 bg-white/90 backdrop-blur">
        <div className="mx-auto flex h-14 max-w-5xl items-center gap-6 px-4">
          <Logo />
          <nav className="hidden gap-1 sm:flex">
            {enlaces.map(({ a, texto, Icono }) => (
              <NavLink
                key={a}
                to={conMes(a)}
                end
                className={({ isActive }) =>
                  `flex items-center gap-2 rounded-lg px-3 py-1.5 text-sm font-medium transition ${
                    isActive ? 'bg-emerald-50 text-emerald-700' : 'text-slate-600 hover:bg-slate-100'
                  }`
                }
              >
                <Icono className="size-4" aria-hidden />
                {texto}
              </NavLink>
            ))}
          </nav>
          <div className="ml-auto flex items-center gap-2">
            <span className="hidden text-sm text-slate-600 md:inline">{usuario?.nombre}</span>
            <button
              type="button"
              onClick={logout}
              className="flex items-center gap-1.5 rounded-lg px-2 py-1.5 text-sm text-slate-500 hover:bg-slate-100 hover:text-slate-800"
            >
              <LogOut className="size-4" aria-hidden />
              Salir
            </button>
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-5xl px-4 py-6">
        <Outlet />
      </main>

      {/* En el celular la navegación va abajo, al alcance del pulgar. */}
      <nav className="fixed inset-x-0 bottom-0 z-10 flex border-t border-slate-200 bg-white sm:hidden">
        {enlaces.map(({ a, texto, Icono }) => (
          <NavLink
            key={a}
            to={conMes(a)}
            end
            className={({ isActive }) =>
              `flex flex-1 flex-col items-center gap-0.5 py-2 text-xs font-medium ${
                isActive ? 'text-emerald-700' : 'text-slate-500'
              }`
            }
          >
            <Icono className="size-5" aria-hidden />
            {texto}
          </NavLink>
        ))}
      </nav>
    </div>
  )
}
