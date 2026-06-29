import type { SessionState } from './types';

/**
 * Derived selectors. Keep computation out of components so it can be reused and
 * tested. Pass these to `useSessionStore(selector)`.
 */

/** The currently selected coverage object (or undefined). */
export const selectSelectedCoverage = (s: SessionState) =>
  s.coverages.find((c) => c.id === s.selectedCoverageId);

/** Whether step 1 is ready to submit. */
export const selectCanSubmitPlate = (s: SessionState) => {
  const compact = s.plate.replace(/\s/g, '');
  return compact.length >= 6 && compact.length <= 7 && s.termsAccepted;
};
