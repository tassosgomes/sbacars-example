import { Button, Card } from '@sbacars/ui';
import { useAuth } from 'react-oidc-context';
import { Navigate } from 'react-router-dom';

export function LoginPage() {
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

  if (auth.isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface p-4">
      <Card
        title="Sign in"
        description="Sign in with your organization account to access the backoffice."
        className="w-full max-w-md"
      >
        <Button type="button" onClick={() => auth.signinRedirect()}>Sign in</Button>
      </Card>
    </div>
  );
}
