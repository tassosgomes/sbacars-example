import { AppRouter } from '@app/router';
import { AppAuthProvider } from '@features/auth';

export function App() {
  return (
    <AppAuthProvider>
      <AppRouter />
    </AppAuthProvider>
  );
}
