import { create } from 'zustand';

import { sendLeadEvent } from '@/shared/api/ingress';
import type { LeadEventPayload } from '@/shared/api/ingress';

/**
 * BI / behavioural tracking. Every wizard action reports one event, which the
 * BFF projects onto the lead (its conversion-funnel view).
 *
 * Only the events the browser can actually observe live here. Payment
 * confirmation, abandonment and emission are recorded server-side: the user is
 * redirected to the checkout at that point, and this store's state does not
 * survive the round trip.
 */
export type BiEventName =
  | 'session_start'
  | 'plate_entered'
  | 'plate_validated'
  | 'document_entered'
  | 'holder_completed'
  | 'budget_calculated'
  | 'product_selected'
  | 'wizard_error';

interface BiState {
  /**
   * Identifies one purchase attempt, and therefore one lead. It is minted per
   * page load, which is all a restart needs today: "Volver al inicio" navigates
   * rather than resetting in place.
   */
  flowId: string;
  /** Guards the opening event against React's double effect invocation in dev. */
  sessionStarted: boolean;
  /** Reports that the portal finished loading; safe to call more than once. */
  startSession: () => void;
  track: (name: BiEventName, payload?: LeadEventPayload) => void;
}

export const useBIStore = create<BiState>((set, get) => ({
  flowId: crypto.randomUUID(),
  sessionStarted: false,

  startSession: () => {
    if (get().sessionStarted) return;
    set({ sessionStarted: true });
    sendLeadEvent('session_start', get().flowId);
  },

  track: (name, payload) => sendLeadEvent(name, get().flowId, payload),
}));
