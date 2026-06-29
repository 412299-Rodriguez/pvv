/**
 * Locale-aware formatting helpers (Argentina / es-AR).
 */

/** `45000` -> `"$45.000"` */
export function formatCurrency(value: number): string {
  return `$${value.toLocaleString('es-AR')}`;
}

/** `"32456789"` -> `"32.456.789"` (thousands separator only, digits in). */
export function formatDocument(digits: string): string {
  return digits.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
}

/** `Date` -> `"dd/mm/yyyy"` */
export function formatDate(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()}`;
}
