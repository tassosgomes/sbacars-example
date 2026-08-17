import { runtimeConfig } from '@/config/runtimeConfig';
import { parseProblemDetails } from './problemDetails';

export function getAccessToken(): string | null {
  try {
    if (typeof window === 'undefined') return null;
    const key = `oidc.user:${runtimeConfig.OIDC_AUTHORITY}:${runtimeConfig.OIDC_CLIENT_ID}`;
    const rawUser = window.sessionStorage.getItem(key);
    if (rawUser) {
      const user = JSON.parse(rawUser);
      return user.access_token ?? null;
    }
  } catch {
    // Ignore storage parse errors
  }
  return null;
}

/**
 * The one place this app calls `fetch` against gateway-backoffice. Every future feature call
 * routes through here so OpenTelemetry's fetch instrumentation (`src/telemetry/index.ts`) — which
 * patches the global `fetch`, not this function — sees it as a normal outgoing request and
 * attaches the `traceparent` header the same way it would for any other call to this origin.
 */
export async function apiFetch(path: string, init?: RequestInit): Promise<Response> {
  const token = getAccessToken();
  const headers = new Headers(init?.headers);

  if (token && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  if (init?.body && !headers.has('Content-Type') && typeof init.body === 'string') {
    headers.set('Content-Type', 'application/json');
  }

  return fetch(`${runtimeConfig.API_BASE_URL}${path}`, {
    ...init,
    headers,
  });
}

/**
 * Helper for JSON requests that throws ApiError on non-2xx responses.
 */
export async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await apiFetch(path, init);
  if (!response.ok) {
    throw await parseProblemDetails(response);
  }
  if (response.status === 204) {
    return undefined as unknown as T;
  }
  return response.json() as Promise<T>;
}
