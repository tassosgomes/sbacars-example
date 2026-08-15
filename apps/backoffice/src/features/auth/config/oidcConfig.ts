import { WebStorageStateStore, type UserManagerSettings } from 'oidc-client-ts';
import type { AuthProviderProps } from 'react-oidc-context';

import { runtimeConfig } from '@/config/runtimeConfig';

export function getOidcConfig(): UserManagerSettings {
  const origin = window.location.origin;

  return {
    authority: runtimeConfig.OIDC_AUTHORITY,
    client_id: runtimeConfig.OIDC_CLIENT_ID,
    redirect_uri: `${origin}/auth/callback`,
    post_logout_redirect_uri: `${origin}/login`,
    response_type: 'code',
    scope: 'openid profile email',
    automaticSilentRenew: true,
    userStore: new WebStorageStateStore({ store: window.sessionStorage }),
  };
}

export function getAuthProviderProps(): AuthProviderProps {
  return {
    ...getOidcConfig(),
    onSigninCallback: () => {
      window.history.replaceState({}, document.title, window.location.pathname);
    },
  };
}
