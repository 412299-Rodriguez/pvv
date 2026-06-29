import type { DocumentType, Policyholder } from './types';

/**
 * Demo contact "found" when the user enters a document number.
 * In production this is the result of a customer lookup by document.
 */
export const demoPolicyholder: Policyholder = {
  firstName: 'Juan',
  lastName: 'Pérez',
  email: 'juan@email.com',
  phone: '911 2345678',
};

export const demoDocumentType: DocumentType = 'DNI';
export const demoDocumentNumber = '32456789';
