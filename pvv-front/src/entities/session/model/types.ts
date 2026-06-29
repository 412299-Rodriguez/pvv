import type { Vehicle } from '@/entities/vehicle';
import type { Coverage } from '@/entities/coverage';
import type { ExistingPolicy, IssuedPolicy } from '@/entities/policy';
import type { DocumentType, Policyholder } from '@/entities/policyholder';
import type {
  CheckPlateResponse,
  LookupHolderResponse,
  EmitPolicyResponse,
} from '@/shared/api/ingress';

/** The five rendered screens of the purchase wizard. */
export type WizardStep = 'plate' | 'document' | 'personal' | 'checkout' | 'result';

/** Lifecycle of the policy emission on the result screen. */
export type EmissionStatus = 'idle' | 'emitting' | 'success' | 'error';

/**
 * The full purchase session — the aggregate that ties together the vehicle
 * lookup, the policyholder, the chosen coverage and the emission outcome.
 *
 * The store is a pure STATE container: the async lookups themselves live in the
 * `shared/api/ingress` client and are invoked from the feature components, which
 * then feed the results back here via the `apply…` actions. This keeps timing /
 * side effects out of the store and makes it trivial to test.
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
  /** Vehicle resolved by the plate lookup (null until step 1 succeeds). */
  vehicle: Vehicle | null;
  /** Existing in-force policy, set only when the plate already has coverage. */
  existingPolicy: ExistingPolicy | null;

  // ---- Step 2: document ------------------------------------------------
  documentType: DocumentType;
  documentNumber: string;

  // ---- Step 3: personal data ------------------------------------------
  /** True when contact data was pre-filled from a found customer. */
  contactFound: boolean;
  policyholder: Policyholder;

  // ---- Step 4: coverage + payment -------------------------------------
  /** Coverages quoted for this session (from the QUOTE handler). */
  coverages: Coverage[];
  selectedCoverageId: string | null;

  // ---- Step 5: emission -----------------------------------------------
  emission: EmissionStatus;
  issuedPolicy: IssuedPolicy | null;
}

/** Actions available on the session store. */
export interface SessionActions {
  goTo: (step: WizardStep) => void;
  /** Step one screen back through the data-entry flow (no-op on plate/result). */
  goBack: () => void;

  setPlate: (plate: string) => void;
  toggleTerms: () => void;
  /** Apply a plate-check result: open the renew modal or advance to step 2. */
  applyPlateCheck: (result: CheckPlateResponse) => void;
  confirmRenewal: () => void;
  cancelRenewal: () => void;

  setDocumentType: (type: DocumentType) => void;
  setDocumentNumber: (value: string) => void;
  /** Apply a holder-lookup result (pre-fills personal data when found). */
  applyHolderLookup: (result: LookupHolderResponse) => void;
  /** Skip the lookup entirely ("Continuar sin buscar") — empty personal data. */
  skipHolderLookup: () => void;

  setPolicyholderField: (field: keyof Policyholder, value: string) => void;
  submitPersonalData: () => void;

  /** Store the quoted coverages so the summary/payment can read price + name. */
  setCoverages: (coverages: Coverage[]) => void;
  selectCoverage: (id: string) => void;

  startEmission: () => void;
  /** Resolve a successful emission into the issued-policy ticket. */
  applyEmissionResult: (result: EmitPolicyResponse) => void;
  /** Mark the emission as failed (shows the retry screen). */
  failEmission: () => void;

  /** Reset everything back to a fresh step-1 session. */
  reset: () => void;
}

export type SessionStore = SessionState & SessionActions;
