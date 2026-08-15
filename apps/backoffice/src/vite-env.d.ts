/// <reference types="vite/client" />

export interface RuntimeEnv {
  API_BASE_URL: string;
  OIDC_AUTHORITY: string;
  OIDC_CLIENT_ID: string;
}

declare global {
  interface Window {
    RUNTIME_ENV?: Partial<RuntimeEnv>;
  }
}

export {};
