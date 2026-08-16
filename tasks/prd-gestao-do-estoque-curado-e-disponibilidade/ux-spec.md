# UX Spec — Gestão do Estoque Curado e Disponibilidade

> **Etapa 1 do planejamento.** Define o *contrato de tela* do D02: inventário, estados,
> campos, ações e transições. Alimenta os briefs do Stitch (`stitch-briefs.md`) e o
> API Contract (`api-contract.yaml`).
>
> **Fonte:** `prd.md` (RF-01 a RF-06) · `domains/estoque-curado/domain.md` (RN-01 a RN-10)
> **App de destino:** `apps/backoffice` — feature `inventory`
> **Idioma da UI:** PT-BR
> **Última revisão:** 2026-08-16

---

## 1. Escopo

Todas as telas deste documento pertencem ao **backoffice da Operação central**. A
experiência pública do comprador é de D01 (`apps/catalog`) e está fora deste PRD.

Dois papéis operam o domínio:

| Papel | Código | O que faz |
|---|---|---|
| Operador de estoque | `operador` | Cadastra veículos, mantém fatos, preço e disponibilidade, abre solicitações. |
| Responsável de validação | `responsavel` | Aprova ou rejeita solicitações de elegibilidade, preço, retirada e reversão de venda. |

---

## 2. Decisões de UX

| ID | Decisão | Motivo |
|---|---|---|
| DUX-01 | Rótulos em PT-BR, alinhados aos Termos Canônicos do PRD. O shell existente (`BackofficeLayout`) é traduzido de EN para PT-BR. | O usuário é a Operação central brasileira; drift entre doc e tela custa mais que 6 strings. |
| DUX-02 | `Validação` é item próprio na sidebar, com badge de pendências. | É outro papel e outro fluxo de trabalho; permite esconder o item por permissão. |
| DUX-03 | **Solicitação pendente é uma entidade única com campo `tipo`** (4 valores), não quatro fluxos separados. | Uma fila, uma tela de detalhe, um ciclo aprovar/rejeitar. Evita quadruplicar telas e endpoints. |
| DUX-04 | O **Detalhe da oferta (T03) é o hub**: consolida fatos, preço, disponibilidade, elegibilidade e pendências abertas. Preço e disponibilidade são modais sobre ele. | O operador trabalha uma oferta por vez; navegar entre 5 páginas para completar um cadastro é atrito. |
| DUX-05 | O **checklist de critérios mínimos fica visível e permanente** no T03, com o que falta destacado. | RF-06 e o objetivo de 100% de conformidade dependem do operador saber o que falta *antes* de solicitar. |
| DUX-06 | Toda tela que exibe um valor sob curadoria mostra **valor + data de atualização + responsável**. | Requisito explícito da seção "Experiência do Usuário" do PRD. |
| DUX-07 | Enquanto houver pendência aberta de um tipo, **nova solicitação do mesmo tipo é bloqueada** na UI (botão desabilitado + motivo). | Evita fila ambígua e disputa entre duas propostas do mesmo campo. |
| DUX-08 | O `responsavel` **não aprova a própria solicitação**; o botão aparece desabilitado com o motivo. | Segregação de função. DP-02 cria a validação justamente para haver um segundo par de olhos. |

---

## 3. Modelo de estados

### 3.1 Situação da oferta

Quatro estados. `suspensa` não está nomeada na tabela do PRD, mas é exigida pelo
terceiro critério do RF-03.

```
                  solicitação de elegibilidade aprovada
   ┌────────────────┐ ────────────────────────────────► ┌──────────┐
   │ em-preparacao  │                                   │ elegivel │
   └────────────────┘ ◄──────────┐                      └──────────┘
                                 │                          │    │
                    aprovada     │      alteração quebra    │    │ solicitação de
                                 │      critério mínimo     │    │ retirada aprovada
                            ┌──────────┐ ◄────────────────┘    │
                            │ suspensa │                        ▼
                            └──────────┘                  ┌──────────┐
                                 │  solicitação de        │ retirada │
                                 └── retirada aprovada ──►└──────────┘
```

| Estado | Rótulo na UI | Elegível para D01? | Como sai |
|---|---|---|---|
| `em-preparacao` | Em preparação | Não | Solicitação de elegibilidade aprovada |
| `elegivel` | Elegível | **Sim** | Retirada aprovada, ou suspensão automática |
| `suspensa` | Suspensa | Não | Correção + nova solicitação de elegibilidade |
| `retirada` | Retirada | Não | Nova solicitação de elegibilidade aprovada (QA-01) |

**Suspensão é automática, não passa pela fila.** Quando uma edição faz a oferta deixar
de cumprir um critério mínimo, ela cai para `suspensa` na hora (RF-03). Voltar exige
validação.

### 3.2 Disponibilidade operacional

Independente da situação da oferta (RN-05). Uma oferta `retirada` conserva sua
disponibilidade.

```
   ┌────────────┐  registrar reserva   ┌───────────┐  concluir venda  ┌─────────┐
   │ disponivel │ ───────────────────► │ reservado │ ───────────────► │ vendido │
   └────────────┘ ◄─────────────────── └───────────┘                  └─────────┘
         ▲          encerrar reserva                                       │
         │                                                                 │
         └───────── reversão de venda (EXIGE VALIDAÇÃO) ───────────────────┘
```

| Transição | Validação? | Origem |
|---|---|---|
| `disponivel` → `reservado` | Não — ação direta do operador | RF-05 |
| `reservado` → `disponivel` | Não — exige ação explícita, nunca expira sozinha (DP-04) | RF-05, DP-04 |
| `reservado` → `vendido` | Não — ação direta do operador | RF-05 |
| `disponivel` → `vendido` | Não — venda direta, sem reserva prévia | QA-02 |
| `vendido` → `disponivel` | **Sim** — solicitação tipo `reversao-venda` | RF-05 |

Agendar test drive (D03) **não** altera disponibilidade (RN-08). A UI não expõe
nenhuma ação de D03 aqui.

### 3.3 Solicitação pendente

```
   ┌──────────┐ ──── aprovar ────► ┌──────────┐   aplica a alteração
   │ pendente │                    │ aprovada │
   └──────────┘ ──── rejeitar ───► ┌──────────┐   estado vigente permanece
                (justificativa      │rejeitada │   + motivo volta ao operador
                 obrigatória)       └──────────┘
```

Quatro tipos:

| `tipo` | Rótulo | O que altera ao aprovar | Origem |
|---|---|---|---|
| `elegibilidade` | Elegibilidade | Situação → `elegivel` | RF-01, RF-06 |
| `preco` | Preço oficial | Preço vigente → novo valor | RF-04 |
| `retirada` | Retirada da oferta | Situação → `retirada`; **não** toca a disponibilidade | RF-02, RN-05 |
| `reversao-venda` | Reversão de venda | Disponibilidade `vendido` → `disponivel` | RF-05 |

**Enquanto pendente, o estado vigente continua valendo** (RF-04) — a UI mostra o valor
vigente como principal e a proposta como anotação secundária.

### 3.4 Critérios mínimos de elegibilidade

Checklist do DUX-05. Deriva do §4 do domain doc + RF-06.

| # | Critério | Satisfeito quando |
|---|---|---|
| CM-1 | Identificação | Placa preenchida (chassi opcional) |
| CM-2 | Dados básicos | Marca, modelo, versão, ano, quilometragem e câmbio preenchidos |
| CM-3 | Localização | Cidade e UF preenchidas |
| CM-4 | Preço oficial | Existe preço vigente aprovado |
| CM-5 | Disponibilidade conhecida | Estado de disponibilidade definido |
| CM-6 | Transparência dos fatos | Cada bloco (origem, condição, histórico) está **preenchido** ou tem **limitação declarada** |

CM-6 é o que operacionaliza o DP-03 e o RN-03: ausência de dado não bloqueia, ausência
de *declaração* bloqueia. Certificação formal nunca é exigida (RN-09).

---

## 4. Navegação e rotas

```
Sidebar                    Rota                          Papel
─────────────────────────────────────────────────────────────────────
Painel                     /                             ambos
Estoque                    /estoque                      operador
  └ detalhe                /estoque/:ofertaId            ambos
  └ novo veículo           /estoque/novo                 operador
  └ editar veículo         /estoque/:ofertaId/editar     operador
  └ fatos conhecidos       /estoque/:ofertaId/fatos      operador
Validação            (N)   /validacao                    responsavel
  └ detalhe                /validacao/:solicitacaoId     responsavel
Interesses                 /leads                        (D03, fora do escopo)
Compras                    /purchases                    (D04, fora do escopo)
```

O badge `(N)` na sidebar mostra a contagem de solicitações `pendente`.

**Ajuste necessário no código existente:** `apps/backoffice/src/app/layouts/BackofficeLayout.tsx`
e `apps/backoffice/src/app/router.tsx` hoje usam `/inventory` e rótulos em inglês.

---

## 5. Inventário de telas

### T01 — Lista do estoque

**Rota** `/estoque` · **Papel** `operador` · **RF** 01, 02, 06

Ponto de entrada da operação. Tabela densa, otimizada para varredura e triagem.

**Conteúdo**

- Cabeçalho: título "Estoque curado", contagem total, botão primário **Cadastrar veículo**
- Faixa de filtros: busca (placa, marca, modelo) + chips de situação (`Em preparação`,
  `Elegível`, `Suspensa`, `Retirada`) + select de disponibilidade + select de UF
- Tabela, colunas: Veículo (marca + modelo + versão / placa em linha secundária) ·
  Ano · KM · Localização · Preço oficial · Situação (badge) · Disponibilidade (badge) ·
  Pendências (ícone + tipo, quando houver) · Atualizado em
- Linha inteira clicável → T03
- Paginação no rodapé

**Ações**: Cadastrar veículo → T02 · clicar linha → T03

**Estados**: vazio (nenhum veículo cadastrado, CTA de cadastro) · vazio por filtro
(com botão limpar filtros) · carregando (skeleton de 6 linhas) · erro de carga (com retry)

---

### T02 — Cadastro / edição de veículo

**Rota** `/estoque/novo`, `/estoque/:ofertaId/editar` · **Papel** `operador` · **RF** 01

Formulário em seções. **Salva com dados parciais** — é o comportamento central do RF-01.

**Seções e campos**

| Seção | Campos |
|---|---|
| Identificação | Placa* · Chassi (VIN) |
| Categoria | Tipo de veículo* — apenas `Carro seminovo` e `Carro usado` são selecionáveis (RN-01) |
| Dados básicos | Marca* · Modelo* · Versão · Ano de fabricação* · Ano modelo · Quilometragem* · Cor · Combustível · Câmbio |
| Localização | CEP · Cidade* · UF* |

`*` = compõe critério mínimo — **não** é obrigatório para salvar, apenas para elegibilidade.

**Comportamento-chave (RF-01):** salvar com campos vazios é caminho feliz, não erro. O
formulário mostra um aviso informativo, nunca bloqueante:

> ⓘ Este cadastro ficará **em preparação**. Faltam 3 critérios para solicitar elegibilidade.

Tentar cadastrar um tipo fora de carro seminovo/usado é recusado com mensagem explícita
(RN-01) — modelado como opções indisponíveis no select, não como erro pós-submit.

**Ações**: Salvar (→ T03) · Cancelar · Excluir (só em edição, só se `em-preparacao`)

**Estados**: criação · edição · salvando · erro de validação de formato (placa/ano/km) ·
tentativa de tipo não permitido

---

### T03 — Detalhe da oferta *(hub)*

**Rota** `/estoque/:ofertaId` · **Papel** ambos · **RF** 01–06 · **É a tela mais importante**

Layout de duas colunas.

**Cabeçalho**

- Marca + modelo + versão · placa · ano
- Badges: situação da oferta · disponibilidade
- Ações à direita: **Solicitar elegibilidade** (primária) · **Solicitar retirada** · menu ⋯ (Editar veículo)

**Coluna principal (~65%)**

1. **Alerta de suspensão** — quando `suspensa`, banner âmbar no topo: qual critério
   quebrou, quando, e que exige nova validação (RF-03)
2. **Card Fatos conhecidos** — três blocos (Origem · Condição · Histórico). Cada um mostra
   conteúdo + fonte + evidência, ou o selo `Limitação declarada` com o texto da limitação.
   Bloco sem preenchimento e sem limitação aparece em vermelho como pendência. Botão
   **Editar fatos** → T04
3. **Card Dados do veículo** — grade de leitura dos campos do T02, com link Editar

**Coluna lateral (~35%)**

4. **Card Preço oficial** — **dois estados**, não duas telas:
   - **Sem preço vigente** (oferta nova): sem valor, texto `Preço oficial ainda não definido`
     e botão **Definir preço** → M05-b. É ação **direta**, não vai para a fila.
   - **Com preço vigente**: valor grande (`R$ 87.900,00`), abaixo em texto pequeno
     `Atualizado em 12/08/2026 por Ana Souza` (DUX-06). Botão **Solicitar alteração** → M05.
     Havendo pendência de preço, tarja: `Alteração para R$ 84.500,00 aguardando validação`
5. **Card Disponibilidade** — estado atual + as transições válidas a partir dele como
   botões (§3.2). Em `vendido`, o único botão é **Solicitar reversão** → M06
6. **Card Critérios de elegibilidade** — checklist CM-1..CM-6 com ✓/✗, contador
   `4 de 6 critérios atendidos`. Cada item não atendido é link para o campo que resolve
7. **Card Pendências abertas** — lista de solicitações `pendente` da oferta: tipo, valor
   proposto, autor, data. Vazio na maior parte do tempo

**Regras de habilitação**

- **Solicitar elegibilidade** desabilitado se: nem todos os CM atendidos (tooltip lista
  os que faltam) · sem preço oficial (RF-04, caso explícito) · já há pendência de
  elegibilidade (DUX-07) · situação já `elegivel`
- **Solicitar retirada** desabilitado se situação `retirada` ou já há pendência do tipo

**Estados**: em preparação (checklist incompleto) · elegível · suspensa · retirada
(read-only, com faixa) · com pendências · carregando · não encontrada

---

### T04 — Fatos conhecidos

**Rota** `/estoque/:ofertaId/fatos` · **Papel** `operador` · **RF** 03

Tela própria porque são três blocos ricos e o preenchimento é a etapa de maior atrito
do MVP. Estrutura idêntica repetida 3×: **Origem**, **Condição**, **Histórico**.

**Cada bloco**

| Campo | Tipo | Regra |
|---|---|---|
| Toggle `Informação indisponível` | switch | Ao ligar, colapsa os campos abaixo e revela o campo de limitação |
| Descrição | textarea | O que a operação sabe |
| Fonte | texto | De onde veio (ex.: "Laudo cautelar Auto Check, 03/2026") |
| Evidência | upload de arquivo (S3, URL pré-autenticada) | Opcional. PDF/JPG/PNG. Ver §8.1 |
| Limitação declarada | textarea | **Obrigatório** quando o toggle está ligado (RN-03, CM-6) |

**Aviso permanente no topo** (DP-03 / RN-09):

> ⓘ Dado ausente não impede elegibilidade — dado ausente **sem limitação declarada**, sim.
> Nenhuma certificação formal é exigida.

**Efeito colateral que a tela deve avisar (RF-03):** se a oferta está `elegivel` e a edição
quebra um critério mínimo, um diálogo de confirmação avisa antes de salvar:

> Esta alteração suspende a elegibilidade desta oferta. Voltar a ser elegível exigirá nova validação.
> [Cancelar] [Salvar e suspender]

**Ações**: Salvar · Cancelar · Anexar evidência · Remover evidência
**Estados**: vazio · parcial · com limitação declarada · confirmação de suspensão ·
salvando · evidência enviando (progresso) · evidência anexada · falha de upload ·
arquivo recusado por tipo ou tamanho

---

### M05 — Solicitar alteração de preço *(modal sobre T03)*

**Papel** `operador` · **RF** 04

- Preço vigente (leitura), com data e responsável
- **Novo preço oficial** — input monetário BRL
- **Justificativa** — textarea, obrigatória
- Nota: `A alteração entra na fila de validação. O preço vigente continua valendo até a aprovação.` (RF-04)
- Ações: Cancelar · Enviar para validação

**Estados**: normal · valor inválido · já existe pendência de preço (modal abre em modo
informativo, sem formulário)

---

### M05-b — Definir preço inicial *(modal sobre T03)*

**Papel** `operador` · **RF** 04 · **Só quando não existe preço vigente**

Variante do M05, mais curta, porque não há valor vigente a comparar nem fila a alimentar:

- **Preço oficial** — input monetário BRL. Único campo obrigatório
- **Sem** bloco de preço vigente, **sem** linha de variação, **sem** justificativa
- Nota: `Este é o primeiro preço desta oferta e passa a valer imediatamente. Alterações
  futuras exigirão validação.`
- Ações: Cancelar · Definir preço

A justificativa não é pedida aqui porque não há decisão de terceiro a informar — ninguém vai
revisar. Exigi-la seria cerimônia sem leitor.

---

### M06 — Alterar disponibilidade *(modal sobre T03)*

**Papel** `operador` · **RF** 05

O modal é contextual à transição escolhida no card:

| Transição | Corpo do modal |
|---|---|
| → `reservado` | Confirmação + campo opcional de observação. Nota: `A reserva não expira automaticamente; liberar exige ação explícita.` (DP-04) |
| `reservado` → `disponivel` | Confirmação + motivo do encerramento |
| `reservado` → `vendido` | Confirmação, com aviso de que reverter exigirá validação |
| `vendido` → `disponivel` | **Formulário de solicitação**: justificativa obrigatória + nota de que vai para a fila |

Nota fixa em todos: `Retirar a oferta não altera a disponibilidade, e vice-versa.` (RN-05)

---

### T07 — Fila de validação

**Rota** `/validacao` · **Papel** `responsavel` · **RF** 02, 04, 05, 06

Fila de trabalho. Otimizada para decidir rápido e para o SLA de 1 dia útil.

**Conteúdo**

- Cabeçalho: "Validação", contagem de pendentes
- Filtros: chips de tipo (`Elegibilidade` · `Preço` · `Retirada` · `Reversão de venda`)
  + abas `Pendentes` / `Decididas`
- Tabela: Veículo (marca modelo / placa) · Tipo (badge colorido por tipo) ·
  Estado atual → Proposto · Solicitado por · Aberta há (ex.: `4h`, **`1d 6h` em vermelho** quando estoura o SLA)
- Ações rápidas por linha: **Aprovar** · **Rejeitar** · clicar linha → T08

O indicador de idade em vermelho é o que torna a meta de 90% em 1 dia útil visível
no lugar onde a decisão acontece.

**Estados**: fila vazia (estado positivo — "Nenhuma solicitação pendente") · com
pendências · aba decididas · SLA estourado · carregando

---

### T08 — Detalhe da solicitação

**Rota** `/validacao/:solicitacaoId` · **Papel** `responsavel` · **RF** 02, 04, 05, 06

Tela de decisão. Tudo que o Responsável precisa, sem precisar abrir a oferta.

**Conteúdo**

- Cabeçalho: badge do tipo · veículo (link para T03) · aberta em / por / há quanto tempo
- **Bloco de comparação** — o centro da tela: `Vigente` vs `Proposto` lado a lado.
  Para `elegibilidade`, o "proposto" é o checklist CM-1..CM-6 todo verde
- **Justificativa do solicitante** — texto integral
- **Contexto da oferta** — resumo read-only: fatos com limitações declaradas, preço,
  disponibilidade, situação
- **Impacto ao aprovar** — frase explícita do que muda. Ex.:
  `Ao aprovar, esta oferta passa a ser fornecida a D01 em até 1 hora.` /
  `Ao aprovar, a oferta deixa de ser fornecida a D01. A disponibilidade permanece "reservado".`
- **Barra de decisão fixa no rodapé**: [Rejeitar] [Aprovar]

**Rejeitar** abre campo de **justificativa obrigatória** (RF-02) — sem ela o botão de
confirmação fica desabilitado. A justificativa volta ao operador.

**Estados**: pendente · aprovada (read-only, com quem/quando) · rejeitada (read-only,
com motivo em destaque) · autoria própria (botões desabilitados, DUX-08) · carregando

---

## 6. Componentes compartilhados

Vão para `packages/ui` ou `apps/backoffice/src/shared/components`:

| Componente | Uso |
|---|---|
| `BadgeSituacao` | 4 estados da oferta, cores consistentes em T01/T03/T08 |
| `BadgeDisponibilidade` | 3 estados |
| `BadgeTipoSolicitacao` | 4 tipos, cor por tipo |
| `ValorComProcedencia` | valor + "Atualizado em X por Y" (DUX-06) |
| `ChecklistElegibilidade` | CM-1..CM-6, usado em T03 e T08 |
| `SeloLimitacao` | marca um fato como limitação declarada |
| `IndicadorSLA` | idade da solicitação, vermelho após 1 dia útil |
| `UploadEvidencia` | zona de upload + progresso + chip do anexo, sobre URL pré-autenticada S3 (§8.1) |
| `EmptyState` | **já existe** em `shared/components/EmptyState.tsx` |

---

## 7. Rastreabilidade

| RF | Telas | Regras |
|---|---|---|
| RF-01 Cadastro e manutenção | T02, T03 | RN-01, RN-02, RN-10 |
| RF-02 Curadoria e retirada | T03, T07, T08 | RN-02, RN-05, RN-07 |
| RF-03 Fatos conhecidos | T04, T03 | RN-03, RN-07, RN-09 |
| RF-04 Preço oficial | M05, T03, T08 | RN-06, RN-07 |
| RF-05 Disponibilidade | M06, T03, T07, T08 | RN-04, RN-08 |
| RF-06 Elegibilidade | T03 (checklist), T07, T08 | RN-03, RN-05, RN-07, RN-09 |

Toda tela do inventário rastreia a pelo menos um RF. Nenhum RF ficou sem tela.

---

## 8. Decisões resolvidas

Eram questões em aberto; decididas em 16/08/2026. Todas entram no API Contract.

| ID | Questão | Decisão |
|---|---|---|
| QA-01 | Oferta `retirada` pode voltar ao estoque? | **Sim.** Via nova solicitação de elegibilidade, a partir do T03 em modo read-only. Adiciona a transição `retirada` → `elegivel` ao §3.1. |
| QA-02 | `disponivel` → `vendido` direto? | **Permitido.** A operação real vende sem reserva prévia. Adiciona a transição ao §3.2, sem validação. |
| QA-03 | Mesma pessoa nos dois papéis? | **Papéis acumuláveis**, mas ninguém aprova a própria solicitação. DUX-08 é regra de **sistema**, não só de UI — o backend rejeita a aprovação, a UI apenas antecipa. |
| QA-04 | Evidência é upload ou URL? | **Upload de arquivo em S3 com URL pré-autenticada.** Ver §8.1. |
| QA-05 | "1 dia útil" considera feriados? | **Não na Fase 1.** Seg–sex, sem calendário de feriados. Cálculo do `IndicadorSLA`. |

### 8.1 Evidência — upload S3 com URL pré-autenticada

Substitui a proposta inicial de campo de URL em texto. Impacto em três lugares:

**Fluxo na UI (T04)** — o campo `Evidência` de cada bloco vira uma zona de upload:

1. Operador seleciona ou arrasta o arquivo
2. Front pede ao backend uma URL de upload pré-autenticada
3. Front faz `PUT` direto no S3, com barra de progresso
4. Front confirma ao backend, que passa a referenciar a evidência
5. Para visualizar, o front pede uma URL de leitura pré-autenticada, de vida curta

O anexo aparece como um chip com nome do arquivo, tamanho, ação de baixar e de remover.

**Estados novos no T04**: sem evidência · enviando (progresso) · anexada · falha de
upload (com retry) · tipo ou tamanho recusado.

**Impacto no API Contract** — dois endpoints a mais do que o previsto:

| Endpoint | Papel |
|---|---|
| `POST /ofertas/{id}/evidencias/upload-url` | Devolve URL pré-autenticada de escrita + chave do objeto |
| `GET /evidencias/{chave}/download-url` | Devolve URL pré-autenticada de leitura, de vida curta |

Restrições a definir na TechSpec: tipos aceitos (PDF, JPG, PNG), tamanho máximo,
tempo de vida das URLs, política de retenção e se o bucket é privado com acesso
exclusivo por URL assinada — **sim, deve ser**, já que evidências podem conter dado
pessoal (RN-03 exige declarar a fonte, não expô-la publicamente).

**Efeito no CM-6**: evidência continua **opcional**. O critério mínimo exige conteúdo
**ou** limitação declarada — nunca anexo. Um fato com fonte textual e sem arquivo é
válido.

---

## 9. Ajustes de frontend registrados (não executados)

Nada de `apps/` foi alterado neste planejamento. Estes itens viram tasks na etapa de
`tsg-flow-task-creator`:

| ID | Ajuste | Arquivo | Origem |
|---|---|---|---|
| AJ-01 | Traduzir o shell de EN para PT-BR: nav (`Dashboard`→`Painel`, `Inventory`→`Estoque`, `Leads`→`Interesses`, `Purchases`→`Compras`), header (`Operations workspace`→`Área de operação`, `Sign out`→`Sair`) | `apps/backoffice/src/app/layouts/BackofficeLayout.tsx` | DUX-01 |
| AJ-02 | Adicionar item `Validação` à sidebar, com badge de contagem de pendências | `apps/backoffice/src/app/layouts/BackofficeLayout.tsx` | DUX-02 |
| AJ-03 | Renomear rota `/inventory` → `/estoque` e adicionar `/validacao` | `apps/backoffice/src/app/router.tsx` | §4 |
| AJ-04 | **Substituir os tokens inferidos pelos do `DESIGN.md`.** Os valores atuais (primária azul `#2563eb`, superfície `#f8fafc`) contradizem o sistema real (primária Deep Navy `#2E2E3A`, ação laranja `#FC8422`, fundo `#f9f9ff`). Inclui a escala tipográfica, `data-tabular` e `label-caps`. | `packages/ui/src/tokens/tokens.css`, `packages/ui/tailwind.preset.ts` | `DESIGN.md` |
| AJ-05 | Atualizar `.stitch/metadata.json`: `tokensSource` deixa de ser `inferred-minimal` | `.stitch/metadata.json` | `DESIGN.md` |
| AJ-06 | Criar os componentes compartilhados do §6 | `packages/ui`, `apps/backoffice/src/shared/components` | §6 |
| AJ-07 | Adicionar `estoque:validar` a `API_SCOPES` — a lista atual tem 4 scopes e não inclui o do Responsável de validação | `apps/backoffice/src/features/auth/config/oidcConfig.ts` | QC-01 do `api-contract.md` |
| AJ-08 | Esconder o item `Validação` da sidebar para quem não tem `estoque:validar` | `apps/backoffice/src/app/layouts/BackofficeLayout.tsx` | DUX-02 |
| AJ-09 | **Card de preço da T03 precisa do estado "sem preço vigente"** — o HTML gerado só cobre o estado com valor e o botão `Solicitar alteração`. Falta o estado com `Definir preço` e o modal M05-b | `tasks/.../screens/t03-detalhe-oferta.html`, `m05-modal-preco.html` | QT-01 da `techspec.md` |

**AJ-04 é o mais relevante e o mais silencioso.** Enquanto ele não for feito, o HTML que
sair do Stitch e o código do backoffice usam paletas diferentes — o Stitch em Deep Navy
e laranja, o código em azul. Não quebra nada, mas o primeiro componente construído a
partir do HTML vai parecer "fora do lugar" sem motivo aparente.

---

*Próxima etapa: `api-contract.yaml` (OpenAPI 3.1).*
