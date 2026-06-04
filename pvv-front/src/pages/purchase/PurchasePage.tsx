export function PurchasePage() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-6 bg-slate-50">
      <h1 className="text-3xl font-bold text-slate-800">PVV — Portal de Ventas</h1>
      <button
        type="button"
        className="rounded-lg bg-blue-600 px-6 py-3 font-medium text-white transition-colors hover:bg-blue-700"
      >
        Iniciar compra
      </button>
    </main>
  )
}
