import { runtimeConfig } from '@/config/runtimeConfig';

/**
 * The one place this app calls `fetch` against gateway-public. Every future feature call routes
 * through here so OpenTelemetry's fetch instrumentation (`src/telemetry/index.ts`) — which patches
 * the global `fetch`, not this function — sees it as a normal outgoing request and attaches the
 * `traceparent` header the same way it would for any other call to this origin.
 */
export function apiFetch(path: string, init?: RequestInit): Promise<Response> {
  return fetch(`${runtimeConfig.API_BASE_URL}${path}`, init);
}
