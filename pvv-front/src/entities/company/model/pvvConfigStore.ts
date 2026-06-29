import { create } from 'zustand';

import { ingressRequest } from '@/shared/api';

/** Bootstrap status of the tenant: still loading, valid, or not a real portal. */
export type CompanyStatus = 'loading' | 'ready' | 'invalid';

export interface FaqItem {
  question: string;
  answer: string;
}

/** Backend UI config blob (PascalCase), served by pvv-config as PVV_UI_CONFIG. */
interface UiConfigDto {
  PrimaryColor?: string;
  SecondaryColor?: string;
  LogoUrl?: string;
  CompanyDisplayName?: string;
  FooterText?: string;
  Texts?: Record<string, string>;
  AdImages?: string[];
  TermsText?: string;
  PrivacyText?: string;
  Faqs?: { Question?: string; Answer?: string }[];
}

interface PvvConfigState {
  status: CompanyStatus;
  companyName: string;
  logoUrl: string | null;
  footerText: string;
  texts: Record<string, string>;
  adImages: string[];
  termsText: string;
  privacyText: string;
  faqs: FaqItem[];
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
  footerText: '',
  texts: {},
  adImages: [],
  termsText: '',
  privacyText: '',
  faqs: [],

  markInvalid: () => set({ status: 'invalid' }),

  loadAndApply: async () => {
    try {
      const dto = await ingressRequest<UiConfigDto>('CONFIG_LOAD', { type: 'PVV_UI_CONFIG' });
      applyAppearance(dto);
      set({
        status: 'ready',
        companyName: dto.CompanyDisplayName ?? '',
        logoUrl: dto.LogoUrl ? dto.LogoUrl : null,
        footerText: dto.FooterText ?? '',
        texts: dto.Texts ?? {},
        adImages: dto.AdImages ?? [],
        termsText: dto.TermsText ?? '',
        privacyText: dto.PrivacyText ?? '',
        faqs: (dto.Faqs ?? []).map((f) => ({ question: f.Question ?? '', answer: f.Answer ?? '' })),
      });
    } catch {
      // No valid company config → not a real point of sale.
      set({ status: 'invalid' });
    }
  },
}));
