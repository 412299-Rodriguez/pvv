import type { ComponentType, SVGProps } from 'react';

import { CarIcon, ShieldCheckIcon, CreditCardIcon } from '@/shared/ui/icons';
import type { WizardStep } from '@/entities/session';

/** A single visual stage in the top stepper. */
export interface StepperStage {
  key: string;
  label: string;
  icon: ComponentType<SVGProps<SVGSVGElement>>;
  /** Step to navigate to when this (completed) stage is clicked. */
  target: WizardStep;
}

/**
 * The three visual stages. Note these do NOT map 1:1 to wizard steps:
 *  - "Cotizá" spans the plate AND document screens
 *  - "Completá tus datos" is the personal-data screen
 *  - "Pagá" is the checkout screen
 */
export const STAGES: readonly StepperStage[] = [
  { key: 'quote', label: 'Cotizá', icon: CarIcon, target: 'plate' },
  { key: 'data', label: 'Completá tus datos', icon: ShieldCheckIcon, target: 'personal' },
  { key: 'pay', label: 'Pagá', icon: CreditCardIcon, target: 'checkout' },
];

/** Computed view-model for rendering the stepper for a given wizard step. */
export interface StepperView {
  /** Index of the active stage, or -1 when nothing is active (result screen). */
  activeIndex: number;
  /** Indices of completed stages (rendered green with a check). */
  doneIndices: number[];
  /** Progress of the connector line, 0–100. */
  progress: number;
}

/** Map the current wizard step to the stepper view-model. */
export function getStepperView(step: WizardStep): StepperView {
  switch (step) {
    case 'plate':
    case 'document':
      return { activeIndex: 0, doneIndices: [], progress: 6 };
    case 'personal':
      return { activeIndex: 1, doneIndices: [0], progress: 50 };
    case 'checkout':
      return { activeIndex: 2, doneIndices: [0, 1], progress: 100 };
    case 'result':
      return { activeIndex: -1, doneIndices: [0, 1, 2], progress: 100 };
  }
}
