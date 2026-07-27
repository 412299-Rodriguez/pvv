import { RANGE_PRESETS, type RangePreset } from '@/shared/lib'

/**
 * One filter row above everything it scopes — never per-card filters, so every
 * number on the page always describes the same slice of time.
 */
export function DateRangeFilter({
  value,
  onChange,
  children,
}: {
  value: RangePreset
  onChange: (preset: RangePreset) => void
  children?: React.ReactNode
}) {
  return (
    <div className="mb-6 flex flex-wrap items-center gap-2">
      <div className="flex rounded-lg border border-slate-300 bg-white p-1">
        {RANGE_PRESETS.map((preset) => (
          <button
            key={preset.value}
            type="button"
            onClick={() => onChange(preset.value)}
            className={`rounded-md px-3 py-1.5 text-sm font-semibold transition ${
              value === preset.value
                ? 'bg-blue-50 text-blue-700'
                : 'text-slate-600 hover:bg-slate-100'
            }`}
          >
            {preset.label}
          </button>
        ))}
      </div>
      {children}
    </div>
  )
}
