import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import type { ReactNode } from 'react';
import { afterAll, afterEach, beforeAll, vi } from 'vitest';
import { server } from './msw/server';

vi.mock('react-oidc-context', () => ({
  AuthProvider: ({ children }: { children: ReactNode }) => children,
  useAuth: vi.fn(() => ({
    user: {
      profile: {
        name: 'Ana Souza',
        preferred_username: 'ana.souza',
        scope: 'openid profile email estoque:gerenciar estoque:ler estoque:validar',
      },
    },
    isAuthenticated: true,
    signinRedirect: vi.fn(),
    signoutRedirect: vi.fn(),
  })),
}));

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' });
});

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  server.resetHandlers();
});

afterAll(() => {
  server.close();
});
