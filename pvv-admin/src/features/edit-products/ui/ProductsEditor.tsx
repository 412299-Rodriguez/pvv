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

interface IndexedRule {
  rule: PricingRule
  /** Position in the saved array — grouping is a view, the list stays flat. */
  index: number
}

/** Compact input for a table cell: the column header is already the label. */
const cellInput =
  'w-full rounded border border-stone-300 bg-white px-2 py-1.5 text-sm text-stone-900 outline-none transition focus:border-stone-900 focus:ring-2 focus:ring-stone-900/10'

/**
 * The prices of one product, one row per vehicle type.
 *
 * The flat list repeated the product dropdown and all five labels on every
 * single rule, so four vehicle types across two products meant eight boxed
 * cards saying mostly the same words. Grouping drops the product from each row
 * — the heading says it — and the column headers carry the labels once.
 */
function PricingGroup({
  title,
  entries,
  missingCount,
  onAdd,
  onFill,
  onUpdate,
  onRemove,
}: {
  title: string
  entries: IndexedRule[]
  missingCount: number
  onAdd?: () => void
  onFill?: () => void
  onUpdate: (index: number, changes: Partial<PricingRule>) => void
  onRemove: (index: number) => void
}) {
  return (
    <div className="rounded-lg border border-stone-200">
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-stone-200 bg-stone-50/60 px-4 py-2.5">
        <span className="text-sm font-semibold text-stone-900">{title}</span>
        <div className="flex items-center gap-2">
          {onFill && missingCount > 0 ? (
            <button
              type="button"
              onClick={onFill}
              className="text-xs font-medium text-stone-500 underline-offset-4 transition hover:text-stone-900 hover:underline"
            >
              Completar los {missingCount} tipos que faltan
            </button>
          ) : null}
          {onAdd ? (
            <button
              type="button"
              onClick={onAdd}
              className="rounded border border-stone-300 bg-white px-2.5 py-1 text-xs font-medium text-stone-700 transition hover:border-stone-400"
            >
              + Precio
            </button>
          ) : null}
        </div>
      </div>

      {entries.length === 0 ? (
        <p className="px-4 py-5 text-sm text-stone-400">Este producto todavía no tiene precios.</p>
      ) : (
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-xs font-medium text-stone-500">
              <th className="w-44 py-2 pl-4 pr-2">Vehículo</th>
              <th className="w-28 py-2 pr-2">Año desde</th>
              <th className="w-28 py-2 pr-2">Año hasta</th>
              <th className="py-2 pr-2">Precio</th>
              <th className="w-10 py-2 pr-3" />
            </tr>
          </thead>
          <tbody>
            {entries.map(({ rule, index }) => (
              <tr key={rule.PricingId} className="border-t border-stone-100">
                <td className="py-1.5 pl-4 pr-2">
                  <select
                    value={rule.VehicleType}
                    onChange={(e) => onUpdate(index, { VehicleType: e.target.value })}
                    className={cellInput}
                  >
                    {VEHICLE_TYPES.map((v) => (
                      <option key={v} value={v}>
                        {VEHICLE_TYPE_LABELS[v] ?? v}
                      </option>
                    ))}
                  </select>
                </td>
                <td className="py-1.5 pr-2">
                  <input
                    type="number"
                    value={rule.YearFrom}
                    onChange={(e) => onUpdate(index, { YearFrom: Number(e.target.value) })}
                    className={`${cellInput} tabular-nums`}
                  />
                </td>
                <td className="py-1.5 pr-2">
                  <input
                    type="number"
                    value={rule.YearTo}
                    onChange={(e) => onUpdate(index, { YearTo: Number(e.target.value) })}
                    className={`${cellInput} tabular-nums`}
                  />
                </td>
                <td className="py-1.5 pr-2">
                  <div className="relative">
                    <span className="pointer-events-none absolute left-2 top-1/2 -translate-y-1/2 text-sm text-stone-400">
                      $
                    </span>
                    <input
                      type="number"
                      value={rule.Price}
                      onChange={(e) => onUpdate(index, { Price: Number(e.target.value) })}
                      className={`${cellInput} pl-5 tabular-nums`}
                    />
                  </div>
                </td>
                <td className="py-1.5 pr-3 text-right">
                  <button
                    type="button"
                    onClick={() => onRemove(index)}
                    title="Quitar este precio"
                    aria-label="Quitar este precio"
                    className="rounded px-1.5 py-1 text-stone-400 transition hover:bg-red-50 hover:text-red-600"
                  >
                    ✕
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}

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
  const removeRule = (i: number) => setRules((rs) => rs.filter((_, idx) => idx !== i))

  /**
   * Rules carry their position in the saved array so the grouped view can edit
   * them in place — grouping is a way of showing the list, not of storing it.
   */
  const indexedRules = rules.map((rule, index) => ({ rule, index }))
  const orphanRules = indexedRules.filter(
    (e) => !products.some((p) => p.ProductId === e.rule.ProductId),
  )

  /** A new price starts from the last one of its product: usually a small edit. */
  const newRule = (productId: string, vehicleType: string): PricingRule => {
    const previous = [...rules].reverse().find((r) => r.ProductId === productId)
    return {
      PricingId: crypto.randomUUID(),
      ProductId: productId,
      VehicleType: vehicleType,
      YearFrom: previous?.YearFrom ?? 2010,
      YearTo: previous?.YearTo ?? new Date().getFullYear(),
      Price: previous?.Price ?? 0,
    }
  }

  const addRule = (productId: string, vehicleType: string) =>
    setRules((rs) => [...rs, newRule(productId, vehicleType)])

  /** Covering the remaining vehicle types one dropdown at a time is the tedious part. */
  const fillMissing = (productId: string, missing: readonly string[]) =>
    setRules((rs) => [...rs, ...missing.map((v) => newRule(productId, v))])

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
    return <p className="text-sm text-stone-500">Cargando productos…</p>
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold tracking-tight text-stone-900">Productos y precios</h1>
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
          <h2 className="font-bold text-stone-800">Productos</h2>
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
        {products.length === 0 && <p className="text-sm text-stone-400">Sin productos.</p>}
        <div className="space-y-3">
          {products.map((p, i) => (
            <div key={p.ProductId} className="grid gap-3 rounded-lg border border-stone-200 p-4 sm:grid-cols-2">
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
              <label className="flex items-center gap-2 text-sm font-medium text-stone-700">
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

      {/* Pricing rules, grouped by product */}
      <Card>
        <div className="mb-4">
          <h2 className="font-semibold text-stone-900">Reglas de precio</h2>
          <p className="mt-0.5 text-sm text-stone-500">
            Cuánto sale cada producto según el tipo de vehículo y su año.
          </p>
        </div>

        {products.length === 0 ? (
          <p className="text-sm text-stone-400">
            Agregá un producto primero para poder ponerle precio.
          </p>
        ) : null}

        <div className="space-y-5">
          {products.map((product) => {
            const entries = indexedRules.filter((e) => e.rule.ProductId === product.ProductId)
            const missing = VEHICLE_TYPES.filter(
              (v) => !entries.some((e) => e.rule.VehicleType === v),
            )

            return (
              <PricingGroup
                key={product.ProductId}
                title={product.Name || '(sin nombre)'}
                entries={entries}
                missingCount={missing.length}
                onAdd={() => addRule(product.ProductId, missing[0] ?? 'Car')}
                onFill={() => fillMissing(product.ProductId, missing)}
                onUpdate={updateRule}
                onRemove={removeRule}
              />
            )
          })}

          {/* A rule whose product no longer exists would otherwise disappear from
              the screen while still being saved. Show it so it can be dealt with. */}
          {orphanRules.length > 0 ? (
            <PricingGroup
              title="Sin producto asociado"
              entries={orphanRules}
              missingCount={0}
              onUpdate={updateRule}
              onRemove={removeRule}
            />
          ) : null}
        </div>
      </Card>
    </div>
  )
}
