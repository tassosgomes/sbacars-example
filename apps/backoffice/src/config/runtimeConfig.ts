import type { RuntimeEnv } from '@/vite-env';

const DEV_DEFAULTS: RuntimeEnv = {
  API_BASE_URL: 'http://localhost:5001',
  // ENDPOINT do Logto é http://localhost:3001 (decisão registrada em A5 para A6): resolvível pelo
  // navegador sem editar /etc/hosts. Issuer OIDC é sempre "<ENDPOINT>/oidc" (ver §5.1).
  OIDC_AUTHORITY: 'http://localhost:3001/oidc',
  // Gerado pelo Logto ao criar a aplicação SPA "backoffice" — não é escolhível como no Keycloak.
  // infra/logto/bootstrap.mjs atualiza este valor (e o runtime-env.js) automaticamente após
  // provisionar a aplicação; o placeholder abaixo só é usado antes do primeiro bootstrap rodar.
  OIDC_CLIENT_ID: 'REPLACE_AFTER_LOGTO_BOOTSTRAP',
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
