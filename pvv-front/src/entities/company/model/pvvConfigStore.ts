import { create } from 'zustand';

import { ingressRequest } from '@/shared/api';

/** Bootstrap status of the tenant: still loading, valid, or not a real portal. */
export type CompanyStatus = 'loading' | 'ready' | 'invalid';

/** The appearance the portal applies for the current company. */
export interface AppearanceConfig {
  primaryColor: string | null;
  secondaryColor: string | null;
  logoUrl: string | null;
  /** Per-screen copy (welcome / footer / companyName). */
  texts: Record<string, string>;
}

/** Backend UI config blob (PascalCase), served by pvv-config as PVV_UI_CONFIG. */
interface UiConfigDto {
  PrimaryColor?: string;
  SecondaryColor?: string;
  LogoUrl?: string;
  WelcomeText?: string;
  FooterText?: string;
  CompanyDisplayName?: string;
}

interface PvvConfigState extends AppearanceConfig {
  status: CompanyStatus;
  /** Fetch the company's UI config via the ingress and apply it as the theme. */
  loadAndApply: () => Promise<void>;
  /** Mark the portal invalid (e.g. no/unknown company token). */
  markInvalid: () => void;
}

/** Inject the company colors as CSS custom properties on :root. */
function applyCssVariables(appearance: AppearanceConfig): void {
  const root = document.documentElement;
  if (appearance.primaryColor) {
    root.style.setProperty('--color-primary', appearance.primaryColor);
    root.style.setProperty('--color-primary-hover', appearance.primaryColor);
  }
  if (appearance.secondaryColor) {
    root.style.setProperty('--color-secondary', appearance.secondaryColor);
  }
}

export const usePvvConfigStore = create<PvvConfigState>((set) => ({
  primaryColor: null,
  secondaryColor: null,
  logoUrl: null,
  texts: {},
  status: 'loading',

  markInvalid: () => set({ status: 'invalid' }),

  loadAndApply: async () => {
    try {
      const dto = await ingressRequest<UiConfigDto>('CONFIG_LOAD', { type: 'PVV_UI_CONFIG' });
      const appearance: AppearanceConfig = {
        primaryColor: dto.PrimaryColor ?? null,
        secondaryColor: dto.SecondaryColor ?? null,
        logoUrl: dto.LogoUrl ? dto.LogoUrl : null,
        texts: {
          welcome: dto.WelcomeText ?? '',
          footer: dto.FooterText ?? '',
          companyName: dto.CompanyDisplayName ?? '',
        },
      };
      applyCssVariables(appearance);
      set({ ...appearance, status: 'ready' });
    } catch {
      // No valid company config → not a real point of sale.
      set({ status: 'invalid' });
    }
  },
}));
