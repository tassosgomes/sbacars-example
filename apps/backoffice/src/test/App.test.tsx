import { fireEvent, render, screen } from '@testing-library/react';
import { useAuth } from 'react-oidc-context';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import { AppRouter } from '@app/router';
import { BackofficeLayout } from '@app/layouts/BackofficeLayout';
import { LoginPage } from '@features/auth';

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
          profile: { name: 'Ana Operação', preferred_username: 'ana' },
        } as ReturnType<typeof useAuth>['user'],
      }),
    );

    render(<AppRouter />);

    expect(screen.getByText('AutoTransparência')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /dashboard/i })).toBeInTheDocument();
    expect(screen.getByText(/dashboard metrics are not available yet/i)).toBeInTheDocument();
  });

  it('redirects unauthenticated users from protected routes to login', () => {
    mockedUseAuth.mockReturnValue(createAuthState());

    render(<AppRouter />);

    expect(screen.getByRole('heading', { name: /sign in/i })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: /dashboard/i })).not.toBeInTheDocument();
  });

  it('calls signinRedirect when Sign in is clicked on the login page', async () => {
    const signinRedirect = vi.fn();
    mockedUseAuth.mockReturnValue(createAuthState({ signinRedirect }));

    render(<LoginPage />);

    await fireEvent.click(screen.getByRole('button', { name: /sign in/i }));

    expect(signinRedirect).toHaveBeenCalledOnce();
  });

  it('calls signoutRedirect when Sign out is clicked in the layout', async () => {
    const signoutRedirect = vi.fn();
    mockedUseAuth.mockReturnValue(
      createAuthState({
        isAuthenticated: true,
        signoutRedirect,
        user: {
          profile: { name: 'Bruno Estoque', preferred_username: 'bruno' },
        } as ReturnType<typeof useAuth>['user'],
      }),
    );

    render(
      <MemoryRouter>
        <BackofficeLayout />
      </MemoryRouter>,
    );

    expect(screen.getByText('Bruno Estoque')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /sign out/i }));

    expect(signoutRedirect).toHaveBeenCalledOnce();
  });
});
