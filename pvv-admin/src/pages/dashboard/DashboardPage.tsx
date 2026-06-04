import { Link } from 'react-router-dom'

export function DashboardPage() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-8 bg-slate-100">
      <h1 className="text-3xl font-bold text-slate-800">PVV Admin</h1>
      <nav className="flex gap-6 text-blue-600">
        <Link className="hover:underline" to="/companies">
          Compañías
        </Link>
        <Link className="hover:underline" to="/products">
          Productos
        </Link>
        <Link className="hover:underline" to="/">
          Dashboard
        </Link>
      </nav>
    </main>
  )
}
