import type { RuntimeEnv } from '@/vite-env';

const DEV_DEFAULTS: RuntimeEnv = {
  API_BASE_URL: 'http://localhost:5000',
  // Aspire Dashboard's OTLP/HTTP endpoint (docker-compose, A8) — active by default in local dev
  // so `npm run dev` traces without extra setup; empty in an environment where no dashboard runs.
  OTEL_EXPORTER_OTLP_ENDPOINT: 'http://localhost:18890',
};

function getRuntimeEnv(): RuntimeEnv {
  const env = window.RUNTIME_ENV ?? {};

  return {
    API_BASE_URL: env.API_BASE_URL ?? DEV_DEFAULTS.API_BASE_URL,
    OTEL_EXPORTER_OTLP_ENDPOINT: env.OTEL_EXPORTER_OTLP_ENDPOINT ?? DEV_DEFAULTS.OTEL_EXPORTER_OTLP_ENDPOINT,
  };
}

export const runtimeConfig = getRuntimeEnv();
