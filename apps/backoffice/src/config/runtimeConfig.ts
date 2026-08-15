import type { RuntimeEnv } from '@/vite-env';

const DEV_DEFAULTS: RuntimeEnv = {
  API_BASE_URL: 'http://localhost:5001',
  OIDC_AUTHORITY: 'http://localhost:8080/realms/sbacars',
  OIDC_CLIENT_ID: 'backoffice',
};

function getRuntimeEnv(): RuntimeEnv {
  const env = window.RUNTIME_ENV ?? {};

  return {
    API_BASE_URL: env.API_BASE_URL ?? DEV_DEFAULTS.API_BASE_URL,
    OIDC_AUTHORITY: env.OIDC_AUTHORITY ?? DEV_DEFAULTS.OIDC_AUTHORITY,
    OIDC_CLIENT_ID: env.OIDC_CLIENT_ID ?? DEV_DEFAULTS.OIDC_CLIENT_ID,
  };
}

export const runtimeConfig = getRuntimeEnv();
