import { create } from 'zustand';

export interface AlertContent {
  title: string;
  detail?: string | undefined;
}

interface AlertModalState {
  alert: AlertContent | null;
  /** Show a friendly error/alert modal. */
  showAlert: (title: string, detail?: string) => void;
  closeAlert: () => void;
}

/**
 * Global alert modal state. Any feature can surface an error in a modal (instead
 * of inline in a card) by calling showAlert(); the AlertModal widget renders it.
 */
export const useAlertModalStore = create<AlertModalState>((set) => ({
  alert: null,
  showAlert: (title, detail) => set({ alert: { title, detail } }),
  closeAlert: () => set({ alert: null }),
}));
