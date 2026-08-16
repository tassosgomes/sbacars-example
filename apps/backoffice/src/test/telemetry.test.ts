import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

/**
 * Proves the one behavior A8 requires an automated test for on the frontend side: a call to
 * gateway-backoffice carries the W3C `traceparent` header (§8) — the mechanism that makes the
 * trace born in the browser continue into the backend at all. `initTelemetry` patches the global
 * `fetch`, so the mock is installed first and the module is imported fresh per test to get a
 * clean "already started" flag.
 */
describe('OpenTelemetry Web — traceparent propagation', () => {
  const originalFetch = globalThis.fetch;

  beforeEach(() => {
    vi.resetModules();
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  it('attaches a traceparent header to a call made to the configured API origin', async () => {
    const innerFetch = vi.fn().mockResolvedValue(new Response('ok', { status: 200 }));
    vi.stubGlobal('fetch', innerFetch);

    const { initTelemetry } = await import('@/telemetry');
    initTelemetry();

    // http://localhost:5001 is gateway-backoffice's default API_BASE_URL (see runtimeConfig's
    // DEV_DEFAULTS) — cross-origin from jsdom's default test origin, which is exactly the case
    // propagateTraceHeaderCorsUrls exists for (§8: without it, the browser would strip the header
    // on a cross-origin request).
    await fetch('http://localhost:5001/api/inventory/vehicles');

    expect(innerFetch).toHaveBeenCalledTimes(1);
    const [, init] = innerFetch.mock.calls[0] as [unknown, RequestInit | undefined];
    const headers = new Headers(init?.headers);

    expect(headers.get('traceparent')).toMatch(/^00-[0-9a-f]{32}-[0-9a-f]{16}-0[01]$/);
  });
});
