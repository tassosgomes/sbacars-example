# Logto — infraestrutura de identidade (A5)

Este diretório contém tudo que a §5.1 do [plano de fundação do
backend](../../docs/architecture/backend-foundation.md) pede para o Logto: o banco dedicado, o
script de bootstrap da Management API e este README.

Logto é o **único** provedor de identidade do projeto, em todos os ambientes (§5.0). O Keycloak
foi removido.

## Arquivos

| Arquivo | O que faz |
|---|---|
| `db-init.sql` | Cria a role e o banco Postgres `logto`, dedicados (fora do `sbacars`). Executado pelo job `logto-db-init` do `docker-compose.yml`. Idempotente. |
| `bootstrap.mjs` | Cria via Management API o API resource, os scopes, a aplicação SPA `backoffice`, os papéis e os usuários de desenvolvimento. Idempotente — ver "Como rodar" abaixo. |
| `.env.example` | Modelo para `infra/logto/.env` (gitignored) com as credenciais da aplicação M2M — ver "Passo manual obrigatório". |

## Subindo o Logto

```bash
docker compose up -d postgres logto-db-init logto-seed logto
```

Isso é automático e não pede nada manual:

1. `postgres` sobe (o mesmo Postgres de A4, banco `sbacars`).
2. `logto-db-init` cria a role `logto` e o banco `logto` na mesma instância, e sai.
3. `logto-seed` roda `npx @logto/cli db seed --swe` — cria o schema interno do Logto. `--swe`
   ("skip when exists") é a flag oficial do CLI para isso ser seguro de reexecutar: se a tabela de
   config já existe, o seed é pulado.
4. `logto` sobe servindo OIDC em `:3001` e o console administrativo em `:3002`.

Verificação rápida:

```bash
curl -s http://localhost:3001/oidc/.well-known/openid-configuration | jq '.issuer'
# "http://localhost:3001/oidc"
```

**Nota sobre a role do banco:** `db-init.sql` concede `CREATEROLE` (não `NOCREATEROLE`, diferente
das roles de negócio de `backend/docker/postgres/init/`) porque o próprio `@logto/cli db seed`
cria internamente uma role por tenant para isolamento (`logto_tenant_<database>`, ver
`_before_all.sql` do pacote `@logto/schemas`) — sem `CREATEROLE` o seed falha com "permission
denied to create role". Isso foi confirmado rodando o seed de verdade contra este script, não é
suposição. A role continua sem `CREATEDB` e sem `SUPERUSER`.

## Passo manual obrigatório: criar a aplicação Machine-to-Machine

`bootstrap.mjs` fala com a Management API do Logto, que exige autenticação `client_credentials` de
uma aplicação Machine-to-Machine com o papel embutido **"Logto Management API access"**. No Logto
self-hosted (OSS) **não existe endpoint de API para criar essa primeira aplicação M2M** — seria a
aplicação se autoconceder acesso administrativo, o que a Management API propositalmente não
permite. Este é o único passo manual desta task; todo o resto é reexecutável por script.

Passo a passo (uma vez por instância do Logto — ou seja, uma vez por volume do Postgres local; se
o volume for recriado, repita):

1. Suba o Logto (seção acima) e abra o console em <http://localhost:3002>.
2. Na primeira execução, o console pede para criar a conta de administrador do tenant — crie com
   usuário/senha de desenvolvimento à sua escolha (não é usado por mais nada no projeto).
3. Vá em **Console → Applications → Create application → Machine-to-machine**.
4. Nomeie (sugestão: `sbacars-bootstrap`) e crie.
5. Na página da aplicação criada, aba **Permissions → Assign roles**, marque **"Logto Management
   API access"** (papel já embutido pelo seed, não precisa criar).
6. Copie o **App ID** e o **App secret** da aba **Settings**.
7. `cp infra/logto/.env.example infra/logto/.env` e preencha `LOGTO_M2M_APP_ID` e
   `LOGTO_M2M_APP_SECRET` com os valores copiados.

## Como rodar o bootstrap

```bash
export $(grep -v '^#' infra/logto/.env | xargs)
node infra/logto/bootstrap.mjs
```

O script:

1. Troca as credenciais M2M por um token de Management API
   (`POST /oidc/token`, `grant_type=client_credentials`, `resource=<endpoint>/api`, `scope=all`).
2. Garante o **API resource** `https://api.sbacars.app` com os 4 scopes da Fase 1
   (`estoque:gerenciar`, `estoque:ler`, `catalogo:gerenciar`, `atendimento:gerenciar`) —
   `compra:gerenciar` e `reserva:estender` (Fase 2) **não** entram.
3. Garante a **aplicação SPA `backoffice`** (Authorization Code + PKCE), com redirect URI
   `http://localhost:5174/auth/callback` e post-logout `http://localhost:5174/login`.
4. Garante os **papéis** `estoque` (→ `estoque:gerenciar`, `estoque:ler`) e `operacao` (→
   `estoque:ler`, `catalogo:gerenciar`, `atendimento:gerenciar`), conforme a tabela da §5.4.
5. Garante os **usuários de desenvolvimento**:

   | Usuário | Senha | Papel |
   |---|---|---|
   | `ana` | `ana123` | `operacao` |
   | `bruno` | `bruno123` | `estoque` |

6. Atualiza `OIDC_CLIENT_ID` em `apps/backoffice/public/runtime-env.js` (e no `dist/` equivalente,
   se existir) com o `client_id` que o Logto gerou para a aplicação `backoffice`. O Logto **não**
   deixa escolher o `client_id` — diferente do Keycloak, onde `backoffice` era um nome fixo — então
   o bootstrap é a única fonte de verdade sobre qual id o frontend deve usar contra esta instância.

**Idempotência:** cada passo primeiro procura pelo identificador de negócio (`indicator` do
resource, `name` da aplicação/papel, `username` do usuário) via `GET` e só cria o que não existir.
Rodar o script de novo contra uma base já provisionada não cria duplicata nem falha — apenas
confirma que cada item já existe e sai.

## Por que Node puro, sem dependências

O monorepo já exige Node 20+ e usa workspaces npm; um script sem `package.json`/`npm install`
próprio evita acoplar `infra/` ao lockfile das apps e roda igual no host (`node
infra/logto/bootstrap.mjs`) ou dentro de qualquer imagem `node:*` em CI. `fetch` é global desde o
Node 18, então nenhuma biblioteca HTTP é necessária.

## Reset completo

```bash
docker compose down -v   # remove o volume do Postgres — Logto E os schemas de negócio juntos
docker compose up -d postgres logto-db-init logto-seed logto
# repita o passo manual (a aplicação M2M some com o volume) e rode o bootstrap de novo
```
