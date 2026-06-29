import { create } from 'zustand';

import { ACTIVE_POLICY_TRIGGER_PLATE } from '@/shared/config';
import { formatDate } from '@/shared/lib';

import { demoVehicle, vehicleTitle } from '@/entities/vehicle';
import {
  demoPolicyholder,
  demoDocumentNumber,
  demoDocumentType,
  emptyPolicyholder,
  policyholderFullName,
} from '@/entities/policyholder';
import { findCoverage } from '@/entities/coverage';
import { generatePolicyNumber } from '@/entities/policy';

import type { SessionState, SessionStore } from './types';

/** Initial state for a brand-new session (also used by `reset`). */
const initialState: SessionState = {
  step: 'plate',

  plate: '',
  termsAccepted: false,
  renewalAcknowledged: false,
  isRenewModalOpen: false,

  documentType: demoDocumentType,
  documentNumber: '',

  contactFound: false,
  policyholder: emptyPolicyholder,

  selectedCoverageId: null,

  emission: 'idle',
  issuedPolicy: null,
};

/**
 * Zustand store holding the whole purchase session.
 *
 * Components subscribe with selectors (see `selectors.ts` and the `useSession`
 * hooks) so they only re-render when the slice they read changes.
 *
 * Side effects with timers (payment processing, emission delay) are NOT run
 * here — the store only flips status flags. The owning feature component drives
 * the timing via effects, which keeps the store pure and easy to test.
 */
export const useSessionStore = create<SessionStore>((set, get) => ({
  ...initialState,

  goTo: (step) => set({ step }),

  // ---- Step 1 ----------------------------------------------------------
  setPlate: (plate) => set({ plate }),
  toggleTerms: () => set((s) => ({ termsAccepted: !s.termsAccepted })),

  submitPlate: () => {
    const { plate, renewalAcknowledged } = get();
    const compact = plate.replace(/\s/g, '').toUpperCase();
    // Demo: this plate simulates a vehicle that already has active coverage.
    if (compact === ACTIVE_POLICY_TRIGGER_PLATE && !renewalAcknowledged) {
      set({ isRenewModalOpen: true });
      return;
    }
    set({ step: 'document' });
  },

  confirmRenewal: () =>
    set({ renewalAcknowledged: true, isRenewModalOpen: false, step: 'document' }),
  cancelRenewal: () => set({ isRenewModalOpen: false }),

  // ---- Step 2 ----------------------------------------------------------
  setDocumentType: (documentType) => set({ documentType }),
  setDocumentNumber: (documentNumber) => set({ documentNumber }),

  submitDocument: (withContact) => {
    if (withContact) {
      set({
        contactFound: true,
        policyholder: { ...demoPolicyholder },
        // If the user did not type a document, fall back to the demo one so the
        // summary still shows a coherent value.
        documentNumber: get().documentNumber || demoDocumentNumber,
        step: 'personal',
      });
    } else {
      set({
        contactFound: false,
        policyholder: { ...emptyPolicyholder },
        step: 'personal',
      });
    }
  },

  // ---- Step 3 ----------------------------------------------------------
  setPolicyholderField: (field, value) =>
    set((s) => ({ policyholder: { ...s.policyholder, [field]: value } })),
  submitPersonalData: () => set({ step: 'checkout' }),

  // ---- Step 4 ----------------------------------------------------------
  selectCoverage: (selectedCoverageId) => set({ selectedCoverageId }),

  // ---- Step 5 ----------------------------------------------------------
  startEmission: () => set({ step: 'result', emission: 'emitting', issuedPolicy: null }),

  finishEmission: (result) => {
    if (result === 'error') {
      set({ emission: 'error', issuedPolicy: null });
      return;
    }
    const { policyholder, selectedCoverageId } = get();
    const coverage = findCoverage(selectedCoverageId);
    const from = new Date();
    const until = new Date();
    until.setFullYear(until.getFullYear() + 1);

    set({
      emission: 'success',
      issuedPolicy: {
        number: generatePolicyNumber(),
        vehicleTitle: vehicleTitle(demoVehicle),
        holderName: policyholderFullName(policyholder) || policyholderFullName(demoPolicyholder),
        validFrom: formatDate(from),
        validUntil: formatDate(until),
        pricePaid: coverage?.pricePerYear ?? 0,
      },
    });
  },

  reset: () => set({ ...initialState }),
}));
