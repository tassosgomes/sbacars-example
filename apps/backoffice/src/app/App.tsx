import { QueryClientProvider } from '@tanstack/react-query';
import { AppRouter } from '@app/router';
import { AppAuthProvider } from '@features/auth';
import { queryClient } from '@/shared/api/queryClient';

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AppAuthProvider>
        <AppRouter />
      </AppAuthProvider>
    </QueryClientProvider>
  );
}
