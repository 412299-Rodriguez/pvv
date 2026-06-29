import { create } from 'zustand';

import { formatDate } from '@/shared/lib';
import { holderToPolicyholder } from '@/shared/api/ingress';

import { demoVehicle, vehicleTitle } from '@/entities/vehicle';
import {
  demoPolicyholder,
  demoDocumentType,
  emptyPolicyholder,
  policyholderFullName,
} from '@/entities/policyholder';
import { findCoverage } from '@/entities/coverage';

import type { SessionState, SessionStore } from './types';

/** Initial state for a brand-new session (also used by `reset`). */
const initialState: SessionState = {
  step: 'plate',

  plate: '',
  termsAccepted: false,
  renewalAcknowledged: false,
  isRenewModalOpen: false,
  vehicle: null,
  existingPolicy: null,

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
 * Components subscribe with selectors (see `selectors.ts`) so they only
 * re-render when the slice they read changes. The store itself performs NO
 * network or timing side effects — feature components call the
 * `shared/api/ingress` client and hand the result back through the `apply…`
 * actions, which keeps this store pure and easy to test.
 */
export const useSessionStore = create<SessionStore>((set, get) => ({
  ...initialState,

  goTo: (step) => set({ step }),

  goBack: () =>
    set((s) => {
      const order: SessionState['step'][] = ['plate', 'document', 'personal', 'checkout'];
      const index = order.indexOf(s.step);
      const previous = index > 0 ? order[index - 1] : undefined;
      return previous ? { step: previous } : {};
    }),

  // ---- Step 1 ----------------------------------------------------------
  setPlate: (plate) => set({ plate }),
  toggleTerms: () => set((s) => ({ termsAccepted: !s.termsAccepted })),

  applyPlateCheck: (result) => {
    const blockedByActivePolicy = result.hasActivePolicy && !get().renewalAcknowledged;
    set({
      vehicle: result.vehicle,
      existingPolicy: result.existingPolicy,
      isRenewModalOpen: blockedByActivePolicy,
      step: blockedByActivePolicy ? get().step : 'document',
    });
  },

  confirmRenewal: () =>
    set({ renewalAcknowledged: true, isRenewModalOpen: false, step: 'document' }),
  cancelRenewal: () => set({ isRenewModalOpen: false }),

  // ---- Step 2 ----------------------------------------------------------
  setDocumentType: (documentType) => set({ documentType }),
  setDocumentNumber: (documentNumber) => set({ documentNumber }),

  applyHolderLookup: (result) => {
    if (result.found && result.holder) {
      set({
        contactFound: true,
        policyholder: holderToPolicyholder(result.holder),
        step: 'personal',
      });
    } else {
      set({ contactFound: false, policyholder: { ...emptyPolicyholder }, step: 'personal' });
    }
  },

  skipHolderLookup: () =>
    set({ contactFound: false, policyholder: { ...emptyPolicyholder }, step: 'personal' }),

  // ---- Step 3 ----------------------------------------------------------
  setPolicyholderField: (field, value) =>
    set((s) => ({ policyholder: { ...s.policyholder, [field]: value } })),
  submitPersonalData: () => set({ step: 'checkout' }),

  // ---- Step 4 ----------------------------------------------------------
  selectCoverage: (selectedCoverageId) => set({ selectedCoverageId }),

  // ---- Step 5 ----------------------------------------------------------
  startEmission: () => set({ step: 'result', emission: 'emitting', issuedPolicy: null }),

  applyEmissionResult: (result) => {
    const { policyholder, selectedCoverageId, vehicle } = get();
    const coverage = findCoverage(selectedCoverageId);
    set({
      emission: 'success',
      issuedPolicy: {
        number: result.policyNumber,
        vehicleTitle: vehicleTitle(vehicle ?? demoVehicle),
        holderName: policyholderFullName(policyholder) || policyholderFullName(demoPolicyholder),
        validFrom: formatDate(new Date(result.validFromIso)),
        validUntil: formatDate(new Date(result.validUntilIso)),
        pricePaid: coverage?.pricePerYear ?? 0,
      },
    });
  },

  failEmission: () => set({ emission: 'error', issuedPolicy: null }),

  reset: () => set({ ...initialState }),
}));
