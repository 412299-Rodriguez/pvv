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
    <div className="mb-5 flex flex-wrap items-center gap-2">
      <div className="inline-flex rounded-md border border-stone-300 bg-white p-0.5">
        {RANGE_PRESETS.map((preset) => (
          <button
            key={preset.value}
            type="button"
            onClick={() => onChange(preset.value)}
            className={`rounded px-3 py-1.5 text-sm transition ${
              value === preset.value
                ? 'bg-stone-900 font-medium text-white'
                : 'text-stone-500 hover:text-stone-900'
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
