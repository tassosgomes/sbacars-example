import { WebStorageStateStore, type UserManagerSettings } from 'oidc-client-ts';
import type { AuthProviderProps } from 'react-oidc-context';

import { runtimeConfig } from '@/config/runtimeConfig';

// API resource pedido em todo login (§5.1/§5.2 do plano de fundação). Fixo em todos os
// ambientes — só a autoridade (Logto) muda entre local/dev/produção, a audience não (decisão 22).
const API_RESOURCE = 'https://api.sbacars.app';

// União de todos os scopes definidos para o resource acima (Fase 1, §5.4). O Logto só inclui no
// access token os scopes que o papel do usuário autenticado realmente concede — pedir a união
// inteira aqui é seguro e evita o frontend precisar saber "quem pode o quê".
const API_SCOPES = ['estoque:gerenciar', 'estoque:ler', 'catalogo:gerenciar', 'atendimento:gerenciar'];

export function getOidcConfig(): UserManagerSettings {
  const origin = window.location.origin;

  return {
    authority: runtimeConfig.OIDC_AUTHORITY,
    client_id: runtimeConfig.OIDC_CLIENT_ID,
    redirect_uri: `${origin}/auth/callback`,
    post_logout_redirect_uri: `${origin}/login`,
    response_type: 'code',
    scope: ['openid', 'profile', 'email', ...API_SCOPES].join(' '),
    // RFC 8707 resource indicator. Sem isso o Logto emite um access token opaco (ou com aud do
    // userinfo endpoint) em vez de um JWT com aud/scope do backend — era exatamente o problema do
    // Keycloak (`aud: account`). `oidc-client-ts` trata `resource` como propriedade nativa de
    // `OidcClientSettingsStore` e o propaga sozinho para as requisições de autorização, token e
    // refresh — não é preciso (nem deve ser usado) `extraQueryParams` para isso.
    resource: API_RESOURCE,
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
