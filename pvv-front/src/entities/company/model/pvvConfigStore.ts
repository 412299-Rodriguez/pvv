import { create } from 'zustand';

/**
 * Dynamic appearance / theming config — RESERVED for HU-10.
 *
 * Each company customises its portal (colors, logo, texts) from pvv-admin; that
 * config is stored in pvv-config and served through the BFF. HU-10 fetches it at
 * app start and applies it by injecting CSS custom properties on :root — the
 * base CSS-var theme already lives in `app/styles/tokens.css`, so theming is an
 * override, not a rewrite. Not wired yet.
 */
export interface AppearanceConfig {
  /** Overrides `--color-primary`; null = use the token default. */
  primaryColor: string | null;
  logoUrl: string | null;
  /** Per-screen copy overrides, keyed by a stable text id. */
  texts: Record<string, string>;
}

interface PvvConfigState extends AppearanceConfig {
  applyAppearance: (config: AppearanceConfig) => void;
}

export const usePvvConfigStore = create<PvvConfigState>((set) => ({
  primaryColor: null,
  logoUrl: null,
  texts: {},
  // TODO HU-10: besides storing it, inject the CSS vars on document.documentElement.
  applyAppearance: (config) => set(config),
}));
