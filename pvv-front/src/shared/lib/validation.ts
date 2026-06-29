/**
 * Pure validation helpers used by the wizard forms.
 * Each returns a boolean and never throws.
 */

/** Uppercase and strip anything that is not a letter, digit or space. */
export function normalizePlate(value: string): string {
  return value.toUpperCase().replace(/[^A-Z0-9 ]/g, '');
}

/** Argentine plates have 6 (old) or 7 (Mercosur) alphanumeric characters. */
export function isPlateValid(value: string): boolean {
  const compact = value.replace(/\s/g, '');
  return compact.length >= 6 && compact.length <= 7;
}

/** Keep only digits — handy for document / phone inputs. */
export function digitsOnly(value: string): string {
  return value.replace(/\D/g, '');
}

export function isNonEmptyName(value: string): boolean {
  return value.trim().length >= 2;
}

export function isValidEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim());
}

export function isValidPhone(value: string): boolean {
  return digitsOnly(value).length >= 8;
}
