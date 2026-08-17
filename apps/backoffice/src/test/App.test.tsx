import { fireEvent, screen } from '@testing-library/react';
import { useAuth } from 'react-oidc-context';
import { describe, expect, it, vi } from 'vitest';

import { AppRouter } from '@app/router';
import { BackofficeLayout } from '@app/layouts/BackofficeLayout';
import { LoginPage } from '@features/auth';
import { renderWithProviders } from './test-utils';

const mockedUseAuth = vi.mocked(useAuth);

function createAuthState(
  overrides: Partial<ReturnType<typeof useAuth>> = {},
): ReturnType<typeof useAuth> {
  return {
    isLoading: false,
    isAuthenticated: false,
    error: undefined,
    user: undefined,
    signinRedirect: vi.fn(),
    signoutRedirect: vi.fn(),
    ...overrides,
  } as ReturnType<typeof useAuth>;
}

describe('Backoffice app', () => {
  it('renders the backoffice layout with dashboard when authenticated', () => {
    mockedUseAuth.mockReturnValue(
      createAuthState({
        isAuthenticated: true,
        user: {
          profile: { name: 'Ana Operação', preferred_username: 'ana', scope: 'estoque:ler' },
        } as unknown as ReturnType<typeof useAuth>['user'],
      }),
    );

    renderWithProviders(<AppRouter />, { withRouter: false });

    expect(screen.getByText('AutoTransparência')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /dashboard/i })).toBeInTheDocument();
  });

  it('redirects unauthenticated users from protected routes to login', () => {
    mockedUseAuth.mockReturnValue(createAuthState());

    renderWithProviders(<AppRouter />, { withRouter: false });

    expect(screen.getByRole('heading', { name: /sign in/i })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: /dashboard/i })).not.toBeInTheDocument();
  });

  it('calls signinRedirect when Sign in is clicked on the login page', async () => {
    const signinRedirect = vi.fn();
    mockedUseAuth.mockReturnValue(createAuthState({ signinRedirect }));

    renderWithProviders(<LoginPage />);

    await fireEvent.click(screen.getByRole('button', { name: /sign in/i }));

    expect(signinRedirect).toHaveBeenCalledOnce();
  });

  it('calls signoutRedirect when Sair is clicked in the layout', async () => {
    const signoutRedirect = vi.fn();
    mockedUseAuth.mockReturnValue(
      createAuthState({
        isAuthenticated: true,
        signoutRedirect,
        user: {
          profile: { name: 'Bruno Estoque', preferred_username: 'bruno', scope: 'estoque:ler' },
        } as unknown as ReturnType<typeof useAuth>['user'],
      }),
    );

    renderWithProviders(<BackofficeLayout />);

    expect(screen.getByText('Bruno Estoque')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /sair/i }));

    expect(signoutRedirect).toHaveBeenCalledOnce();
  });
});
