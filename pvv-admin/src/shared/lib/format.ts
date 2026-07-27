/** Date + time as an Argentinian operator reads it: 27/07/2026 14:35. */
export function formatDateTime(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleString('es-AR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

/** Shows a dash rather than an empty cell, so a column never looks broken. */
export function orDash(value: string | null | undefined): string {
  return value && value.trim().length > 0 ? value : '—'
}
