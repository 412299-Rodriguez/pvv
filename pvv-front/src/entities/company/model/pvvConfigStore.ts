import { create } from 'zustand';

import { ingressRequest } from '@/shared/api';

/** Bootstrap status of the tenant: still loading, valid, or not a real portal. */
export type CompanyStatus = 'loading' | 'ready' | 'invalid';

/** Backend UI config blob (PascalCase), served by pvv-config as PVV_UI_CONFIG. */
interface UiConfigDto {
  PrimaryColor?: string;
  SecondaryColor?: string;
  LogoUrl?: string;
  CompanyDisplayName?: string;
  Texts?: Record<string, string>;
  AdImages?: string[];
}

interface PvvConfigState {
  status: CompanyStatus;
  companyName: string;
  logoUrl: string | null;
  /** Per-screen copy overrides, keyed by id (see useText). */
  texts: Record<string, string>;
  /** Public image URLs for the step-1 ad carousel. */
  adImages: string[];
  loadAndApply: () => Promise<void>;
  markInvalid: () => void;
}

function applyAppearance(dto: UiConfigDto): void {
  const root = document.documentElement;
  if (dto.PrimaryColor) {
    root.style.setProperty('--color-primary', dto.PrimaryColor);
    root.style.setProperty('--color-primary-hover', dto.PrimaryColor);
  }
  if (dto.SecondaryColor) {
    root.style.setProperty('--color-secondary', dto.SecondaryColor);
  }
  if (dto.CompanyDisplayName) {
    document.title = dto.CompanyDisplayName;
  }
}

export const usePvvConfigStore = create<PvvConfigState>((set) => ({
  status: 'loading',
  companyName: '',
  logoUrl: null,
  texts: {},
  adImages: [],

  markInvalid: () => set({ status: 'invalid' }),

  loadAndApply: async () => {
    try {
      const dto = await ingressRequest<UiConfigDto>('CONFIG_LOAD', { type: 'PVV_UI_CONFIG' });
      applyAppearance(dto);
      set({
        status: 'ready',
        companyName: dto.CompanyDisplayName ?? '',
        logoUrl: dto.LogoUrl ? dto.LogoUrl : null,
        texts: dto.Texts ?? {},
        adImages: dto.AdImages ?? [],
      });
    } catch {
      // No valid company config → not a real point of sale.
      set({ status: 'invalid' });
    }
  },
}));
