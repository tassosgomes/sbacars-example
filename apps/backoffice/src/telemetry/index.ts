import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';
import { XMLHttpRequestInstrumentation } from '@opentelemetry/instrumentation-xml-http-request';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { resourceFromAttributes } from '@opentelemetry/resources';
import { ATTR_SERVICE_NAME } from '@opentelemetry/semantic-conventions';
import { BatchSpanProcessor } from '@opentelemetry/sdk-trace-base';
import type { ReadableSpan, SpanProcessor } from '@opentelemetry/sdk-trace-base';
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { ZoneContextManager } from '@opentelemetry/context-zone';
import { runtimeConfig } from '@/config/runtimeConfig';

// service.name = the app's own folder name (react-observability skill, §3): distinguishes this
// SPA's spans from gateway-backoffice's/the services' own — same word, different signal, on
// purpose (the browser and the backend edge happen to share a domain name here, not an identity).
const SERVICE_NAME = 'backoffice';

let started = false;

/**
 * Wires OpenTelemetry Web (A8, §8): a trace span per `fetch`/`XHR` call, exported as OTLP/HTTP to
 * the Aspire Dashboard, with the W3C `traceparent` header attached to same-origin requests
 * automatically and to gateway-backoffice's cross-origin one via `propagateTraceHeaderCorsUrls` —
 * this is the mechanism that makes the SPA the place a trace is *born*, not just where it is
 * displayed.
 *
 * No-op when `RuntimeEnv.OTEL_EXPORTER_OTLP_ENDPOINT` is unset — the same posture
 * `AddSbaCarsObservability` takes on the backend (see its remarks): a developer with no Aspire
 * Dashboard running is not misconfigured, so this must never throw or block rendering over it.
 * Idempotent: a second call is a no-op, so it is safe to call unconditionally from `main.tsx`.
 */
export function initTelemetry(): void {
  if (started) {
    return;
  }

  const endpoint = runtimeConfig.OTEL_EXPORTER_OTLP_ENDPOINT;
  if (!endpoint) {
    return;
  }

  // Only the gateway this app actually calls is cross-origin from the SPA's own origin (5174 vs.
  // 5001) — propagateTraceHeaderCorsUrls is an allow-list, not a default-on: without it, the
  // browser's CORS preflight would strip `traceparent` before it ever left the tab. A RegExp, not
  // the plain origin string: shouldPropagateTraceHeaders (@opentelemetry/core) compares a bare
  // string entry against the *entire* request URL with `===`, which would never match any actual
  // request path — only a pattern anchored on the origin matches every path under it.
  const apiOrigin = new URL(runtimeConfig.API_BASE_URL).origin;
  const apiOriginPattern = new RegExp(`^${escapeRegExp(apiOrigin)}`);

  const provider = new WebTracerProvider({
    resource: resourceFromAttributes({ [ATTR_SERVICE_NAME]: SERVICE_NAME }),
    spanProcessors: [
      // Runs before the batch exporter, in registration order (same rule the backend's
      // BaseProcessor<Activity> chain follows) — every span is stripped before it is ever queued
      // for export.
      new StripSensitiveUrlDataProcessor(),
      new BatchSpanProcessor(new OTLPTraceExporter({ url: `${endpoint}/v1/traces` })),
    ],
  });

  // ZoneContextManager, not the SDK's plain default: a fetch/XHR call's response (and the
  // instrumentation code that reads it) runs after at least one microtask/task boundary, and only
  // Zone.js keeps "which span is active" correct across that gap — the stack-based default is
  // only reliable for purely synchronous code.
  provider.register({ contextManager: new ZoneContextManager() });

  registerInstrumentations({
    instrumentations: [
      new FetchInstrumentation({
        propagateTraceHeaderCorsUrls: [apiOriginPattern],
        // Never trace the exporter's own delivery to the collector — nothing to learn from it,
        // and it would otherwise re-trigger export on every export.
        ignoreUrls: [endpoint],
        clearTimingResources: true,
      }),
      new XMLHttpRequestInstrumentation({
        propagateTraceHeaderCorsUrls: [apiOriginPattern],
        ignoreUrls: [endpoint],
      }),
    ],
  });

  started = true;
}

/**
 * §5.7/§8: no personal data in a span attribute — and the backoffice specifically is the app
 * where an operator's search/filter query strings are most likely to carry a customer's name,
 * document number or contact detail. The fetch/XHR instrumentations' default URL attribute
 * (`url.full`, `http.url` on older semantic conventions) includes the query string; every URL this
 * app talks to is stripped of it unconditionally, rather than trying to recognize case by case
 * which query string is "safe" — the same last-mile-before-export shape as the backend's
 * `SensitiveDataRedactionProcessor` (`BaseProcessor<Activity>.OnEnd`), just on the two attribute
 * names the JS SDK actually uses.
 */
class StripSensitiveUrlDataProcessor implements SpanProcessor {
  private static readonly UrlAttributeKeys = ['http.url', 'url.full'] as const;

  onStart(): void {
    // No-op: the URL attribute is only set once the request completes.
  }

  onEnd(span: ReadableSpan): void {
    const attributes = span.attributes as Record<string, unknown>;
    for (const key of StripSensitiveUrlDataProcessor.UrlAttributeKeys) {
      const value = attributes[key];
      if (typeof value === 'string' && value.includes('?')) {
        attributes[key] = value.split('?')[0];
      }
    }
  }

  shutdown(): Promise<void> {
    return Promise.resolve();
  }

  forceFlush(): Promise<void> {
    return Promise.resolve();
  }
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
