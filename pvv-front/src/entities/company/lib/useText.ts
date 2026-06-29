import { usePvvConfigStore } from '../model/pvvConfigStore';

/**
 * Returns the company-customized copy for `key`, falling back to `fallback` when
 * the company hasn't overridden it. Lets every screen be fully customizable
 * while still rendering sensible defaults out of the box.
 */
export function useText(key: string, fallback: string): string {
  return usePvvConfigStore((state) => state.texts[key] || fallback);
}
