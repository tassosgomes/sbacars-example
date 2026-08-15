/// <reference types="vite/client" />

export interface RuntimeEnv {
  API_BASE_URL: string;
}

declare global {
  interface Window {
    RUNTIME_ENV?: Partial<RuntimeEnv>;
  }
}

export {};
