import { Link } from 'react-router-dom'

export function ProductsPage() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-6 bg-slate-100">
      <h1 className="text-2xl font-semibold text-slate-800">Productos</h1>
      <Link className="text-blue-600 hover:underline" to="/">
        ← Volver al dashboard
      </Link>
    </main>
  )
}
