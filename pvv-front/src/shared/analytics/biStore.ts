import { create } from 'zustand';

/**
 * BI / behavioural-tracking store — RESERVED for HU-07 (pvv-bff leads).
 *
 * The wizard is meant to emit a tracking event per step so the BFF can build the
 * lead profile (see the architecture BI events table). This skeleton holds the
 * event shape and a local `track` action, but it is NOT wired into any component
 * yet. HU-07 will forward each event to the BFF wizard-event endpoint
 * (LEAD_EVENT) instead of (or in addition to) buffering it locally.
 */
export type BiEventName =
  | 'wizard_start'
  | 'plate_entered'
  | 'document_entered'
  | 'personal_completed'
  | 'coverage_selected'
  | 'payment_started'
  | 'policy_emitted'
  | 'wizard_error';

export interface BiEvent {
  name: BiEventName;
  /** ISO timestamp of when the event fired. */
  at: string;
  payload?: Record<string, unknown> | undefined;
}

interface BiState {
  events: BiEvent[];
  /** TODO HU-07: also POST the event to the BFF wizard-event endpoint. */
  track: (name: BiEventName, payload?: Record<string, unknown>) => void;
}

export const useBIStore = create<BiState>((set) => ({
  events: [],
  track: (name, payload) =>
    set((s) => ({ events: [...s.events, { name, at: new Date().toISOString(), payload }] })),
}));
