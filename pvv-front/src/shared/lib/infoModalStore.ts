import { create } from 'zustand';

export type InfoModalKind = 'terms' | 'privacy' | 'faq';

interface InfoModalState {
  open: InfoModalKind | null;
  openModal: (kind: InfoModalKind) => void;
  close: () => void;
}

/**
 * Which info modal (legal terms / privacy / FAQ) is open. Lives in `shared` so
 * any feature can trigger it and the widget can render it.
 */
export const useInfoModalStore = create<InfoModalState>((set) => ({
  open: null,
  openModal: (kind) => set({ open: kind }),
  close: () => set({ open: null }),
}));
