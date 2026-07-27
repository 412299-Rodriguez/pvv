/** The time windows the dashboard and the leads table can be scoped to. */
export type RangePreset = '7d' | '30d' | '90d' | 'all'

export const RANGE_PRESETS: { value: RangePreset; label: string }[] = [
  { value: '7d', label: 'Últimos 7 días' },
  { value: '30d', label: 'Últimos 30 días' },
  { value: '90d', label: 'Últimos 90 días' },
  { value: 'all', label: 'Todo' },
]

const DAYS: Record<Exclude<RangePreset, 'all'>, number> = { '7d': 7, '30d': 30, '90d': 90 }

/**
 * Turns a preset into the `from` filter the API expects. "Todo" sends nothing,
 * which the backend reads as "since the beginning".
 */
export function presetToFilter(preset: RangePreset): { from?: string } {
  if (preset === 'all') return {}

  const from = new Date()
  from.setDate(from.getDate() - DAYS[preset])
  from.setHours(0, 0, 0, 0)
  return { from: from.toISOString() }
}
