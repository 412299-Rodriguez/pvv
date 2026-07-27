import { useSessionStore } from '@/entities/session'
import { ProductsEditor } from '@/features/edit-products'

export function ProductsPage() {
  const companyId = useSessionStore((s) => s.companyId)

  if (!companyId) {
    return <p className="text-sm text-stone-500">Tu usuario no tiene una compañía asociada.</p>
  }

  return <ProductsEditor companyId={companyId} />
}
