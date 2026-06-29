import { useEffect, useState } from 'react'

import {
  CONFIG_TYPE,
  VEHICLE_TYPES,
  VEHICLE_TYPE_LABELS,
  emptyPricingConfig,
  emptyProductConfig,
  getConfig,
  putConfig,
  type PricingConfig,
  type PricingRule,
  type ProductConfig,
  type ProductItem,
} from '@/entities/config'
import { Button, Card, Field, inputClass } from '@/shared/ui'

type SaveStatus = 'idle' | 'saving' | 'saved' | 'error'

/** Edits a company's PRODUCT_CONFIG + PRICING_CONFIG. Reusable: pass the companyId. */
export function ProductsEditor({ companyId }: { companyId: string }) {
  const [products, setProducts] = useState<ProductItem[]>([])
  const [rules, setRules] = useState<PricingRule[]>([])
  const [loading, setLoading] = useState(true)
  const [status, setStatus] = useState<SaveStatus>('idle')

  useEffect(() => {
    let active = true
    // `loading` already starts true; remounting (via key) resets it on company change.
    Promise.all([
      getConfig<ProductConfig>(companyId, CONFIG_TYPE.product, emptyProductConfig),
      getConfig<PricingConfig>(companyId, CONFIG_TYPE.pricing, emptyPricingConfig),
    ])
      .then(([p, pr]) => {
        if (!active) return
        setProducts(p.Products ?? [])
        setRules(pr.Rules ?? [])
        setLoading(false)
      })
      .catch(() => active && setLoading(false))
    return () => {
      active = false
    }
  }, [companyId])

  const updateProduct = (i: number, changes: Partial<ProductItem>) =>
    setProducts((ps) => ps.map((p, idx) => (idx === i ? { ...p, ...changes } : p)))
  const updateRule = (i: number, changes: Partial<PricingRule>) =>
    setRules((rs) => rs.map((r, idx) => (idx === i ? { ...r, ...changes } : r)))

  const save = async () => {
    setStatus('saving')
    try {
      await putConfig(companyId, CONFIG_TYPE.product, { Products: products })
      await putConfig(companyId, CONFIG_TYPE.pricing, { Rules: rules })
      setStatus('saved')
      window.setTimeout(() => setStatus('idle'), 2500)
    } catch {
      setStatus('error')
    }
  }

  if (loading) {
    return <p className="text-sm text-slate-500">Cargando productos…</p>
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-slate-800">Productos y precios</h1>
        <div className="flex items-center gap-3">
          {status === 'saved' && <span className="text-sm font-medium text-green-600">Guardado ✓</span>}
          {status === 'error' && <span className="text-sm font-medium text-red-600">Error al guardar</span>}
          <Button onClick={save} disabled={status === 'saving'}>
            {status === 'saving' ? 'Guardando…' : 'Guardar cambios'}
          </Button>
        </div>
      </div>

      {/* Products */}
      <Card>
        <div className="mb-3 flex items-center justify-between">
          <h2 className="font-bold text-slate-800">Productos</h2>
          <Button
            variant="secondary"
            onClick={() =>
              setProducts((ps) => [
                ...ps,
                {
                  ProductId: crypto.randomUUID(),
                  Name: '',
                  CoverageType: '',
                  Conditions: '',
                  IsActive: true,
                },
              ])
            }
          >
            + Agregar producto
          </Button>
        </div>
        {products.length === 0 && <p className="text-sm text-slate-400">Sin productos.</p>}
        <div className="space-y-3">
          {products.map((p, i) => (
            <div key={p.ProductId} className="grid gap-3 rounded-lg border border-slate-200 p-4 sm:grid-cols-2">
              <Field label="Nombre">
                <input value={p.Name} onChange={(e) => updateProduct(i, { Name: e.target.value })} className={inputClass} />
              </Field>
              <Field label="Tipo de cobertura">
                <input
                  value={p.CoverageType}
                  onChange={(e) => updateProduct(i, { CoverageType: e.target.value })}
                  className={inputClass}
                />
              </Field>
              <div className="sm:col-span-2">
                <Field label="Condiciones / beneficios">
                  <input
                    value={p.Conditions}
                    onChange={(e) => updateProduct(i, { Conditions: e.target.value })}
                    className={inputClass}
                  />
                </Field>
              </div>
              <label className="flex items-center gap-2 text-sm font-medium text-slate-700">
                <input
                  type="checkbox"
                  checked={p.IsActive}
                  onChange={(e) => updateProduct(i, { IsActive: e.target.checked })}
                />
                Activo
              </label>
              <div className="flex justify-end">
                <Button variant="danger" onClick={() => setProducts((ps) => ps.filter((_, idx) => idx !== i))}>
                  Quitar
                </Button>
              </div>
            </div>
          ))}
        </div>
      </Card>

      {/* Pricing rules */}
      <Card>
        <div className="mb-3 flex items-center justify-between">
          <h2 className="font-bold text-slate-800">Reglas de precio</h2>
          <Button
            variant="secondary"
            disabled={products.length === 0}
            onClick={() =>
              setRules((rs) => [
                ...rs,
                {
                  PricingId: crypto.randomUUID(),
                  ProductId: products[0]?.ProductId ?? '',
                  VehicleType: 'Car',
                  YearFrom: 2010,
                  YearTo: new Date().getFullYear(),
                  Price: 0,
                },
              ])
            }
          >
            + Agregar regla
          </Button>
        </div>
        {products.length === 0 && (
          <p className="text-sm text-slate-400">Agregá un producto primero para poder ponerle precio.</p>
        )}
        <div className="space-y-3">
          {rules.map((r, i) => (
            <div key={r.PricingId} className="grid items-end gap-3 rounded-lg border border-slate-200 p-4 sm:grid-cols-5">
              <Field label="Producto">
                <select
                  value={r.ProductId}
                  onChange={(e) => updateRule(i, { ProductId: e.target.value })}
                  className={inputClass}
                >
                  {products.map((p) => (
                    <option key={p.ProductId} value={p.ProductId}>
                      {p.Name || '(sin nombre)'}
                    </option>
                  ))}
                </select>
              </Field>
              <Field label="Vehículo">
                <select
                  value={r.VehicleType}
                  onChange={(e) => updateRule(i, { VehicleType: e.target.value })}
                  className={inputClass}
                >
                  {VEHICLE_TYPES.map((v) => (
                    <option key={v} value={v}>
                      {VEHICLE_TYPE_LABELS[v] ?? v}
                    </option>
                  ))}
                </select>
              </Field>
              <Field label="Año desde">
                <input
                  type="number"
                  value={r.YearFrom}
                  onChange={(e) => updateRule(i, { YearFrom: Number(e.target.value) })}
                  className={inputClass}
                />
              </Field>
              <Field label="Año hasta">
                <input
                  type="number"
                  value={r.YearTo}
                  onChange={(e) => updateRule(i, { YearTo: Number(e.target.value) })}
                  className={inputClass}
                />
              </Field>
              <div className="flex items-end gap-2">
                <Field label="Precio">
                  <input
                    type="number"
                    value={r.Price}
                    onChange={(e) => updateRule(i, { Price: Number(e.target.value) })}
                    className={inputClass}
                  />
                </Field>
                <Button variant="danger" onClick={() => setRules((rs) => rs.filter((_, idx) => idx !== i))}>
                  ✕
                </Button>
              </div>
            </div>
          ))}
        </div>
      </Card>
    </div>
  )
}
