/**
 * Data-visualisation tokens.
 *
 * The funnel stages are an ORDERED scale, so they use an ordinal ramp: one hue
 * (the app's blue), stepping light to dark as the buyer goes deeper. Bar length
 * carries the magnitude — the ramp only says which stage you are looking at.
 *
 * Validated against the white card surface: single hue (3° spread), monotone
 * lightness, every adjacent step separated, and the lightest step still clears
 * 2:1 contrast (2.11:1).
 */
export const FUNNEL_RAMP = ['#86b6ef', '#5598e7', '#2a78d6', '#1c5cab', '#104281'] as const

/** Chart chrome — recessive by design; the data is the only loud thing. */
export const VIZ = {
  /** Unfilled part of a bar: one step off the surface, never a competing hue. */
  track: '#f1f5f9',
  /** 2px of surface separating touching marks. */
  gap: '#ffffff',
} as const

/** Formats 0-100 as a percentage the way the dashboard shows it. */
export function formatPercent(value: number): string {
  return `${value.toLocaleString('es-AR', { maximumFractionDigits: 1 })}%`
}

/** Thousands-separated integer. */
export function formatCount(value: number): string {
  return value.toLocaleString('es-AR')
}

/** Money as pesos, no decimals (all demo prices are round). */
export function formatMoney(value: number): string {
  return value.toLocaleString('es-AR', {
    style: 'currency',
    currency: 'ARS',
    maximumFractionDigits: 0,
  })
}
