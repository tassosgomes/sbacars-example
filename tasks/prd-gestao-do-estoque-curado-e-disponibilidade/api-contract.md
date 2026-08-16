# API Contract — Gestão do Estoque Curado e Disponibilidade

> **Gerado a partir de:** `prd.md` (RF-01 a RF-06) e `ux-spec.md`
> **Domain doc:** `domains/estoque-curado/domain.md` (RN-01 a RN-10)
> **Data:** 2026-08-16
> **Status:** Em Revisão
> **Versão do contrato:** 1.0.0
> **Spec técnica:** `api-contract.yaml` (OpenAPI 3.1) — fonte de verdade para tipos

---

## Premissas e Decisões

Todas derivadas do código já existente em `backend/`. Este contrato **não introduz padrão
novo** — exceto onde marcado com ⚠️.

| Decisão | Escolha | Motivo |
|---|---|---|
| Autenticação | OAuth2 Authorization Code + PKCE, JWT do Logto | Já implementado em `AuthExtensions` e `oidcConfig.ts`. O resource indicator RFC 8707 `https://api.sbacars.app` é obrigatório. |
| Autorização | Políticas por permissão | `Permissoes.EstoqueLer`, `EstoqueGerenciar` e ⚠️ `estoque:validar` (novo) |
| Paginação | `page` 1-based + `pageSize`, padrão 20, máx 100 | Espelha `PagedRequest`/`PagedResult` — serialização sem adaptador |
| Formato de erro | RFC 9457 `ProblemDetails` + extensão `traceId` | É exatamente o que `GlobalExceptionHandler` já emite |
| Datas | ISO 8601 UTC | `UtcDateTimeOffsetConverter` já garante isso na persistência |
| Valores monetários | **Inteiro em centavos**, sufixo `Centavos` | O consumidor é TypeScript: `number` é float binário e `87900.10` não é exato |
| Nomenclatura JSON | `camelCase` | Padrão do ASP.NET Core, sem configuração extra |
| Enums | `kebab-case` | Casa com o vocabulário do domain doc (`em-preparacao`, `reversao-venda`) |
| Versionamento | Sem prefixo de versão | O backend já usa `/api/...` direto |
| Arrays vazios | `[]`, nunca `null` | Elimina branch de nulidade no cliente |

### Roteamento pelo gateway

O `SbaCars.Gateway.Backoffice` já mapeia `/api/inventory/{**rest}` para o inventory-service,
removendo o prefixo e reaplicando `/api`:

| Consumidor chama | inventory-service implementa |
|---|---|
| `GET /api/inventory/ofertas` | `GET /api/ofertas` |
| `POST /api/inventory/solicitacoes/{id}/aprovar` | `POST /api/solicitacoes/{id}/aprovar` |

Os paths deste documento são **relativos ao gateway**, que é o que o frontend consome.

---

## Resumo de Endpoints

| Método | Path | Descrição | Permissão | Status |
|---|---|---|---|---|
| `GET` | `/ofertas` | Listar ofertas do estoque | `estoque:ler` | 200, 400, 401, 403, 500 |
| `POST` | `/ofertas` | Cadastrar veículo (aceita parcial) | `estoque:gerenciar` | 201, 400, 401, 403, 409, 422, 500 |
| `GET` | `/ofertas/{ofertaId}` | Detalhe consolidado da oferta | `estoque:ler` | 200, 401, 403, 404, 500 |
| `DELETE` | `/ofertas/{ofertaId}` | Excluir oferta em preparação | `estoque:gerenciar` | 204, 401, 403, 404, 422, 500 |
| `PATCH` | `/ofertas/{ofertaId}/veiculo` | Atualizar dados do veículo | `estoque:gerenciar` | 200, 400, 401, 403, 404, 409, 422, 500 |
| `PUT` | `/ofertas/{ofertaId}/fatos` | Substituir os três blocos de fatos | `estoque:gerenciar` | 200, 400, 401, 403, 404, 409, 422, 500 |
| `POST` | `/ofertas/{ofertaId}/evidencias/upload-url` | Gerar URL S3 de upload | `estoque:gerenciar` | 201, 400, 401, 403, 404, 413, 415, 500 |
| `GET` | `/evidencias/{evidenciaId}/download-url` | Gerar URL S3 de leitura | `estoque:ler` | 200, 401, 403, 404, 500 |
| `POST` | `/ofertas/{ofertaId}/disponibilidade` | Transição direta de disponibilidade | `estoque:gerenciar` | 200, 400, 401, 403, 404, 422, 500 |
| `POST` | `/ofertas/{ofertaId}/solicitacoes` | Abrir solicitação de validação | `estoque:gerenciar` | 201, 400, 401, 403, 404, 409, 422, 500 |
| `GET` | `/solicitacoes` | Fila de validação | `estoque:validar` | 200, 400, 401, 403, 500 |
| `GET` | `/solicitacoes/pendentes/contagem` | Contagem para o badge da sidebar | `estoque:validar` | 200, 401, 403, 500 |
| `GET` | `/solicitacoes/{solicitacaoId}` | Detalhe da solicitação | `estoque:validar` | 200, 401, 403, 404, 500 |
| `POST` | `/solicitacoes/{solicitacaoId}/aprovar` | Aprovar e aplicar | `estoque:validar` | 200, 401, 403, 404, 409, 422, 500 |
| `POST` | `/solicitacoes/{solicitacaoId}/rejeitar` | Rejeitar com justificativa | `estoque:validar` | 200, 400, 401, 403, 404, 409, 422, 500 |
| `GET` | `/ofertas-elegiveis` | Fornecer ofertas elegíveis a D01 | `estoque:ler` | 200, 400, 401, 403, 500 |

**16 endpoints.** Cobertura de RF-01 a RF-06 na seção "Rastreabilidade".

---

## Endpoints Detalhados

### `GET /ofertas` — Listar ofertas do estoque

**Propósito:** carregar a tela de estoque com triagem por situação, disponibilidade e localização.
**Consumido por:** T01 — Lista do estoque.

| Parâmetro | Tipo | Obrigatório | Default | Descrição |
|---|---|---|---|---|
| `page` | integer | Não | 1 | Página, 1-based |
| `pageSize` | integer | Não | 20 | Máximo 100 |
| `busca` | string | Não | — | Placa, marca ou modelo. Mínimo 2 caracteres |
| `situacao` | array | Não | — | `OR` entre valores |
| `disponibilidade` | array | Não | — | `OR` entre valores |
| `uf` | string | Não | — | Sigla de 2 letras |
| `ordenarPor` | enum | Não | `atualizadoEm:desc` | Ordem em que a operação espera ver o que mudou |

**Response 200**

```json
{
  "items": [
    {
      "ofertaId": "7a4c1e90-2b8d-4f6a-9c31-0e5b7d2a8f14",
      "placa": "ABC1D23",
      "descricaoVeiculo": "Honda Civic EXL 2.0",
      "anoFabricacao": 2021,
      "anoModelo": 2022,
      "quilometragem": 48300,
      "localizacao": { "cep": "13010-111", "cidade": "Campinas", "uf": "SP" },
      "precoOficialCentavos": 8790000,
      "situacao": "elegivel",
      "disponibilidade": "disponivel",
      "pendencias": ["preco"],
      "atualizadoEm": "2026-08-14T18:02:11Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 142,
  "totalPages": 8,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

---

### `POST /ofertas` — Cadastrar veículo

**Propósito:** criar o veículo e a oferta. **Aceita dados parciais** — comportamento central
do RF-01, não exceção. A oferta nasce em `em-preparacao`.
**Consumido por:** T02 — Cadastro de veículo.

**Request** (o caso comum é este, incompleto)

```json
{
  "placa": "ABC1D23",
  "tipoVeiculo": "carro-seminovo",
  "marca": "Honda",
  "modelo": "Civic"
}
```

Apenas `tipoVeiculo` é obrigatório, e só aceita `carro-seminovo` ou `carro-usado` (RN-01);
qualquer outro valor retorna 422.

**Response 201** — corpo igual ao de `GET /ofertas/{ofertaId}`, com `Location` apontando
para a oferta criada.

---

### `GET /ofertas/{ofertaId}` — Detalhe da oferta

**Propósito:** carga **única** da tela de detalhe. O frontend não deve compor essa tela com
várias chamadas.
**Consumido por:** T03 — Detalhe da oferta (hub).

**Response 200** (abreviado nos blocos repetidos)

```json
{
  "ofertaId": "7a4c1e90-2b8d-4f6a-9c31-0e5b7d2a8f14",
  "situacao": "elegivel",
  "motivoSuspensao": null,
  "suspensaEm": null,
  "veiculo": {
    "placa": "ABC1D23",
    "chassi": "93HFC2650MZ204817",
    "tipoVeiculo": "carro-seminovo",
    "marca": "Honda",
    "modelo": "Civic",
    "versao": "EXL 2.0",
    "anoFabricacao": 2021,
    "anoModelo": 2022,
    "quilometragem": 48300,
    "cor": "Prata",
    "combustivel": "Flex",
    "cambio": "Automático",
    "localizacao": { "cep": "13010-111", "cidade": "Campinas", "uf": "SP" }
  },
  "fatos": {
    "origem": {
      "tipo": "origem",
      "indisponivel": false,
      "descricao": "Veículo de frota corporativa, único proprietário pessoa jurídica.",
      "fonte": "Contrato de cessão Localiza, 02/2026",
      "evidencia": {
        "evidenciaId": "3f2a8c14-9d31-4c7e-b8a1-5e6f0d2b9c47",
        "nomeArquivo": "contrato-cessao-localiza.pdf",
        "tipoConteudo": "application/pdf",
        "tamanhoBytes": 1258291,
        "enviadaEm": "2026-08-10T11:04:33Z"
      },
      "limitacaoDeclarada": null,
      "atendeTransparencia": true,
      "atualizadoPor": {
        "usuarioId": "b21e4f77-0c53-4a8e-91d2-6f4a8c05e3b9",
        "nome": "Ana Souza",
        "em": "2026-08-10T11:05:02Z"
      }
    },
    "condicao": { "tipo": "condicao", "indisponivel": false, "atendeTransparencia": true },
    "historico": {
      "tipo": "historico",
      "indisponivel": true,
      "descricao": null,
      "fonte": null,
      "evidencia": null,
      "limitacaoDeclarada": "Não foi possível obter o histórico de sinistros deste veículo junto às bases consultadas.",
      "atendeTransparencia": true
    }
  },
  "precoOficial": {
    "valorCentavos": 8790000,
    "moeda": "BRL",
    "definidoPor": {
      "usuarioId": "b21e4f77-0c53-4a8e-91d2-6f4a8c05e3b9",
      "nome": "Ana Souza",
      "em": "2026-08-12T14:22:05Z"
    }
  },
  "disponibilidade": {
    "estado": "disponivel",
    "desde": "2026-08-05T09:14:00Z",
    "transicoesPermitidas": ["reservado", "vendido"]
  },
  "elegibilidade": {
    "atendidos": 5,
    "total": 6,
    "criterios": [
      { "codigo": "identificacao", "atendido": true },
      { "codigo": "dados-basicos", "atendido": true },
      { "codigo": "localizacao", "atendido": true },
      { "codigo": "preco-oficial", "atendido": true },
      { "codigo": "disponibilidade", "atendido": true },
      {
        "codigo": "transparencia-fatos",
        "atendido": false,
        "pendencia": "Condição sem limitação declarada"
      }
    ],
    "podeSolicitarElegibilidade": false
  },
  "pendencias": [
    {
      "solicitacaoId": "9c1f7e28-4b6a-4d5f-8a09-2c3e5b7d1f40",
      "tipo": "preco",
      "resumoAlteracao": "R$ 87.900,00 → R$ 84.500,00",
      "abertaEm": "2026-08-16T09:12:00Z",
      "abertaPor": {
        "usuarioId": "d94b2a10-7e6f-4c3b-8a55-1b9e0f7c4d28",
        "nome": "Carlos Lima",
        "em": "2026-08-16T09:12:00Z"
      }
    }
  ],
  "criadaEm": "2026-08-05T09:00:00Z",
  "atualizadoEm": "2026-08-14T18:02:11Z"
}
```

Três campos existem para que a interface **não reimplemente regra de domínio**:
`disponibilidade.transicoesPermitidas` (quais botões existem), `elegibilidade.podeSolicitarElegibilidade`
(se o botão primário está habilitado) e `fatos.*.atendeTransparencia` (se o bloco está em falta).

---

### `PATCH /ofertas/{ofertaId}/veiculo` e `PUT /ofertas/{ofertaId}/fatos` — o protocolo de suspensão

**Consumido por:** T02 (edição) e T04 (fatos).

Ambos compartilham a mesma mecânica de duas fases, que implementa o RF-03.

1. Cliente envia a alteração com `confirmaSuspensao: false`
2. Se a alteração fizer a oferta `elegivel` deixar de cumprir um critério, o servidor responde
   **409** e **não grava nada**:

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "Alteração suspenderia a elegibilidade.",
  "status": 409,
  "detail": "Confirme a suspensão para prosseguir.",
  "instance": "/api/ofertas/7a4c1e90-2b8d-4f6a-9c31-0e5b7d2a8f14/fatos",
  "traceId": "0af7651916cd43dd8448eb211c80319c",
  "codigo": "suspensao-nao-confirmada",
  "criteriosAfetados": ["transparencia-fatos"]
}
```

3. Cliente exibe o diálogo "Salvar e suspender" usando `criteriosAfetados`
4. Cliente repete com `confirmaSuspensao: true`; a oferta passa a `suspensa`

`PUT` em fatos substitui os três blocos de uma vez — espelha o formulário completo da tela.
Um bloco com `indisponivel: true` exige `limitacaoDeclarada` e ignora descrição, fonte e
evidência.

---

### Upload de evidência — fluxo S3

**Consumido por:** T04 — campo Evidência de cada bloco.

```
1. POST /ofertas/{id}/evidencias/upload-url    → { evidenciaId, uploadUrl, headersObrigatorios }
2. PUT  <uploadUrl>  (browser → S3, direto)     → 200
3. PUT  /ofertas/{id}/fatos  com evidenciaId    → vincula a evidência ao bloco
4. GET  /evidencias/{evidenciaId}/download-url  → URL de leitura, no clique
```

**Request de (1)**

```json
{
  "nomeArquivo": "laudo-cautelar.pdf",
  "tipoConteudo": "application/pdf",
  "tamanhoBytes": 4194304
}
```

**Response 201**

```json
{
  "evidenciaId": "5b8d3f21-6a04-4e9c-b712-8c3f0a5d6e29",
  "uploadUrl": "https://s3.sa-east-1.amazonaws.com/autotransparencia-evidencias/...",
  "expiraEm": "2026-08-16T10:45:00Z",
  "headersObrigatorios": { "Content-Type": "application/pdf" }
}
```

Restrições da Fase 1: PDF, JPEG e PNG; máximo 10 MiB. O bucket é **privado** — o objeto só é
acessível por URL assinada, porque evidências de origem podem conter dado pessoal.

---

### `POST /ofertas/{ofertaId}/disponibilidade` — Transição direta

**Consumido por:** T03 (card) e M06 (modal).

Aceita apenas transições que **não** exigem validação:

| De | Para |
|---|---|
| `disponivel` | `reservado` |
| `disponivel` | `vendido` |
| `reservado` | `disponivel` |
| `reservado` | `vendido` |

`vendido` → `disponivel` **não** passa aqui: exige validação, via `POST /ofertas/{id}/solicitacoes`
com `tipo: reversao-venda`.

Reservas nunca expiram sozinhas (DP-04). Alterar disponibilidade não altera a situação da
oferta e vice-versa (RN-05). Agendamento de test drive por D03 nunca chama este endpoint (RN-08).

```json
{ "novoEstado": "reservado", "observacao": "Reserva para test drive agendado." }
```

---

### `POST /ofertas/{ofertaId}/solicitacoes` — Abrir solicitação

**Consumido por:** T03 (elegibilidade, retirada), M05 (preço), M06 (reversão).

**Um endpoint para os quatro tipos**, discriminado por `tipo` — decisão DUX-03 da UX spec:
solicitação é uma entidade única, o que mantém a fila, a tela de detalhe e o ciclo
aprovar/rejeitar uniformes.

```json
{
  "tipo": "preco",
  "novoPrecoCentavos": 8450000,
  "justificativa": "Ajuste para alinhar ao valor de mercado da região."
}
```

| `tipo` | Campo extra | Regra |
|---|---|---|
| `elegibilidade` | — | Exige os 6 critérios atendidos. Também reinclui oferta `retirada` (QA-01) |
| `preco` | `novoPrecoCentavos` | Preço vigente continua valendo até aprovar (RF-04) |
| `retirada` | — | Ao aprovar, não altera a disponibilidade (RN-05) |
| `reversao-venda` | — | Só a partir de `vendido` |

Solicitação pendente do mesmo tipo para a mesma oferta → **409** (DUX-07). A interface deve
desabilitar o botão antes disso; o 409 é rede de segurança.

---

### `GET /solicitacoes` — Fila de validação

**Consumido por:** T07 — Fila de validação.

Padrão: apenas `pendente`, da mais antiga para a mais recente — a ordem em que o SLA de um
dia útil deve ser atacado.

```json
{
  "items": [
    {
      "solicitacaoId": "9c1f7e28-4b6a-4d5f-8a09-2c3e5b7d1f40",
      "ofertaId": "7a4c1e90-2b8d-4f6a-9c31-0e5b7d2a8f14",
      "placa": "ABC1D23",
      "descricaoVeiculo": "Honda Civic EXL 2.0",
      "tipo": "elegibilidade",
      "status": "pendente",
      "valorVigente": "Em preparação",
      "valorProposto": "Elegível",
      "abertaEm": "2026-08-15T09:12:00Z",
      "abertaPor": {
        "usuarioId": "d94b2a10-7e6f-4c3b-8a55-1b9e0f7c4d28",
        "nome": "Carlos Lima",
        "em": "2026-08-15T09:12:00Z"
      },
      "foraDoSla": true
    }
  ],
  "page": 1, "pageSize": 20, "totalCount": 7,
  "totalPages": 1, "hasNextPage": false, "hasPreviousPage": false
}
```

`foraDoSla` é **calculado no servidor**. Se o cliente derivasse do relógio local, o indicador
vermelho da fila mudaria conforme a máquina do operador — e é justamente esse indicador que
torna a meta de 90% em um dia útil visível onde a decisão acontece.

---

### `POST /solicitacoes/{id}/aprovar` e `/rejeitar` — Decisão

**Consumido por:** T07 (ações rápidas) e T08 (tela de decisão).

**Quem abriu não pode aprovar** (DUX-08). É regra de **servidor**, não de interface — a
tentativa retorna 403 mesmo que a UI tenha permitido o clique:

```json
{
  "type": "https://httpstatuses.io/403",
  "title": "Aprovação não permitida.",
  "status": 403,
  "detail": "Quem abriu a solicitação não pode aprová-la.",
  "instance": "/api/solicitacoes/9c1f7e28-4b6a-4d5f-8a09-2c3e5b7d1f40/aprovar",
  "traceId": "0af7651916cd43dd8448eb211c80319c"
}
```

O campo `podeDecidir` no detalhe permite à interface antecipar isso e desabilitar os botões.

Rejeitar exige `justificativa` — ela volta ao operador como motivo (RF-02).

---

### `GET /ofertas-elegiveis` — Integração com D01

**Consumido por:** catalog-service (D01), **não** pelo backoffice.

Fornece apenas ofertas em situação `elegivel`. Retiradas, suspensas e em preparação nunca
aparecem — é a terceira condição de aceite do RF-06.

A projeção `OfertaElegivel` **não expõe** solicitações, checklist nem qualquer dado do fluxo
interno de validação. D01 recebe o que o comprador precisa ver, incluindo as limitações
declaradas, e nada do processo.

`atualizadoApos` permite sincronização incremental. Este endpoint é a **reconciliação**; o
caminho quente é o evento. D01 deve usá-lo para corrigir divergências, não como polling primário.

---

## Schemas Principais

### Oferta (detalhe)

| Campo | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `ofertaId` | uuid | Sim | Identificador da oferta curada |
| `situacao` | enum | Sim | `em-preparacao`, `elegivel`, `suspensa`, `retirada` |
| `motivoSuspensao` | string\|null | Não | Só quando `suspensa` |
| `veiculo` | Veiculo | Sim | Todos os campos opcionais exceto `tipoVeiculo` |
| `fatos` | FatosConhecidos | Sim | Três blocos: origem, condição, histórico |
| `precoOficial` | PrecoOficial\|null | Não | `null` enquanto não definido |
| `disponibilidade` | Disponibilidade | Sim | Estado + `transicoesPermitidas` |
| `elegibilidade` | ChecklistElegibilidade | Sim | CM-1 a CM-6 + `podeSolicitarElegibilidade` |
| `pendencias` | PendenciaResumo[] | Sim | `[]` quando não há nenhuma |

### BlocoFato

| Campo | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `tipo` | enum | Sim | `origem`, `condicao`, `historico` |
| `indisponivel` | boolean | Sim | `true` = informação não obtida, com limitação declarada |
| `descricao` | string\|null | Não | O que a operação sabe |
| `fonte` | string\|null | Não | De onde veio |
| `evidencia` | Evidencia\|null | Não | Anexo em S3 |
| `limitacaoDeclarada` | string\|null | Condicional | **Obrigatória** quando `indisponivel` é `true` |
| `atendeTransparencia` | boolean | Sim | Derivado: tem conteúdo **ou** limitação declarada |

### Solicitação (detalhe)

| Campo | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `solicitacaoId` | uuid | Sim | — |
| `tipo` | enum | Sim | `elegibilidade`, `preco`, `retirada`, `reversao-venda` |
| `status` | enum | Sim | `pendente`, `aprovada`, `rejeitada` |
| `valorVigente` / `valorProposto` | string\|null | Não | Transição já em texto exibível |
| `justificativa` | string | Sim | Texto de quem abriu |
| `novoPrecoCentavos` | integer\|null | Condicional | Só quando `tipo` é `preco` |
| `elegibilidadeProposta` | Checklist\|null | Condicional | Só quando `tipo` é `elegibilidade` |
| `impactoAoAprovar` | string | Sim | Consequência em texto, gerada pelo servidor |
| `foraDoSla` | boolean | Sim | Pendente há mais de um dia útil |
| `podeDecidir` | boolean | Sim | `false` se o usuário autenticado abriu a solicitação |

---

## Códigos de Erro

| Status | Quando | Corpo |
|---|---|---|
| `400` | Requisição malformada, parâmetro inválido | `ProblemDetails` |
| `401` | Token ausente, expirado ou inválido | `ProblemDetails` |
| `403` | Sem a permissão exigida, **ou** tentativa de aprovar a própria solicitação | `ProblemDetails` |
| `404` | Oferta, solicitação ou evidência inexistente | `ProblemDetails` |
| `409` | Placa duplicada · solicitação pendente do mesmo tipo · já decidida · **suspensão não confirmada** | `ProblemDetails` (+ `codigo` e `criteriosAfetados` no caso de suspensão) |
| `413` | Evidência maior que 10 MiB | `ProblemDetails` |
| `415` | Tipo de arquivo fora de PDF/JPEG/PNG | `ProblemDetails` |
| `422` | Regra de domínio violada: tipo fora de carro seminovo/usado (RN-01), transição inválida, critério mínimo ausente | `ProblemDetails` |
| `500` | Erro inesperado — `traceId` correlaciona com o log | `ProblemDetails` |

Todo corpo de erro segue RFC 9457 com a extensão `traceId`, exatamente como o
`GlobalExceptionHandler` já produz. Nenhum carrega stack trace ou nome de tipo interno.

---

## Eventos publicados

Fora do escopo HTTP — Rebus/RabbitMQ como CloudEvents, via `SbaCars.BuildingBlocks.Messaging`.

| Evento | Publicado quando |
|---|---|
| `estoque.oferta-incluida` | Cadastro criado; solicitação de elegibilidade aprovada |
| `estoque.oferta-atualizada` | Veículo, fatos ou preço alterados; elegibilidade suspensa ou restaurada |
| `estoque.oferta-retirada` | Solicitação de retirada aprovada |
| `estoque.disponibilidade-alterada` | Transição direta ou reversão de venda aprovada |

O evento é o caminho quente para D01 refletir mudanças em até uma hora; `GET /ofertas-elegiveis`
é a reconciliação.

---

## Rastreabilidade

| RF | Endpoints | Telas |
|---|---|---|
| RF-01 Cadastro e manutenção | `POST /ofertas`, `PATCH /ofertas/{id}/veiculo`, `DELETE /ofertas/{id}` | T02, T03 |
| RF-02 Curadoria e retirada | `POST /ofertas/{id}/solicitacoes` (`retirada`), `POST /solicitacoes/{id}/aprovar`, `/rejeitar` | T03, T07, T08 |
| RF-03 Fatos conhecidos | `PUT /ofertas/{id}/fatos`, endpoints de evidência | T04, T03 |
| RF-04 Preço oficial | `POST /ofertas/{id}/solicitacoes` (`preco`) | M05, T03, T08 |
| RF-05 Disponibilidade | `POST /ofertas/{id}/disponibilidade`, solicitação `reversao-venda` | M06, T03, T07, T08 |
| RF-06 Elegibilidade | `GET /ofertas/{id}` (checklist), solicitação `elegibilidade`, `GET /ofertas-elegiveis` | T03, T07, T08 |

Nenhum RF ficou sem endpoint; nenhum endpoint ficou sem RF.

---

## Questões em aberto

Precisam de decisão antes da implementação.

| ID | Questão | Impacto | Proposta |
|---|---|---|---|
| QC-01 | ⚠️ **`estoque:validar` é uma permissão nova.** `Permissoes` documenta a Fase 1 como fechada em 4 permissões e diz que a Fase 2 adiciona `compra:gerenciar` e `reserva:extender` "e em nenhum outro lugar". | Sem ela, o Responsável de validação não se distingue do Operador e o DUX-08 vira honra. Exige criar o scope no Logto, adicionar a `Permissoes.All` e incluir em `API_SCOPES` no `oidcConfig.ts`. | Criar. A alternativa — reusar `estoque:gerenciar` — elimina a segregação que é a razão de existir do DP-02. |
| QC-02 | O bucket S3 e a política de retenção de evidências ainda não existem. | Bloqueia RF-03 fim a fim. | Definir na TechSpec de backend: bucket privado, criptografia em repouso, retenção alinhada à LGPD. |
| QC-03 | `GET /ofertas-elegiveis` é chamado por catalog-service com qual identidade? | Token de usuário não serve para chamada serviço-a-serviço. | Client credentials com scope próprio (ex.: `estoque:integrar`), decidido junto com QC-01. |
| QC-04 | A reconciliação de D01 roda em qual periodicidade? | Afeta a meta de "até uma hora" quando um evento se perde. | A cada 15 min com `atualizadoApos`, folga confortável dentro da meta. |
| QC-05 | Placa é imutável após o cadastro? | O `PATCH` hoje permite alterá-la; se for imutável, sai do schema de patch. | Editável enquanto `em-preparacao`, imutável depois. |

---

## Próximos passos

| Para | Instrução |
|---|---|
| **Backend** | `tsg-flow-techspec-creator` referenciando `api-contract.yaml` como spec dos endpoints |
| **Frontend** | `tsg-flow-frontend-techspec-creator` — os schemas são a fonte de verdade para os tipos TypeScript |
| **Mocks** | `npx @stoplight/prism-cli mock tasks/prd-gestao-do-estoque-curado-e-disponibilidade/api-contract.yaml` |
| **Lint** | `npx @stoplight/spectral-cli lint tasks/prd-.../api-contract.yaml --ruleset .claude/skills/tsg-flow-contract-creator/rulesets/openapi.yaml --fail-severity=error` |
