# Especificação Técnica de Frontend — Gestão do Estoque Curado e Disponibilidade

> **PRD de origem:** `tasks/prd-gestao-do-estoque-curado-e-disponibilidade/prd.md`
> **API Contract:** `tasks/prd-gestao-do-estoque-curado-e-disponibilidade/api-contract.yaml` *(soberano)*
> **UX Spec:** `tasks/prd-gestao-do-estoque-curado-e-disponibilidade/ux-spec.md`
> **Telas geradas:** `tasks/prd-gestao-do-estoque-curado-e-disponibilidade/screens/*.html`
> **Sistema visual:** `DESIGN.md`
> **TechSpec backend:** `techspec.md` (Aprovada)
> **App:** `apps/backoffice`
> **Data:** 2026-08-16
> **Status:** Aprovado

---

## Resumo Executivo

O D02 é a **primeira feature do `apps/backoffice` a consumir API**. Hoje o app tem quatro páginas
que renderizam `EmptyState`, um `apiFetch` que só prefixa a base URL, e nenhuma biblioteca de
dados, formulário ou estado. A feature entra em `src/features/inventory`, sobre **TanStack Query**
(server state), **openapi-typescript** (tipos gerados do contrato) e **react-hook-form + zod**
(formulários).

Três princípios governam o desenho e explicam a maior parte das decisões:

1. **O servidor decide, a interface exibe.** Quatro campos existem no contrato exatamente para que
   o frontend não reimplemente regra de domínio: `disponibilidade.transicoesPermitidas` (quais
   botões existem), `elegibilidade.podeSolicitarElegibilidade` (se o botão primário habilita),
   `fatos.*.atendeTransparencia` (se o bloco está em falta) e `foraDoSla` (se o indicador fica
   vermelho). Nenhuma máquina de estados é replicada no cliente.
2. **A T03 é uma carga única.** Um `GET /ofertas/{id}` alimenta a tela e os três modais. Compor
   essa tela com várias chamadas seria contrariar o contrato, que a desenhou assim de propósito.
3. **O `409` de suspensão é fluxo, não erro.** A mutação o intercepta, abre o diálogo com
   `criteriosAfetados` e reexecuta com `confirmaSuspensao: true`.

**Trade-off primário:** escolhemos **tipos gerados com hooks à mão** em vez de cliente inteiramente
gerado (orval). Paga-se 17 funções de fetch escritas manualmente; ganha-se que os três pontos onde
o D02 foge do consumo uniforme — anexar o token, o `409` em duas fases e a união discriminada de
`AbrirSolicitacaoInput` — fiquem em código normal e legível, e não dentro da configuração de um
gerador.

---

## Skills de Referência

| Skill | Caminho | Decisões Influenciadas |
|---|---|---|
| `react-architecture` | `.claude/skills/react-architecture/SKILL.md` | Estrutura feature-based, `index.ts` como API pública, aliases, kebab-case em pastas |
| `react-code-quality` | `.claude/skills/react-code-quality/SKILL.md` | Estados de loading/error/empty obrigatórios, sem props drilling acima de 2 níveis, Hooks bem formados |
| `react-testing` | `.claude/skills/react-testing/SKILL.md` | Vitest + RTL + userEvent, MSW com reset por teste, `renderHook` para hooks |
| `react-observability` | `.claude/skills/react-observability/SKILL.md` | Spans de operação, propagação W3C já ativa, sanitização de dado sensível |
| `react-runtime-config` | `.claude/skills/react-runtime-config/SKILL.md` | `window.RUNTIME_ENV` para `API_BASE_URL`, sem rebuild entre ambientes |
| `react-production-readiness` | `.claude/skills/react-production-readiness/SKILL.md` | Gate antes de merge |

Nenhuma lacuna: a stack está coberta. As skills **não prescrevem** biblioteca de fetching, estado
ou formulário — daí as ADR-005, 006 e 007.

---

## Mapeamento User Story → Tela → Endpoint

| User Story (PRD) | Tela | Endpoints consumidos |
|---|---|---|
| Como Operador, quero cadastrar um veículo mesmo antes de completar as informações | T02 Cadastro | `cadastrarVeiculo`, `atualizarVeiculo`, `excluirOferta` |
| Como Operador, quero ver e triar o estoque | T01 Lista | `listarOfertas` |
| Como Operador, quero manter fatos, preço e disponibilidade | T03 Detalhe · T04 Fatos · M05 · M05-b · M06 | `obterOferta`, `substituirFatos`, `definirPrecoInicial`, `abrirSolicitacao`, `alterarDisponibilidade` |
| Como Operador, quero anexar evidência a um fato | T04 Fatos | `gerarUrlUploadEvidencia`, `gerarUrlDownloadEvidencia` |
| Como Operador, quero saber o que falta para publicar | T03 (card de critérios) | `obterOferta` |
| Como Responsável, quero revisar solicitações pendentes numa fila | T07 Fila | `listarSolicitacoes`, `contarSolicitacoesPendentes` |
| Como Responsável, quero decidir com contexto suficiente | T08 Detalhe · T08-b Rejeição | `obterSolicitacao`, `aprovarSolicitacao`, `rejeitarSolicitacao` |
| Como comprador, quero informações transparentes | — | Consumido por D01, fora deste app |
| Como D01, quero receber ofertas elegíveis | — | `listarOfertasElegiveis`, consumido por catalog-service |

**Cobertura:** 15 dos 17 endpoints do contrato são consumidos por este app. Os dois de fora —
`listarOfertasElegiveis` (client credentials, serviço a serviço) — não pertencem ao backoffice.

---

## Arquitetura de Frontend

### Estrutura de Pastas

Feature-based, como a `react-architecture` prescreve para domínios claros. Tudo novo vive em
`src/features/inventory`, exceto o que é genuinamente compartilhado.

```text
apps/backoffice/src/
├── shared/
│   ├── api/
│   │   ├── schema.d.ts          ← GERADO, não editar
│   │   ├── types.ts             ← aliases de domínio sobre schema.d.ts
│   │   ├── client.ts            ← MODIFICADO: passa a anexar Authorization
│   │   ├── problemDetails.ts    ← parse de RFC 9457 + code → mensagem
│   │   └── queryClient.ts       ← QueryClient configurado
│   ├── components/
│   │   ├── EmptyState.tsx       ← já existe
│   │   ├── DataTable.tsx        ← tabela densa, usada por T01 e T07
│   │   ├── ErrorState.tsx
│   │   └── TableSkeleton.tsx
│   ├── formatters/
│   │   ├── moeda.ts             ← centavos ⇄ BRL
│   │   ├── data.ts              ← ISO ⇄ DD/MM/AAAA
│   │   └── placa.ts
│   └── hooks/
│       └── useFiltrosNaUrl.ts   ← filtros ⇄ query string
├── features/inventory/
│   ├── index.ts                 ← API pública da feature
│   ├── api/                     ← queries, mutations e chaves
│   ├── components/              ← badges, checklist, cards
│   ├── pages/                   ← T01…T08
│   ├── schemas/                 ← zod dos formulários
│   └── validacao/               ← subárea do Responsável (T07, T08)
└── app/
    ├── router.tsx               ← MODIFICADO
    └── layouts/BackofficeLayout.tsx ← MODIFICADO
```

`DataTable`, `ErrorState` e os formatters vão para `shared/` porque a T07 já é o **segundo
consumidor real** — a mesma regra que o backend usa na §3.3 do plano de fundação. Nada sobe para
`shared/` com um consumidor só.

### Roteamento

| Rota | Página | Permissão |
|---|---|---|
| `/estoque` | T01 Lista do estoque | `estoque:ler` |
| `/estoque/novo` | T02 Cadastro | `estoque:gerenciar` |
| `/estoque/:ofertaId` | T03 Detalhe (hub) | `estoque:ler` |
| `/estoque/:ofertaId/editar` | T02 em modo edição | `estoque:gerenciar` |
| `/estoque/:ofertaId/fatos` | T04 Fatos conhecidos | `estoque:gerenciar` |
| `/validacao` | T07 Fila | `estoque:validar` |
| `/validacao/:solicitacaoId` | T08 Detalhe da solicitação | `estoque:validar` |

M05, M05-b e M06 são **modais sobre a T03**, não rotas — estado local da página.

`ProtectedRoute` já existe e cobre autenticação. Ganha uma prop `permissao` para cobrir
autorização; sem ela, a T07 abriria para quem não pode decidir. A ausência de permissão redireciona
para `/` com aviso, nunca mostra tela vazia sem explicação.

### Hierarquia de Componentes

```text
T03 DetalheOfertaPage
├── useOferta(ofertaId)                     ← única chamada de dados da tela
├── OfertaHeader        (badges + ações)
├── AlertaSuspensao     (só quando situacao === 'suspensa')
├── CardFatosConhecidos (3 × BlocoFatoResumo)
├── CardDadosVeiculo
├── CardPrecoOficial ───→ ModalPrecoInicial | ModalSolicitarPreco
├── CardDisponibilidade ─→ ModalDisponibilidade
├── ChecklistElegibilidade
└── CardPendencias
```

Componentes de apresentação **não fazem fetching**. Os modais recebem os dados por prop da página e
disparam mutations recebidas por prop — o que os torna testáveis isoladamente e evita que três
componentes disputem a mesma chave de cache.

---

## Geração de Tipos do API Contract

### Ferramenta Escolhida

**`openapi-typescript`** (ADR-006). Gera apenas tipos; os hooks são escritos à mão.

### Exemplo de Configuração

`apps/backoffice/package.json`:

```json
{
  "scripts": {
    "gen:api": "openapi-typescript ../../tasks/prd-gestao-do-estoque-curado-e-disponibilidade/api-contract.yaml -o src/shared/api/schema.d.ts",
    "gen:api:check": "npm run gen:api && git diff --exit-code src/shared/api/schema.d.ts"
  }
}
```

`gen:api:check` roda no CI: regenera e falha se o commitado divergir do contrato. É o que impede o
drift silencioso.

### Tipos Gerados Reutilizados

`src/shared/api/types.ts` dá nomes de domínio aos schemas:

```ts
import type { components } from './schema';

type S = components['schemas'];

export type OfertaResumo = S['OfertaResumo'];
export type OfertaDetalhe = S['OfertaDetalhe'];
export type SituacaoOferta = S['SituacaoOferta'];
export type EstadoDisponibilidade = S['EstadoDisponibilidade'];
export type TipoSolicitacao = S['TipoSolicitacao'];
export type CodigoCriterio = S['CodigoCriterio'];
export type SolicitacaoResumo = S['SolicitacaoResumo'];
export type SolicitacaoDetalhe = S['SolicitacaoDetalhe'];
export type VeiculoInput = S['VeiculoInput'];
export type FatosInput = S['FatosInput'];
export type ProblemaSuspensao = S['ProblemaSuspensao'];
```

Os enums viram uniões de string literal. Os mapas de badge usam `Record<SituacaoOferta, …>`
**sem índice de fallback**, de modo que um valor novo no contrato quebre o `tsc` em vez de
renderizar um badge sem cor.

---

## Estratégia de Fetching

### Biblioteca

**TanStack Query v5** (ADR-005). Sem biblioteca de client state.

### Padrões de Hook

Um módulo por recurso em `features/inventory/api/`:

```ts
// features/inventory/api/chaves.ts — hierarquia é o que torna a invalidação por prefixo correta
export const chaves = {
  ofertas: ['ofertas'] as const,
  lista: (filtros: FiltrosOferta) => ['ofertas', 'lista', filtros] as const,
  oferta: (id: string) => ['ofertas', id] as const,
  solicitacoes: ['solicitacoes'] as const,
  fila: (filtros: FiltrosFila) => ['solicitacoes', 'fila', filtros] as const,
  solicitacao: (id: string) => ['solicitacoes', id] as const,
  contagem: ['solicitacoes', 'pendentes', 'contagem'] as const,
};
```

```ts
// features/inventory/api/useAprovarSolicitacao.ts
export function useAprovarSolicitacao() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => aprovarSolicitacao(id),
    onSuccess: (_, id) => {
      qc.invalidateQueries({ queryKey: chaves.solicitacoes }); // fila + detalhe + contagem
      qc.invalidateQueries({ queryKey: chaves.ofertas });      // a oferta mudou de situação
    },
  });
}
```

### Estratégia de Cache e Invalidação

| Query | `staleTime` | Notas |
|---|---|---|
| `listarOfertas` | 30s | Filtros na URL fazem parte da chave |
| `obterOferta` | 30s | Carga única da T03 |
| `listarSolicitacoes` | **0** | Tela de decisão: dado velho aqui custa uma aprovação indevida |
| `obterSolicitacao` | **0** | idem |
| `contarSolicitacoesPendentes` | 30s | `refetchInterval: 60_000` conforme o `x-frontend-notes` |

| Mutação | Invalida |
|---|---|
| `cadastrarVeiculo` | `['ofertas']` |
| `atualizarVeiculo`, `substituirFatos`, `definirPrecoInicial`, `alterarDisponibilidade` | `['ofertas', id]` e `['ofertas']` |
| `abrirSolicitacao` | `['ofertas', id]`, `['solicitacoes']` |
| `aprovarSolicitacao`, `rejeitarSolicitacao` | `['solicitacoes']`, `['ofertas']` |

**Sem optimistic updates.** Toda mutação do D02 tem regra de servidor que pode recusá-la — critério
mínimo, transição inválida, pendência duplicada, auto-aprovação. Fingir sucesso e reverter seria
pior que esperar: o operador veria a oferta virar elegível e voltar atrás sozinha.

### Tratamento Centralizado de Erros

Todo erro do backend é RFC 9457. `shared/api/problemDetails.ts` normaliza e mapeia:

```ts
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly problem: ProblemDetails,
  ) { super(problem.detail ?? problem.title); }
}
```

| Status | Tratamento na UI |
|---|---|
| `400`, `422` | Mensagem do `detail` junto ao formulário — é regra de negócio, texto já legível |
| `401` | `signinRedirect()` do `react-oidc-context` |
| `403` | Aviso explicando a permissão faltante; no caso de auto-aprovação (DUX-08), a mensagem específica do `detail` |
| `404` | Estado "não encontrada" na própria página |
| `409` com `codigo: 'suspensao-nao-confirmada'` | **Não é erro** — abre o diálogo de confirmação. Ver abaixo |
| `409` demais | Mensagem do `detail`; a UI já deveria ter prevenido (botão desabilitado) |
| `413`, `415` | Erro junto à zona de upload |
| `500` | Mensagem genérica **com o `traceId` visível**, para o operador citar no chamado |

O `traceId` na tela de erro de 500 é deliberado: o `GlobalExceptionHandler` do backend grava o
mesmo valor no log, e é o que liga um chamado de suporte a uma linha de log.

**O protocolo de suspensão** (ADR-003) vive num hook próprio, usado por T02 e T04:

```ts
// useMutacaoComSuspensao — chama, intercepta o 409 e devolve o que o diálogo precisa
const { mutate, suspensaoPendente, confirmarSuspensao, cancelar } = useMutacaoComSuspensao(
  useSubstituirFatos(ofertaId),
);
// suspensaoPendente: { criteriosAfetados: CodigoCriterio[] } | null
```

Concentrar isso num hook impede que dois formulários implementem o mesmo fluxo de forma diferente.

---

## Gerenciamento de Estado

### Server State

TanStack Query, integralmente. Nenhum dado de servidor é copiado para `useState`.

### Client State

Nenhuma biblioteca. Três categorias, três destinos:

| Estado | Onde vive | Por quê |
|---|---|---|
| Filtros de T01 e T07 | **Query string da URL** (`useFiltrosNaUrl`) | Torna a triagem do operador compartilhável e sobrevive ao refresh — é dado de navegação |
| Abertura de modal, aba ativa, diálogo de suspensão | `useState` da página | Escopo de um componente; nada mais precisa saber |
| Estado de formulário | react-hook-form | Já é a responsabilidade dele |

Um store global não teria conteúdo: não há dado de cliente compartilhado entre telas distantes.

---

## Validação de Formulários

### Biblioteca

**react-hook-form + zod** via `@hookform/resolvers` (ADR-007).

### Sincronização com o Contract

Os schemas são escritos à mão, mas **checados contra o tipo gerado** em tempo de compilação:

```ts
type _CheckVeiculo = z.infer<typeof veiculoSchema> extends VeiculoInput ? true : never;
type _CheckFatos = z.infer<typeof fatosSchema> extends FatosInput ? true : never;
```

Restrições numéricas do contrato (`maxLength`, `minimum`) **não aparecem** no tipo gerado. Elas
ficam em `features/inventory/schemas/limites.ts`, cada constante comentada com a origem no
contrato. É mitigação por processo, e está registrada como risco assumido no ADR-007.

### Exemplo de Schema

A regra da T04 — a mais difícil do D02 — escrita uma vez e reusada nos três blocos:

```ts
const blocoFatoSchema = z
  .object({
    indisponivel: z.boolean(),
    descricao: z.string().max(LIMITES.descricao).optional(),
    fonte: z.string().max(LIMITES.fonte).optional(),
    evidenciaId: z.string().uuid().nullable().optional(),
    limitacaoDeclarada: z.string().max(LIMITES.limitacao).optional(),
  })
  .refine((b) => !b.indisponivel || !!b.limitacaoDeclarada?.trim(), {
    message: 'Declare a limitação quando a informação estiver indisponível.',
    path: ['limitacaoDeclarada'],
  });

export const fatosSchema = z.object({
  origem: blocoFatoSchema,
  condicao: blocoFatoSchema,
  historico: blocoFatoSchema,
});
```

**A T02 valida formato, nunca presença.** Salvar com campos vazios é o caminho feliz do RF-01, e
um teste cobre exatamente isso — é o comportamento mais fácil de quebrar sem querer ao "melhorar"
a validação.

---

## Mocks e Ambiente de Desenvolvimento

### Estratégia

Dois mocks, com propósitos diferentes:

| Ferramenta | Onde | Para quê |
|---|---|---|
| **Prism** | `npm run dev` | Sobe o contrato como servidor. Permite construir todas as telas antes de o backend existir — e ele **não existe** até a V-01 da TechSpec de backend |
| **MSW** | Vitest | Handlers por teste, com reset entre eles, como a `react-testing` exige |

Os handlers do MSW derivam dos `examples` do contrato, não de dados inventados: os exemplos já são
realistas (Honda Civic EXL, `R$ 87.900,00`, Campinas/SP) e mantêm teste e contrato alinhados.

### Comandos

```bash
# tipos a partir do contrato
npm run gen:api -w @sbacars/backoffice

# mock server do contrato (terminal separado)
npx @stoplight/prism-cli mock tasks/prd-gestao-do-estoque-curado-e-disponibilidade/api-contract.yaml --port 4010

# app apontando para o mock
API_BASE_URL=http://localhost:4010 npm run dev:backoffice
```

### Configuração de MSW

`src/test/msw/` com `server.ts` (setup do `setupServer`) e `handlers/` por recurso. O
`src/test/setup.ts` existente ganha `beforeAll(listen)`, `afterEach(resetHandlers)` e
`afterAll(close)`.

---

## Inventário de Artefatos

### Arquivos a Criar

**Base compartilhada**

| Caminho | Tipo | Skills | Descrição |
|---|---|---|---|
| `src/shared/api/schema.d.ts` | Type (gerado) | `react-architecture` | Tipos do contrato. Nunca editar |
| `src/shared/api/types.ts` | Type | `react-code-quality` | Aliases de domínio sobre `schema.d.ts` |
| `src/shared/api/problemDetails.ts` | Service | `react-code-quality` | `ApiError`, parse de RFC 9457, mapa status → tratamento |
| `src/shared/api/queryClient.ts` | Config | `react-architecture` | `QueryClient` com `staleTime` e `retry` padrão |
| `src/shared/components/DataTable.tsx` | Component | `react-code-quality` | Tabela densa: zebra, bordas horizontais, `data-tabular` |
| `src/shared/components/ErrorState.tsx` | Component | `react-code-quality` | Erro com retry e `traceId` quando 500 |
| `src/shared/components/TableSkeleton.tsx` | Component | `react-code-quality` | Skeleton de 6 linhas |
| `src/shared/formatters/moeda.ts` | Util | `react-code-quality` | Centavos ⇄ BRL, o único lugar que converte |
| `src/shared/formatters/data.ts` | Util | `react-code-quality` | ISO ⇄ DD/MM/AAAA, "há 4h" |
| `src/shared/formatters/placa.ts` | Util | `react-code-quality` | Máscara e normalização |
| `src/shared/hooks/useFiltrosNaUrl.ts` | Hook | `react-architecture` | Filtros ⇄ query string |

**Feature inventory — API**

| Caminho | Tipo | Skills | Descrição |
|---|---|---|---|
| `src/features/inventory/api/chaves.ts` | Service | `react-architecture` | Chaves hierárquicas de query |
| `src/features/inventory/api/ofertas.ts` | Service | `react-code-quality` | 8 funções de fetch tipadas |
| `src/features/inventory/api/solicitacoes.ts` | Service | `react-code-quality` | 5 funções de fetch tipadas |
| `src/features/inventory/api/evidencias.ts` | Service | `react-code-quality` | Upload em 4 passos contra o S3 |
| `src/features/inventory/api/useOfertas.ts` | Hook | `react-code-quality` | Queries de lista e detalhe |
| `src/features/inventory/api/useMutacoesOferta.ts` | Hook | `react-code-quality` | Cadastrar, atualizar, fatos, preço, disponibilidade |
| `src/features/inventory/api/useSolicitacoes.ts` | Hook | `react-code-quality` | Fila, contagem, detalhe |
| `src/features/inventory/api/useDecisao.ts` | Hook | `react-code-quality` | Aprovar e rejeitar, com invalidação cruzada |
| `src/features/inventory/api/useMutacaoComSuspensao.ts` | Hook | `react-code-quality` | Protocolo de duas fases do ADR-003 |

**Feature inventory — Componentes**

| Caminho | Tipo | Skills | Descrição |
|---|---|---|---|
| `src/features/inventory/components/BadgeSituacao.tsx` | Component | `react-code-quality` | 4 estados, `Record` exaustivo |
| `.../BadgeDisponibilidade.tsx` | Component | `react-code-quality` | 3 estados |
| `.../BadgeTipoSolicitacao.tsx` | Component | `react-code-quality` | 4 tipos |
| `.../ValorComProcedencia.tsx` | Component | `react-code-quality` | Valor + "Atualizado em X por Y" (DUX-06) |
| `.../ChecklistElegibilidade.tsx` | Component | `react-code-quality` | CM-1…CM-6, usado em T03 e T08 |
| `.../SeloLimitacao.tsx` | Component | `react-code-quality` | Marca limitação declarada |
| `.../IndicadorSla.tsx` | Component | `react-code-quality` | Consome `foraDoSla` do servidor |
| `.../UploadEvidencia.tsx` | Component | `react-code-quality` | Zona de upload, progresso via XHR, chip do anexo |
| `.../DialogoSuspensao.tsx` | Component | `react-code-quality` | Diálogo do 409, lista `criteriosAfetados` |
| `.../BlocoFatoForm.tsx` | Component | `react-code-quality` | Um bloco, reusado 3× na T04 |

**Feature inventory — Páginas e schemas**

| Caminho | Tipo | Skills | Descrição |
|---|---|---|---|
| `src/features/inventory/pages/ListaEstoquePage.tsx` | Page | `react-architecture` | T01 |
| `.../pages/CadastroVeiculoPage.tsx` | Page | `react-architecture` | T02, criação e edição |
| `.../pages/DetalheOfertaPage.tsx` | Page | `react-architecture` | T03 — hub, carga única |
| `.../pages/FatosConhecidosPage.tsx` | Page | `react-architecture` | T04 |
| `.../components/ModalPrecoInicial.tsx` | Component | `react-code-quality` | M05-b |
| `.../components/ModalSolicitarPreco.tsx` | Component | `react-code-quality` | M05 |
| `.../components/ModalDisponibilidade.tsx` | Component | `react-code-quality` | M06 |
| `.../validacao/pages/FilaValidacaoPage.tsx` | Page | `react-architecture` | T07 |
| `.../validacao/pages/DetalheSolicitacaoPage.tsx` | Page | `react-architecture` | T08 e T08-b |
| `.../schemas/veiculo.ts` | Schema | `react-code-quality` | zod da T02 — formato, nunca presença |
| `.../schemas/fatos.ts` | Schema | `react-code-quality` | zod da T04, com o `refine` condicional |
| `.../schemas/solicitacao.ts` | Schema | `react-code-quality` | zod dos modais |
| `.../schemas/limites.ts` | Config | `react-code-quality` | Constantes espelhando o contrato |
| `src/features/inventory/index.ts` | Barrel | `react-architecture` | API pública da feature |

**Testes e mocks**

| Caminho | Tipo | Skills | Descrição |
|---|---|---|---|
| `src/test/msw/server.ts` | Mock | `react-testing` | `setupServer` |
| `src/test/msw/handlers/*.ts` | Mock | `react-testing` | Handlers derivados dos `examples` do contrato |
| `src/features/inventory/**/*.test.tsx` | Test | `react-testing` | Integração por tela |
| `src/features/inventory/api/*.test.ts` | Test | `react-testing` | Hooks com `renderHook` |
| `src/shared/formatters/*.test.ts` | Test | `react-testing` | Unitários puros |

### Arquivos a Modificar

| Caminho | Skills | Alteração |
|---|---|---|
| `src/shared/api/client.ts` | `react-code-quality` | Anexar `Authorization` com o access token; lançar `ApiError` a partir do corpo RFC 9457 |
| `src/app/router.tsx` | `react-architecture` | Rotas `/estoque/*` e `/validacao/*`; remover `/inventory` (AJ-03) |
| `src/app/layouts/BackofficeLayout.tsx` | `react-architecture` | PT-BR (AJ-01), item `Validação` com badge (AJ-02), esconder por permissão (AJ-08) |
| `src/features/auth/components/ProtectedRoute.tsx` | `react-code-quality` | Prop `permissao` para autorização por rota |
| `src/features/auth/config/oidcConfig.ts` | `react-runtime-config` | `estoque:validar` em `API_SCOPES` (AJ-07) |
| `src/main.tsx` | `react-architecture` | `QueryClientProvider` acima do `AppAuthProvider` |
| `src/test/setup.ts` | `react-testing` | Ciclo de vida do MSW |
| `apps/backoffice/package.json` | `react-architecture` | Deps novas + `gen:api` e `gen:api:check` |
| `packages/ui/src/tokens/tokens.css` | — | Tokens do `DESIGN.md` (AJ-04) |
| `packages/ui/tailwind.preset.ts` | — | Escala tipográfica, `data-tabular`, `label-caps` (AJ-04) |
| `packages/ui/src/components/button/Button.tsx` | `react-code-quality` | `primary` vira laranja sólido, `secondary` vira outline navy, e falta a variante `danger` (T08 rejeitar) |
| `.stitch/metadata.json` | — | `tokensSource` deixa de ser `inferred-minimal` (AJ-05) |

### Arquivos de Referência (não alterar)

| Caminho | Motivo |
|---|---|
| `tasks/.../api-contract.yaml` | Fonte de verdade de tipos, erros e `x-frontend-notes` |
| `tasks/.../ux-spec.md` | Estados, regras de habilitação, DUX-01…DUX-08 |
| `tasks/.../screens/*.html` | Referência visual das 10 telas geradas |
| `DESIGN.md` | Paleta, tipografia, elevação, forma |
| `src/telemetry/index.ts` | Já instrumenta `fetch`; nada a fazer para o `traceparent` |
| `packages/ui/src/components/` | `Button`, `Input`, `Card` disponíveis |

**Dependências novas:** `@tanstack/react-query`, `react-hook-form`, `zod`, `@hookform/resolvers`
(runtime); `openapi-typescript`, `msw` (dev).

---

## Acessibilidade

- Todo campo tem `<label>` associado; o rótulo em CAIXA ALTA do `DESIGN.md` é estilo, não conteúdo.
- Os badges **nunca comunicam só por cor**: cada um carrega texto ("Elegível", "Suspensa"). O
  checklist usa ✓/✗ além de verde/vermelho, e o `IndicadorSla` acompanha o vermelho de um ícone.
- Modais com `role="dialog"`, `aria-modal`, foco preso e `Esc` para fechar; o foco volta ao botão
  que abriu.
- O diálogo de suspensão tem `aria-describedby` apontando para a lista de critérios afetados — é a
  informação que justifica a decisão.
- Erros de formulário associados por `aria-describedby` e anunciados em `aria-live="polite"`.
- Contraste: `#78767c` sobre `#ffffff` dá 4.6:1, acima do mínimo AA para texto pequeno.
- Tabelas com `<caption>` visualmente oculto e `scope` nos cabeçalhos.

## Internacionalização

Não se aplica na Fase 1. UI só em PT-BR (DUX-01), operação nacional. Nenhuma biblioteca de i18n é
introduzida — YAGNI. Os textos ficam em JSX, não num arquivo de mensagens, porque um arquivo de
mensagens sem segundo idioma é indireção sem benefício.

---

## Análise de Impacto

| Componente | Tipo | Descrição e risco | Ação |
|---|---|---|---|
| `apps/backoffice` | modificado | Deixa de ser esqueleto e ganha 4 dependências de runtime. Risco **baixo**: nada em produção depende dele |
| `shared/api/client.ts` | modificado | Passa a anexar token e lançar `ApiError`. Risco **médio**: é o ponto único de rede | Testes de unidade do cliente |
| `packages/ui` | modificado | Tokens trocados e `Button` alterado. Risco **médio**: afeta também o `apps/catalog` | Verificar o catalog após AJ-04 |
| `ProtectedRoute` | modificado | Ganha autorização além de autenticação. Risco **baixo** | Teste por rota |
| `apps/catalog` | **afetado indiretamente** | Herda os tokens novos do `packages/ui`. Risco **médio**: ele tem telas próprias | Revisão visual após AJ-04 |
| TanStack Query | novo padrão | Vira precedente para o `catalog`, que é público e anônimo, com necessidades diferentes | ADR-005 registra que a decisão deve ser revisitada lá |

---

## Abordagem de Testes

### Testes Unitários

Vitest + RTL, já configurados. Alvos: formatters (`moeda`, `data`, `placa`), schemas zod e
componentes de apresentação sem fetching.

Casos que merecem teste nominal:

- `moeda`: `8790000` ⇄ `R$ 87.900,00`; string vazia; valor zero
- `fatosSchema`: bloco `indisponivel` sem limitação **falha**; com limitação **passa**; bloco vazio e sem limitação **passa no schema** (o CM-6 é regra de servidor, não de formulário)
- `veiculoSchema`: **todos os campos vazios passam** — é o RF-01 e o mais fácil de quebrar sem querer
- `BadgeSituacao`: os 4 estados renderizam texto além de cor

### Testes de Integração

RTL + MSW, com handlers derivados dos `examples` do contrato. Por tela, cobrindo os quatro estados
que a `react-code-quality` exige (loading, error, empty, sucesso).

Fluxos que precisam de teste de integração:

| Fluxo | Por quê |
|---|---|
| T04: salvar fatos → `409` → diálogo → confirmar → `200` | É o protocolo do ADR-003; falha silenciosa aqui suspende ofertas sem aviso |
| T08: aprovar → fila **e** badge da sidebar atualizam | É o modo de falha típico de invalidação incompleta (risco do ADR-005) |
| T03: botão "Solicitar elegibilidade" desabilitado quando `podeSolicitarElegibilidade` é `false` | Garante que a UI consome a decisão do servidor em vez de recalcular |
| T08: botões desabilitados quando `podeDecidir` é `false` | DUX-08 no cliente; o 403 do servidor é a rede |
| T02: submit com formulário quase vazio **cria** a oferta | RF-01 |
| T01: filtros vão para a URL e sobrevivem ao reload | Decisão de client state |

### Testes E2E

Fora do escopo da Fase 1 deste PRD. Playwright não está configurado no repositório, e introduzi-lo
junto com quatro dependências novas espalha o risco. Registrado como questão em aberto (QF-03).

### Testes de Contrato

O `gen:api:check` no CI é o teste de contrato do frontend: se o `api-contract.yaml` mudar e o
`schema.d.ts` não for regenerado, o build falha. Divergência de tipo vira erro de compilação, não
de runtime.

---

## Sequenciamento de Desenvolvimento

### Build Order

1. **Base de dados e tipos** — `gen:api`, `types.ts`, `problemDetails.ts`, `queryClient.ts`,
   `client.ts` com token. Sem dependências. Evidência: um `useQuery` de teste bate no Prism.
2. **AJ-01…AJ-03 — shell** — PT-BR, item `Validação`, rotas. Depende de nada. Evidência: navegação
   em PT-BR com as rotas novas.
3. **AJ-04 — tokens do `DESIGN.md`** — em paralelo com 1 e 2. Evidência: o `Button` primário sai
   laranja e o fundo vira `#f9f9ff`.
4. **Compartilhados** — `DataTable`, `ErrorState`, `TableSkeleton`, formatters. Depende de 3.
5. **T01 Lista** — depende de 1 e 4. Evidência: lista contra o Prism, com filtros na URL.
6. **T02 Cadastro** — depende de 1, 4 e dos schemas zod. Evidência: salvar quase vazio cria.
7. **T03 Detalhe** — depende de 5. Evidência: checklist e badges corretos; ações habilitadas pelo servidor.
8. **T04 Fatos** *(sem upload)* — depende de 7. Evidência: protocolo de suspensão ponta a ponta.
9. **M05, M05-b, M06** — depende de 7. Evidência: mutações refletem na T03 sem reload.
10. **AJ-07/AJ-08 + T07 Fila** — depende de 4. Evidência: fila com SLA e badge da sidebar.
11. **T08 e T08-b** — depende de 10. Evidência: aprovar atualiza fila e badge juntos.
12. **Upload de evidência na T04** — depende de 8. Evidência: upload direto do browser para o S3.

Os passos 1 a 11 rodam **inteiramente contra o Prism**, sem depender de uma linha do backend. É o
principal benefício de o contrato ter vindo antes. O passo 12 precisa do S3 real — o Prism não
assina URL — mas a fundação (Fase C) está concluída antes do início do backend (ADR-008).

### Dependências Técnicas Bloqueantes

| Dependência | Bloqueia | Estado |
|---|---|---|
| **AJ-04** — tokens do `DESIGN.md` em `packages/ui` | Fidelidade visual de tudo | ⬜ pendente |
| **Logto** — scope `estoque:validar` (AP-03) | T07 e T08 com token real | ⬜ a configurar |
| **Backend V-11** | Passo 12 com S3 real | ⬜ pendente |

Nada bloqueia o início: o passo 1 pode começar hoje contra o contrato. Os passos 1 a 11 não
dependem do backend em momento algum.

---

## Performance

- **`DataTable` sem virtualização.** A paginação do contrato limita a 100 itens por página, e a
  padrão é 20. Virtualizar 20 linhas é otimização sem problema.
- **T03 numa chamada só**, como o contrato desenhou. Nenhum componente filho busca dado próprio.
- **Sem prefetch especulativo.** A navegação do operador não é previsível o suficiente para
  compensar o tráfego.
- **Upload direto para o S3**, sem passar pela API — o binário nunca entra no bundle nem na memória
  do serviço. Progresso via XHR, porque `fetch` não expõe progresso de upload.
- **Code splitting por rota** com `React.lazy`, separando a subárea de validação: quem só opera o
  estoque não baixa as telas do Responsável.
- **`refetchInterval` de 60s** apenas na contagem do badge. Nenhuma outra query faz polling.

---

## Considerações Técnicas

### Decisões Principais

- **TanStack Query** (ADR-005) — o D02 tem invalidação cruzada em quase toda mutação; trade-off:
  +13kb e uma API nova, e vira precedente para o `catalog`, que tem necessidades diferentes.
- **openapi-typescript com hooks à mão** (ADR-006) — os três pontos fora do padrão (token, `409`
  em duas fases, união discriminada) ficam em código legível; trade-off: 17 funções de fetch
  escritas manualmente.
- **react-hook-form + zod** (ADR-007) — a regra condicional da T04 escrita uma vez; trade-off:
  schemas à mão podem divergir do contrato em restrições não tipadas.
- **Sem biblioteca de client state** — filtros na URL, resto local. Um store não teria conteúdo.
- **Sem optimistic updates** — toda mutação pode ser recusada por regra de servidor.

### Riscos Conhecidos

| Risco | Prob. | Mitigação |
|---|---|---|
| Invalidação incompleta após mutação (fila atualiza, badge não) | Média | Teste de integração específico; lista de invalidações revisada no code review |
| Schemas zod divergirem do contrato em `maxLength`/`pattern` | Média | Constantes em `limites.ts` comentadas com a origem; checagem de tipo cobre a estrutura |
| AJ-04 quebrar o visual do `apps/catalog` | Média | Revisão visual do catalog na mesma alteração |
| Regenerar tipos e não ajustar os hooks | Baixa | `tsc --noEmit` já roda no `build`; `gen:api:check` no CI |
| Diferença entre o HTML do Stitch e o componente React virar drift | Média | O HTML é referência visual, não fonte; a fonte é o `ux-spec.md` |

### Conformidade com Skills

- Estrutura feature-based com `index.ts` público e aliases — `react-architecture`
- Loading, error e empty tratados em toda tela — `react-code-quality`
- Componentes de apresentação sem fetching; sem props drilling acima de 2 níveis — `react-code-quality`
- Vitest + RTL + userEvent; MSW com reset por teste; `renderHook` para hooks — `react-testing`
- `window.RUNTIME_ENV` para `API_BASE_URL`, sem rebuild entre ambientes — `react-runtime-config`
- Telemetria já ativa; `traceparent` sai sem configuração extra — `react-observability`

**Desvios:** nenhum.

---

## Questões em Aberto

- [ ] **QF-01 — O HTML da T03 não cobre o estado "sem preço vigente".** A decisão QT-01 da TechSpec
  de backend criou dois estados para o card de preço, e o `t03-detalhe-oferta.html` gerado só tem
  o estado com valor. O brief do M05-b já está no `stitch-briefs.md` §8b. Registrado como AJ-09.
- [ ] **QF-02 — `estoque:ler` basta para a T01 e a T03?** O contrato exige `estoque:ler` na
  leitura e `estoque:gerenciar` na escrita. Um usuário só com `estoque:ler` veria a T03 com todos
  os botões de ação desabilitados. Confirmar se esse perfil existe na operação ou se todo operador
  tem as duas.
- [ ] **QF-03 — E2E fica para depois?** Playwright não está configurado. Proposta: deixar para
  depois da primeira entrega, quando as telas estabilizarem, em vez de introduzi-lo junto com
  quatro dependências novas.
- [ ] **QF-04 — Quem executa o AJ-04?** A troca de tokens afeta `apps/catalog` também. Fazer junto
  com o D02 ou como alteração separada, com revisão visual dos dois apps?

---

## Architecture Decision Records

### Herdadas (backend)

- [ADR-001: CQRS nativo, sem MediatR](adrs/adr-001.md) — padrão de caso de uso do D02
- [ADR-002: Oferta e Solicitacao como agregados separados](adrs/adr-002.md) — fronteiras de consistência
- [ADR-003: Suspensão de elegibilidade confirmada em duas fases](adrs/adr-003.md) — **o frontend implementa a outra ponta deste protocolo**
- [ADR-004: Eventos de integração só depois do outbox](adrs/adr-004.md) — substituída pela ADR-008; sem efeito no frontend
- [ADR-008: Fundação completa como pré-requisito do backend do D02](adrs/adr-008.md) — remove a Fase C como bloqueio do passo 12

### Criadas nesta sessão (frontend)

- [ADR-005: TanStack Query como camada de server state](adrs/adr-005.md) — invalidação cruzada é o padrão dominante do D02
- [ADR-006: Tipos gerados com openapi-typescript, hooks à mão](adrs/adr-006.md) — os três pontos fora do padrão ficam em código legível
- [ADR-007: react-hook-form + zod](adrs/adr-007.md) — a regra condicional da T04 escrita uma vez

---

## Próximos Passos

1. **Aprovar esta TechSpec** — promove o arquivo e as ADRs 005–007 para `Accepted`.
2. **Tasks:** `tsg-flow-task-creator` referenciando `techspec.md` e `frontend-techspec.md`.
3. **Começar pelo passo 1 do Build Order** — ele não depende de nada, nem do backend.
