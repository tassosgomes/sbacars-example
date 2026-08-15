import type { ReactNode } from 'react';
import { AuthProvider } from 'react-oidc-context';

import { getAuthProviderProps } from '../config/oidcConfig';

export function AppAuthProvider({ children }: { children: ReactNode }) {
  return <AuthProvider {...getAuthProviderProps()}>{children}</AuthProvider>;
}
