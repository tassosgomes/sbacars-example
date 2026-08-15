import { useAuth } from 'react-oidc-context';
import { Navigate } from 'react-router-dom';

export function CallbackPage() {
  const auth = useAuth();

  if (auth.isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface">
        <p className="text-sm text-muted">Completing sign in…</p>
      </div>
    );
  }

  if (auth.error) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface p-4">
        <p className="text-sm text-error">Sign in failed: {auth.error.message}</p>
      </div>
    );
  }

  if (auth.isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface">
      <p className="text-sm text-muted">Completing sign in…</p>
    </div>
  );
}
