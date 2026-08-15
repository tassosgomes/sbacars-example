import { useAuth } from 'react-oidc-context';
import { Navigate, Outlet } from 'react-router-dom';

export function ProtectedRoute() {
  const auth = useAuth();

  if (auth.isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface">
        <p className="text-sm text-muted">Loading session…</p>
      </div>
    );
  }

  if (auth.error) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface p-4">
        <p className="text-sm text-error">Authentication error: {auth.error.message}</p>
      </div>
    );
  }

  if (!auth.isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}
