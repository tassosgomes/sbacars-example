/// <reference types="vite/client" />

export interface RuntimeEnv {
  API_BASE_URL: string;
  /**
   * OTLP/HTTP endpoint of the Aspire Dashboard (A8, §8/§10) — e.g. `http://localhost:18890`.
   * Empty/absent disables OpenTelemetry Web entirely (see `src/telemetry/index.ts`): the SPA
   * boots and works with no collector running, the same posture the six .NET processes take when
   * `Observability:OtlpEndpoint` is unset.
   */
  OTEL_EXPORTER_OTLP_ENDPOINT?: string;
}

declare global {
  interface Window {
    RUNTIME_ENV?: Partial<RuntimeEnv>;
  }
}

export {};
