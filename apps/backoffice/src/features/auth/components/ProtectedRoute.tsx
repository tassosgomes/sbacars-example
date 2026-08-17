import { useAuth } from 'react-oidc-context';
import { Navigate, Outlet } from 'react-router-dom';

export interface ProtectedRouteProps {
  permissao?: string;
}

export function ProtectedRoute({ permissao }: ProtectedRouteProps = {}) {
  const auth = useAuth();

  if (auth.isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface">
        <p className="text-sm text-muted">Carregando sessão…</p>
      </div>
    );
  }

  if (auth.error) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface p-4">
        <p className="text-sm text-error">Erro de autenticação: {auth.error.message}</p>
      </div>
    );
  }

  if (!auth.isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (permissao) {
    const rawScope = (auth.user?.profile?.scope as string | undefined) ?? '';
    const scopes = rawScope.split(' ');
    if (!scopes.includes(permissao)) {
      return <Navigate to="/" replace />;
    }
  }

  return <Outlet />;
}
