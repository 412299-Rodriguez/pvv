import type { DocumentType, Policyholder } from '@/entities/policyholder';
import type { IssuedPolicy } from '@/entities/policy';

/** The five rendered screens of the purchase wizard. */
export type WizardStep = 'plate' | 'document' | 'personal' | 'checkout' | 'result';

/** Lifecycle of the policy emission on the result screen. */
export type EmissionStatus = 'idle' | 'emitting' | 'success' | 'error';

/**
 * The full purchase session — the aggregate that ties together the vehicle
 * lookup, the policyholder, the chosen coverage and the emission outcome.
 */
export interface SessionState {
  /** Currently visible screen. */
  step: WizardStep;

  // ---- Step 1: plate + terms ------------------------------------------
  plate: string;
  termsAccepted: boolean;
  /** True once the user has acknowledged the "active policy" renew modal. */
  renewalAcknowledged: boolean;
  isRenewModalOpen: boolean;

  // ---- Step 2: document ------------------------------------------------
  documentType: DocumentType;
  documentNumber: string;

  // ---- Step 3: personal data ------------------------------------------
  /** True when contact data was pre-filled from a found customer. */
  contactFound: boolean;
  policyholder: Policyholder;

  // ---- Step 4: coverage + payment -------------------------------------
  selectedCoverageId: string | null;

  // ---- Step 5: emission -----------------------------------------------
  emission: EmissionStatus;
  issuedPolicy: IssuedPolicy | null;
}

/** Actions available on the session store. */
export interface SessionActions {
  goTo: (step: WizardStep) => void;

  setPlate: (plate: string) => void;
  toggleTerms: () => void;
  /** Submit step 1. Opens the renew modal for the demo trigger plate. */
  submitPlate: () => void;
  confirmRenewal: () => void;
  cancelRenewal: () => void;

  setDocumentType: (type: DocumentType) => void;
  setDocumentNumber: (value: string) => void;
  /** Submit step 2. `withContact` decides whether demo data is pre-filled. */
  submitDocument: (withContact: boolean) => void;

  setPolicyholderField: (field: keyof Policyholder, value: string) => void;
  submitPersonalData: () => void;

  selectCoverage: (id: string) => void;

  startEmission: () => void;
  /** Resolve the emission; on success an IssuedPolicy is built. */
  finishEmission: (result: Exclude<EmissionStatus, 'idle' | 'emitting'>) => void;

  /** Reset everything back to a fresh step-1 session. */
  reset: () => void;
}

export type SessionStore = SessionState & SessionActions;
