# sbacars-example

Frontend foundation for the car sales platform described in [docs/vision.md](docs/vision.md).

## Structure

```text
apps/
  catalog/      # Public catalog (D01) + interest capture (D03)
  backoffice/   # Inventory (D02), leads (D03), purchase stub (D04)
packages/
  ui/           # Shared design system (tokens + primitives)
context/
  domain-map.md # Domain landscape (bounded contexts)
domains/
  catalogo-descoberta/   # D01
  estoque-curado/          # D02
  interesse-atendimento/   # D03
```

## Prerequisites

- Node.js 20+
- Docker (for local Keycloak OIDC in backoffice development)

## Setup

```bash
npm install
```

## Keycloak (backoffice OIDC)

The backoffice uses OIDC against a local Keycloak instance. Start it from the repository root:

```bash
docker compose up -d
```

Wait until Keycloak is ready at [http://localhost:8080](http://localhost:8080). The `sbacars` realm is imported from `infra/keycloak/sbacars-realm.json` on first startup (`start-dev --import-realm`). There is no persistent volume — `docker compose down` followed by `docker compose up -d` reimports the realm JSON.

| Account | Password | Role / notes |
|---|---|---|
| `admin` | `admin` | Keycloak master realm admin console |
| `ana` | `ana123` | Ana Operação (sbacars realm) |
| `bruno` | `bruno123` | Bruno Estoque (sbacars realm) |

OIDC client: `backoffice` (public, PKCE). Default authority: `http://localhost:8080/realms/sbacars`.

## Development

```bash
# Public catalog — http://localhost:5173
npm run dev:catalog

# Backoffice — http://localhost:5174
npm run dev:backoffice
```

Backoffice auth flow: open the app → **Sign in** → Keycloak login → redirect to `/auth/callback` → dashboard. **Sign out** in the header ends the Keycloak session and returns to `/login`.


## Quality checks

```bash
npm run typecheck
npm run test
npm run build
```

## Design system (Stitch)

Design tokens live in `packages/ui`. The Stitch Design System export failed (401 — no credentials). See [.stitch/designs/README.md](.stitch/designs/README.md) for how to re-export and replace inferred tokens.

## Documentation

| Artifact | Path |
|---|---|
| Vision | [docs/vision.md](docs/vision.md) |
| Domain map | [context/domain-map.md](context/domain-map.md) |
| D01 Catálogo e Descoberta | [domains/catalogo-descoberta/domain.md](domains/catalogo-descoberta/domain.md) |
| D02 Estoque Curado | [domains/estoque-curado/domain.md](domains/estoque-curado/domain.md) |
| D03 Interesse e Atendimento | [domains/interesse-atendimento/domain.md](domains/interesse-atendimento/domain.md) |
