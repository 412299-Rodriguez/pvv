import type { ReactNode } from 'react'

interface FieldProps {
  label: string
  hint?: string
  children: ReactNode
}

export function Field({ label, hint, children }: FieldProps) {
  return (
    <label className="block">
      <span className="mb-1.5 block text-xs font-medium text-stone-600">{label}</span>
      {children}
      {hint && <span className="mt-1 block text-xs text-stone-400">{hint}</span>}
    </label>
  )
}

/** Focus is ink too, so the form agrees with the rest of the panel. */
export const inputClass =
  'w-full rounded-md border border-stone-300 bg-white px-3 py-2 text-sm text-stone-900 outline-none transition placeholder:text-stone-400 focus:border-stone-900 focus:ring-2 focus:ring-stone-900/10'
