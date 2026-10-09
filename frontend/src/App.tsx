import { Navigate, Outlet, Route, Routes } from 'react-router'
import { useAuth } from './auth/contexto'
import { Layout } from './components/Layout'
import { Acceso } from './pages/Acceso'
import { Categorias } from './pages/Categorias'
import { Cuentas } from './pages/Cuentas'
import { Movimientos } from './pages/Movimientos'
import { Panel } from './pages/Panel'

/** Rutas que piden sesión: sin usuario, al login. */
function ConSesion() {
  const { usuario } = useAuth()
  return usuario ? <Outlet /> : <Navigate to="/login" replace />
}

/** Login y registro: con sesión iniciada, al panel. */
function SinSesion() {
  const { usuario } = useAuth()
  return usuario ? <Navigate to="/" replace /> : <Outlet />
}

export default function App() {
  return (
    <Routes>
      <Route element={<SinSesion />}>
        <Route path="/login" element={<Acceso modo="login" />} />
        <Route path="/registro" element={<Acceso modo="registro" />} />
      </Route>
      <Route element={<ConSesion />}>
        <Route element={<Layout />}>
          <Route index element={<Panel />} />
          <Route path="/movimientos" element={<Movimientos />} />
          <Route path="/cuentas" element={<Cuentas />} />
          <Route path="/categorias" element={<Categorias />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
