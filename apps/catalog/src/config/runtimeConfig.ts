import type { RuntimeEnv } from '@/vite-env';

const DEV_DEFAULTS: RuntimeEnv = {
  API_BASE_URL: 'http://localhost:5000',
};

function getRuntimeEnv(): RuntimeEnv {
  const env = window.RUNTIME_ENV ?? {};

  return {
    API_BASE_URL: env.API_BASE_URL ?? DEV_DEFAULTS.API_BASE_URL,
  };
}

export const runtimeConfig = getRuntimeEnv();
