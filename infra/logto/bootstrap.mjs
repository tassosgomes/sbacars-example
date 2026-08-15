#!/usr/bin/env node
// A5 — Bootstrap idempotente do Logto via Management API (§5.1 do plano de fundação).
//
// Cria, se ainda não existirem, tudo que a §5.1 exige:
//   1. API resource `https://api.sbacars.app` com os scopes da Fase 1.
//   2. Aplicação SPA `backoffice` (Authorization Code + PKCE) com as redirect URIs de dev.
//   3. Papéis `estoque` e `operacao`, com os scopes da tabela da §5.4.
//   4. Usuários de desenvolvimento `ana` (operacao) e `bruno` (estoque).
//
// Reexecutável sem duplicar nada: cada passo primeiro procura pelo identificador de negócio
// (indicator, name, username) e só cria se não encontrar. Ao final, grava o `client_id` gerado
// pelo Logto para a aplicação `backoffice` em `apps/backoffice/public/runtime-env.js` (e no
// `dist/` equivalente, se existir) — Logto não permite escolher o `client_id`, então o script é a
// única fonte de verdade sobre qual id o frontend deve usar em cada base de dados nova.
//
// Por que Node puro (sem dependências): o monorepo já tem Node 20+ como pré-requisito e usa
// workspaces npm; um script sem `npm install` próprio evita acoplar `infra/` ao lockfile das
// apps e fica igualmente executável via `node infra/logto/bootstrap.mjs` no host ou dentro de um
// container `node:*-alpine` no compose. `fetch` é global desde o Node 18.
//
// Autenticação na Management API: exige uma aplicação Machine-to-Machine já existente no Logto,
// com o papel embutido "Logto Management API access". Criar essa M2M app é o único passo manual
// que não tem equivalente por API no Logto self-hosted (é assim por desenho: não existe endpoint
// para se auto-conceder acesso administrativo) — ver infra/logto/README.md para o passo a passo
// exato pelo console. As credenciais chegam aqui por variável de ambiente.

import { readFile, writeFile } from 'node:fs/promises';
import { existsSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '..', '..');

// Carrega infra/logto/.env automaticamente, para que o comando documentado no README
// (`node infra/logto/bootstrap.mjs`) funcione sem exportar variável à mão. Variáveis já
// presentes no ambiente têm precedência sobre o arquivo.
const envFile = path.join(__dirname, '.env');
if (existsSync(envFile)) {
  process.loadEnvFile(envFile);
}

const LOGTO_ENDPOINT = (process.env.LOGTO_ENDPOINT ?? 'http://localhost:3001').replace(/\/$/, '');
const M2M_APP_ID = requireEnv('LOGTO_M2M_APP_ID');
const M2M_APP_SECRET = requireEnv('LOGTO_M2M_APP_SECRET');

const API_RESOURCE_INDICATOR = 'https://api.sbacars.app';
const API_RESOURCE_NAME = 'sbacars API';

// Fase 1 apenas (§5.1 item 1) — compra:gerenciar e reserva:estender ficam para a Fase 2.
const SCOPES = [
  { name: 'estoque:gerenciar', description: 'Escrita em inventory-service (curadoria de oferta, disponibilidade)' },
  { name: 'estoque:ler', description: 'Leitura operacional em inventory-service' },
  { name: 'catalogo:gerenciar', description: 'Conteúdo comercial e mídia em catalog-service' },
  { name: 'atendimento:gerenciar', description: 'Painel e continuidade em interest-service' },
];

const BACKOFFICE_APP_NAME = 'backoffice';
const BACKOFFICE_REDIRECT_URIS = ['http://localhost:5174/auth/callback'];
const BACKOFFICE_POST_LOGOUT_REDIRECT_URIS = ['http://localhost:5174/login'];

// Tabela da §5.4: papel -> scopes concedidos.
const ROLES = {
  estoque: {
    description: 'Operação de estoque — curadoria de oferta e disponibilidade (D02)',
    scopes: ['estoque:gerenciar', 'estoque:ler'],
  },
  operacao: {
    description: 'Operação central — catálogo, atendimento e leitura de estoque (D01/D02/D03)',
    scopes: ['estoque:ler', 'catalogo:gerenciar', 'atendimento:gerenciar'],
  },
};

// Usuários de desenvolvimento (§5.1 item 4) — mesma função dos antigos usuários do Keycloak:
// exercitar a mecânica do OIDC, não dado real.
const DEV_USERS = [
  { username: 'ana', name: 'Ana Operação', primaryEmail: 'ana@sbacars.local', password: 'ana123', role: 'operacao' },
  { username: 'bruno', name: 'Bruno Estoque', primaryEmail: 'bruno@sbacars.local', password: 'bruno123', role: 'estoque' },
];

const RUNTIME_ENV_TARGETS = [
  path.join(repoRoot, 'apps', 'backoffice', 'public', 'runtime-env.js'),
  path.join(repoRoot, 'apps', 'backoffice', 'dist', 'runtime-env.js'),
];

function requireEnv(key) {
  const value = process.env[key];
  if (!value) {
    console.error(
      `[bootstrap] variável de ambiente ${key} não definida.\n` +
        '  Este script precisa de credenciais de uma aplicação Machine-to-Machine já criada no\n' +
        '  console do Logto — ver infra/logto/README.md ("Passo manual obrigatório").',
    );
    process.exit(1);
  }
  return value;
}

async function logtoFetch(pathname, { method = 'GET', body, token, form } = {}) {
  const url = `${LOGTO_ENDPOINT}${pathname}`;
  const headers = {};
  if (token) headers.Authorization = `Bearer ${token}`;
  let requestBody;
  if (form) {
    headers['Content-Type'] = 'application/x-www-form-urlencoded';
    requestBody = new URLSearchParams(form).toString();
  } else if (body !== undefined) {
    headers['Content-Type'] = 'application/json';
    requestBody = JSON.stringify(body);
  }

  const response = await fetch(url, { method, headers, body: requestBody });
  const text = await response.text();
  const json = text ? JSON.parse(text) : undefined;

  if (!response.ok) {
    throw new Error(
      `${method} ${pathname} -> ${response.status} ${response.statusText}: ${text || '(sem corpo)'}`,
    );
  }
  return json;
}

// Indicador da Management API. É um identificador LÓGICO derivado do id do tenant, não o endereço
// do deployment: no Logto self-hosted o tenant é sempre `default`, então o indicador é
// `https://default.logto.app/api` mesmo quando o Logto roda em http://localhost:3001.
// A documentação do Logto Cloud usa `$TENANT_ENDPOINT/api`, o que confunde — lá o endpoint é
// `https://<tenant>.logto.app` e por coincidência bate com o indicador. Aqui não bate.
// Verificável na própria instância: select indicator from resources where tenant_id = 'default'.
const MANAGEMENT_API_INDICATOR = 'https://default.logto.app/api';

async function getManagementApiToken() {
  const response = await logtoFetch('/oidc/token', {
    method: 'POST',
    form: {
      grant_type: 'client_credentials',
      client_id: M2M_APP_ID,
      client_secret: M2M_APP_SECRET,
      resource: MANAGEMENT_API_INDICATOR,
      scope: 'all',
    },
  });
  return response.access_token;
}

async function listAll(pathname, token) {
  // Tenant de desenvolvimento é pequeno — uma página de 100 cobre tudo que este script cria.
  return logtoFetch(`${pathname}${pathname.includes('?') ? '&' : '?'}page=1&page_size=100`, { token });
}

async function ensureApiResource(token) {
  const resources = await listAll('/api/resources', token);
  const existing = resources.find((r) => r.indicator === API_RESOURCE_INDICATOR);
  if (existing) {
    console.log(`[bootstrap] API resource "${API_RESOURCE_INDICATOR}" já existe (${existing.id}).`);
    return existing;
  }
  const created = await logtoFetch('/api/resources', {
    method: 'POST',
    token,
    body: { name: API_RESOURCE_NAME, indicator: API_RESOURCE_INDICATOR },
  });
  console.log(`[bootstrap] API resource "${API_RESOURCE_INDICATOR}" criado (${created.id}).`);
  return created;
}

async function ensureScopes(token, resourceId) {
  const existingScopes = await listAll(`/api/resources/${resourceId}/scopes`, token);
  const byName = new Map(existingScopes.map((s) => [s.name, s]));

  for (const scope of SCOPES) {
    if (byName.has(scope.name)) {
      console.log(`[bootstrap] scope "${scope.name}" já existe.`);
      continue;
    }
    const created = await logtoFetch(`/api/resources/${resourceId}/scopes`, {
      method: 'POST',
      token,
      body: { name: scope.name, description: scope.description },
    });
    byName.set(scope.name, created);
    console.log(`[bootstrap] scope "${scope.name}" criado (${created.id}).`);
  }
  return byName;
}

async function ensureApplication(token) {
  const applications = await listAll('/api/applications', token);
  const existing = applications.find((a) => a.name === BACKOFFICE_APP_NAME);
  if (existing) {
    console.log(`[bootstrap] aplicação "${BACKOFFICE_APP_NAME}" já existe (${existing.id}).`);
    return existing;
  }
  const created = await logtoFetch('/api/applications', {
    method: 'POST',
    token,
    body: {
      name: BACKOFFICE_APP_NAME,
      type: 'SPA',
      description: 'Backoffice SPA (Authorization Code + PKCE) — inventory, leads, purchase stub',
      oidcClientMetadata: {
        redirectUris: BACKOFFICE_REDIRECT_URIS,
        postLogoutRedirectUris: BACKOFFICE_POST_LOGOUT_REDIRECT_URIS,
      },
    },
  });
  console.log(`[bootstrap] aplicação "${BACKOFFICE_APP_NAME}" criada (${created.id}).`);
  return created;
}

async function ensureRoles(token, scopesByName) {
  const existingRoles = await listAll('/api/roles', token);
  const byName = new Map(existingRoles.map((r) => [r.name, r]));
  const result = new Map();

  for (const [roleName, { description, scopes }] of Object.entries(ROLES)) {
    let role = byName.get(roleName);
    if (role) {
      console.log(`[bootstrap] papel "${roleName}" já existe (${role.id}).`);
    } else {
      role = await logtoFetch('/api/roles', {
        method: 'POST',
        token,
        body: { name: roleName, description, type: 'User' },
      });
      console.log(`[bootstrap] papel "${roleName}" criado (${role.id}).`);
    }

    const currentScopes = await logtoFetch(`/api/roles/${role.id}/scopes`, { token });
    const currentScopeIds = new Set(currentScopes.map((s) => s.id));
    const wantedScopeIds = scopes.map((scopeName) => {
      const scope = scopesByName.get(scopeName);
      if (!scope) throw new Error(`Scope "${scopeName}" não foi criado antes do papel "${roleName}".`);
      return scope.id;
    });
    const missingScopeIds = wantedScopeIds.filter((id) => !currentScopeIds.has(id));

    if (missingScopeIds.length > 0) {
      await logtoFetch(`/api/roles/${role.id}/scopes`, {
        method: 'POST',
        token,
        body: { scopeIds: missingScopeIds },
      });
      console.log(`[bootstrap] papel "${roleName}" ganhou ${missingScopeIds.length} scope(s) novo(s).`);
    } else {
      console.log(`[bootstrap] papel "${roleName}" já tinha todos os scopes esperados.`);
    }

    result.set(roleName, role);
  }
  return result;
}

async function ensureUsers(token, rolesByName) {
  const existingUsers = await listAll('/api/users', token);
  const byUsername = new Map(existingUsers.map((u) => [u.username, u]));

  for (const devUser of DEV_USERS) {
    let user = byUsername.get(devUser.username);
    if (user) {
      console.log(`[bootstrap] usuário "${devUser.username}" já existe (${user.id}).`);
    } else {
      user = await logtoFetch('/api/users', {
        method: 'POST',
        token,
        body: {
          username: devUser.username,
          name: devUser.name,
          primaryEmail: devUser.primaryEmail,
          password: devUser.password,
        },
      });
      console.log(`[bootstrap] usuário "${devUser.username}" criado (${user.id}).`);
    }

    const role = rolesByName.get(devUser.role);
    const currentRoles = await logtoFetch(`/api/users/${user.id}/roles`, { token });
    const alreadyHasRole = currentRoles.some((r) => r.id === role.id);

    if (!alreadyHasRole) {
      await logtoFetch(`/api/users/${user.id}/roles`, {
        method: 'POST',
        token,
        body: { roleIds: [role.id] },
      });
      console.log(`[bootstrap] usuário "${devUser.username}" recebeu o papel "${devUser.role}".`);
    } else {
      console.log(`[bootstrap] usuário "${devUser.username}" já tinha o papel "${devUser.role}".`);
    }
  }
}

async function updateRuntimeEnvFiles(clientId) {
  for (const target of RUNTIME_ENV_TARGETS) {
    if (!existsSync(target)) continue;
    const original = await readFile(target, 'utf8');
    const updated = original.replace(
      /OIDC_CLIENT_ID:\s*'[^']*'/,
      `OIDC_CLIENT_ID: '${clientId}'`,
    );
    if (updated === original) {
      console.log(`[bootstrap] ${path.relative(repoRoot, target)}: OIDC_CLIENT_ID já está com "${clientId}".`);
      continue;
    }
    await writeFile(target, updated, 'utf8');
    console.log(`[bootstrap] ${path.relative(repoRoot, target)} atualizado com OIDC_CLIENT_ID="${clientId}".`);
  }
}

async function main() {
  console.log(`[bootstrap] Logto endpoint: ${LOGTO_ENDPOINT}`);
  const token = await getManagementApiToken();
  console.log('[bootstrap] token de Management API obtido.');

  const resource = await ensureApiResource(token);
  const scopesByName = await ensureScopes(token, resource.id);
  const application = await ensureApplication(token);
  const rolesByName = await ensureRoles(token, scopesByName);
  await ensureUsers(token, rolesByName);
  await updateRuntimeEnvFiles(application.id);

  console.log('\n[bootstrap] concluído.');
  console.log(`  API resource: ${API_RESOURCE_INDICATOR} (${resource.id})`);
  console.log(`  Aplicação backoffice: ${application.id}`);
  console.log('  Papéis: estoque, operacao');
  console.log('  Usuários de dev: ana/ana123 (operacao), bruno/bruno123 (estoque)');
}

main().catch((error) => {
  console.error('[bootstrap] falhou:', error.message ?? error);
  process.exit(1);
});
