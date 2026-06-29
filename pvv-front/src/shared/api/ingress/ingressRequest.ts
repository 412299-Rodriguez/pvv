import axios from 'axios';
import { axiosInstance } from '../axiosInstance';

interface IngressEnvelope<T> {
  statusCode: number;
  data: T | null;
  error: string | null;
}

/** Error carrying the BFF ingress status code (e.g. 404 unknown route). */
export class IngressError extends Error {
  readonly statusCode: number;

  constructor(statusCode: number, message: string) {
    super(message);
    this.name = 'IngressError';
    this.statusCode = statusCode;
  }
}

/**
 * The single call into the BFF gateway: `POST /api/ingress { hash, body }`.
 * Returns the envelope's `data` or throws an {@link IngressError}. The company /
 * session / Turnstile / client headers are attached by the axios interceptors.
 */
export async function ingressRequest<T>(hash: string, body?: unknown): Promise<T> {
  try {
    const response = await axiosInstance.post<IngressEnvelope<T>>('/api/ingress', {
      hash,
      body: body ?? {},
    });
    const envelope = response.data;
    if (envelope.data === null) {
      throw new IngressError(envelope.statusCode, envelope.error ?? 'Ingress error');
    }
    return envelope.data;
  } catch (error) {
    if (axios.isAxiosError(error) && error.response) {
      const envelope = error.response.data as IngressEnvelope<T> | undefined;
      throw new IngressError(
        envelope?.statusCode ?? error.response.status,
        envelope?.error ?? error.message,
      );
    }
    throw error;
  }
}
