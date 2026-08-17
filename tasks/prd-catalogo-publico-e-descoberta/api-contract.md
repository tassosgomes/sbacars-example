# API Contract — Catálogo Público e Descoberta

> **Gerado a partir de:** `prd.md` (RF-01 a RF-07) e `ux-spec.md`
> **Domain doc:** `domains/catalogo-descoberta/domain.md` (RN-01 a RN-12)
> **Decisões de produto:** [PD-001](../../docs/product-decisions/PD-001-dado-pessoal-do-comprador-pertence-a-d03.md) · [PD-002](../../docs/product-decisions/PD-002-sem-identidade-de-comprador-na-fase-1.md)
> **Data:** 2026-08-16
> **Status:** Em Revisão
> **Versão do contrato:** 1.0.0
> **Spec técnica:** `api-contract.yaml` (OpenAPI 3.1) — fonte de verdade para tipos

---

## O que separa esta API da de D02

Uma diferença estrutural, e vale enunciá-la antes de qualquer detalhe: **esta API é
inteiramente pública e anônima.** Sem `Authorization`, sem sessão, sem usuário, sem
permissão. É a consequência direta de PD-002, e ela muda quase tudo:

| | D02 — Estoque | D01 — Catálogo |
|---|---|---|
| Autenticação | JWT do Logto, obrigatório | **Nenhuma** |
| Autorização | 4 permissões por endpoint | Não se aplica |
| Proteção | Confiança no token | **Limitador por IP** — é a única barreira |
| Identidade | Usuário da Operação central | **Navegador**, nunca pessoa |
| Escrita | Central, com validação humana | 3 endpoints, todos anônimos |
| Dado pessoal | Nome do operador em cada valor | **Nenhum, em lugar nenhum** |

A consequência prática é que toda decisão deste contrato responde a uma pergunta que não
existia em D02: *o que acontece se o mundo inteiro chamar isto?*

---

## Premissas e Decisões

Derivadas do código já existente em `backend/` e do contrato de D02. Este contrato **não
introduz padrão novo** — exceto onde marcado com ⚠️.

| Decisão | Escolha | Motivo |
|---|---|---|
| Autenticação | Nenhuma | PD-002. O gateway público já expõe `/api/catalog/{**rest}` sem política de autorização |
| Proteção de abuso | Limitador global por IP; `sbacars-anonymous-strict` em `POST /interesses` | `RateLimitingPolicies` já existe e seu comentário antecipa exatamente este endpoint |
| Identificação do navegador | Cabeçalho `X-Navegador-Id` (UUID gerado no cliente) | ⚠️ Novo. Termo canônico "Navegador identificado" (RN-07). Não é credencial |
| Paginação | `page` 1-based + `pageSize`, padrão 20, **máx 60** | Espelha `PagedRequest`/`PagedResult`. Máximo menor que os 100 de D02 porque cada item carrega fotos |
| Formato de erro | RFC 9457 `ProblemDetails` + extensão `traceId` | É o que `GlobalExceptionHandler` já emite |
| Datas | ISO 8601 UTC | `UtcDateTimeOffsetConverter` |
| Valores monetários | Inteiro em centavos, sufixo `Centavos` | Igual a D02, pelo mesmo motivo: o consumidor é TypeScript |
| Nomenclatura JSON | `camelCase` | Padrão do ASP.NET Core |
| Enums | `kebab-case` | Casa com o vocabulário do domain doc |
| Versionamento | Sem prefixo de versão | O backend já usa `/api/...` direto |
| Arrays vazios | `[]`, nunca `null` | Elimina branch de nulidade no cliente |
| **`null`** | **Significa "não informado" e é valor de primeira classe** | RN-02. Omitir a chave apagaria a distinção entre "não apurado" e "não existe", que é o núcleo do RF-03 |

### Roteamento pelo gateway

O `SbaCars.Gateway.Public` já mapeia `/api/catalog/{**rest}` para o catalog-service,
removendo o prefixo e reaplicando `/api`:

| Consumidor chama | catalog-service implementa |
|---|---|
| `GET /api/catalog/itens` | `GET /api/itens` |
| `POST /api/catalog/interesses` | `POST /api/interesses` |

Os paths deste documento são **relativos ao gateway**, que é o que o frontend consome.

**A rota atual aceita apenas `GET`, `HEAD` e `OPTIONS`.** Os três `POST` deste contrato
exigem rotas novas no gateway — ver AP-01 e AP-02.

---

## Resumo de Endpoints

| Método | Path | Descrição | Autenticação | Status |
|---|---|---|---|---|
| `GET` | `/itens` | Listar e buscar itens do catálogo | Pública | 200, 400, 422, 429, 500 |
| `GET` | `/itens/{itemId}` | Detalhe do item | Pública | 200, 400, 404, 429, 500 |
| `GET` | `/itens/resumos` | Resolver itens conhecidos pelo navegador | Pública | 200, 400, 429, 500 |
| `GET` | `/filtros` | Facetas disponíveis para o painel de filtros | Pública | 200, 429, 500 |
| `GET` | `/cidades` | Buscar cidade de referência | Pública | 200, 400, 429, 500 |
| `POST` | `/interesses` | Iniciar manifestação de interesse | Pública + `X-Navegador-Id` | 201, 400, 404, 422, 429, 500 |
| `POST` | `/favoritos` | Registrar favorito do navegador *(Fase 2)* | Pública + `X-Navegador-Id` | 202, 400, 404, 429, 500 |
| `POST` | `/comparacoes` | Montar e registrar comparação *(Fase 2)* | Pública + `X-Navegador-Id` | 200, 400, 422, 429, 500 |

**8 endpoints.** Cobertura de RF-01 a RF-07 na seção "Rastreabilidade".

### Os três endpoints que talvez surpreendam

**`GET /filtros` não é conveniência — é o que cumpre o RF-02 sozinho.** O último critério
de aceitação do RF-02 diz que, enquanto D02 não fornecer carroceria, o filtro não é
oferecido. Se a lista de filtros morasse no frontend, cumprir isso seria uma linha
comentada esperando alguém lembrar de descomentar. Vindo do servidor, o filtro aparece no
dia em que o dado existir, sem deploy de frontend.

**`GET /itens/resumos` é o único endpoint que devolve item vendido ou retirado.**
`GET /itens` os esconde porque ninguém deve descobri-los. Mas quem já favoritou um
veículo tem direito de saber o que aconteceu com ele (RF-04, RF-06, RN-09) — e isso não
é a mesma operação que descobrir.

**`POST /comparacoes` existe porque o alinhamento precisa ser do servidor.** O RF-07
exige que toda célula sem valor mostre `Não informado` preservando o alinhamento das
linhas. É regra de apresentação de dados ausentes — exatamente o tipo que se degrada
quando cada cliente a reimplementa.

---

## Contrato de entrada (D02 → D01)

Fora do escopo HTTP desta API, mas é a fonte de tudo que ela devolve.

| Caminho | Como | Frequência |
|---|---|---|
| **Quente** | Eventos `estoque.oferta-incluida`, `estoque.oferta-atualizada`, `estoque.oferta-retirada`, `estoque.disponibilidade-alterada`, via Rebus/RabbitMQ | Ao acontecer — é o que sustenta a meta de 1 hora |
| **Reconciliação** | `GET /ofertas-elegiveis` da API de D02, com client credentials e scope `estoque:integrar` | A cada 15 minutos (QC-04 de D02) |

### O que atravessa e o que não atravessa

A projeção `OfertaElegivel` de D02 traz mais do que o público deve ver. A tradução é
deliberada:

| Campo em `OfertaElegivel` | Vira em D01 | Decisão |
|---|---|---|
| `ofertaId` | `itemId` (identificador próprio de D01) | O item do catálogo é entidade de D01, não a oferta de D02 |
| `veiculo.placa`, `veiculo.chassi` | — | **Não atravessam.** Identificam o veículo em bases de terceiros e não ajudam a decidir (DUX-11) |
| `veiculo.localizacao.cep` | — | **Não atravessa.** Estreita a localização além do necessário para calcular distância até a cidade |
| `veiculo.*` (demais) | `ItemResumo` + `fichaTecnica` | Cada atributo nulo vira uma linha `Não informado`, nunca uma linha omitida |
| `fatos.*.descricao`, `.fonte` | `BlocoFatoPublico.conteudo`, `.fonte` | Texto integral |
| `fatos.*.limitacaoDeclarada` | `BlocoFatoPublico.limitacaoDeclarada` | **Texto literal**, nunca substituído por frase genérica (RF-03) |
| `fatos.*.evidencia` | — | **Não atravessa.** Evidências podem conter dado pessoal; o bucket de D02 é privado por decisão (QC-02 de D02). D01 mostra a fonte em texto, nunca o anexo |
| `fatos.*.atualizadoPor`, `precoOficial.definidoPor` | Só a data | Nome de operador é dado pessoal de funcionário e não tem função na jornada pública (DUX-07) |
| `precoOficial.valorCentavos` | `precoOficialCentavos` | Sem transformação |
| `disponibilidade` + situação da oferta | `statusPublico` | Projeção de dois eixos em quatro estados — ver abaixo |
| Situação, checklist, solicitações | — | **Não atravessam.** O comprador vê status, não processo |

### A projeção de status

```
   D02: situação × disponibilidade                     D01: statusPublico
   ─────────────────────────────────────────────────────────────────────────
   elegivel   × disponivel  ──────────────────────►    disponivel
   elegivel   × reservado   ──────────────────────►    reservado
   qualquer   × vendido     ──────────────────────►    vendido
   em-preparacao | suspensa | retirada ───────────►    indisponivel
```

`indisponivel` **não é um estado novo de disponibilidade** — QA-06 confirma que D02 tem
exatamente três (`disponivel`, `reservado`, `vendido`). É a projeção da *situação da
oferta*, que o comprador não deve ver em detalhe.

---

## Endpoints Detalhados

### `GET /itens` — Listar e buscar

**Propósito:** carregar a tela de resultados com todos os critérios do RF-02.
**Consumido por:** T01 — Resultado do catálogo.

Só devolve `disponivel` e `reservado`. Vendidos, retirados, suspensos e em preparação
nunca aparecem — nem com filtro explícito, porque o filtro não existe.

| Grupo | Parâmetros |
|---|---|
| Paginação | `page`, `pageSize` |
| Texto | `busca` (marca, modelo, versão; mín. 2 caracteres) |
| Atributos | `marca[]`, `modelo[]`, `anoMin`, `anoMax`, `precoMinCentavos`, `precoMaxCentavos`, `quilometragemMax`, `combustivel[]`, `cambio[]` |
| Local | `uf[]`, `cidadeId[]` |
| Referência | `latitude`+`longitude` **ou** `cidadeReferenciaId`, e `raioKm` |
| Ordem | `ordenar` |

**Response 200**

```json
{
  "items": [
    {
      "itemId": "4f2c8a17-3d6b-4e91-a5c0-7b8e1d9f2a34",
      "titulo": "Honda Civic EXL 2.0",
      "marca": "Honda",
      "modelo": "Civic",
      "versao": "EXL 2.0",
      "anoFabricacao": 2021,
      "anoModelo": 2022,
      "quilometragem": 48300,
      "combustivel": "Flex",
      "cambio": "Automático",
      "cor": "Prata",
      "precoOficialCentavos": 8790000,
      "moeda": "BRL",
      "precoAtualizadoEm": "2026-08-12T14:22:05Z",
      "localizacao": { "cidadeId": 3509502, "cidade": "Campinas", "uf": "SP" },
      "distanciaKm": 42,
      "statusPublico": "disponivel",
      "fotoPrincipal": {
        "url": "https://cdn.autotransparencia.com.br/itens/4f2c8a17/frente.jpg",
        "alt": "Honda Civic EXL prata, vista frontal"
      },
      "publicadoEm": "2026-08-06T10:00:00Z",
      "atualizadoEm": "2026-08-14T18:02:11Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 142,
  "totalPages": 8,
  "hasNextPage": true,
  "hasPreviousPage": false,
  "ordenacaoAplicada": "proximidade",
  "localizacaoReferencia": { "tipo": "geolocalizacao", "cidade": "Campinas", "uf": "SP" },
  "sugestoes": []
}
```

**`ordenacaoAplicada` e `localizacaoReferencia` são ecos, e existem por honestidade.** A
ordenação padrão depende de haver localização de referência (DP-08); o cliente não deve
adivinhar qual das duas o servidor usou, nem escrever "Mostrando distâncias a partir de
Campinas/SP" a partir de uma coordenada que ele mesmo mandou.

**Quando `totalCount` é 0**, `sugestoes` traz as ações que devolveriam resultado, **com a
contagem de cada uma**:

```json
{
  "totalCount": 0,
  "items": [],
  "sugestoes": [
    { "campo": "precoMaxCentavos", "rotulo": "Ampliar o preço até R$ 95.000", "valorSugerido": "9500000", "quantidadeEstimada": 12 },
    { "campo": "cambio", "rotulo": "Remover o filtro de câmbio automático", "valorSugerido": null, "quantidadeEstimada": 8 },
    { "campo": "raioKm", "rotulo": "Ampliar o raio para 200 km", "valorSugerido": "200", "quantidadeEstimada": 31 }
  ]
}
```

Sugerir sem contar empurraria o comprador para outro resultado vazio — e "buscas com
resultado ≥ 85%" é métrica do PRD.

**422 em vez de silêncio.** `ordenar=proximidade` ou `raioKm` sem localização de
referência retorna 422. Ordenar por outra coisa sem avisar faria a interface exibir um
rótulo que não corresponde ao que está na tela.

---

### `GET /itens/{itemId}` — Detalhe

**Propósito:** carga **única** da página do veículo. O frontend não compõe essa tela com
várias chamadas.
**Consumido por:** T03 — Detalhe do veículo.

**Response 200** — item disponível, com conteúdo comercial

```json
{
  "itemId": "4f2c8a17-3d6b-4e91-a5c0-7b8e1d9f2a34",
  "titulo": "Honda Civic EXL 2.0",
  "marca": "Honda", "modelo": "Civic", "versao": "EXL 2.0",
  "anoFabricacao": 2021, "anoModelo": 2022, "quilometragem": 48300,
  "statusPublico": "disponivel",
  "avisoStatus": null,
  "precoOficialCentavos": 8790000,
  "moeda": "BRL",
  "precoAtualizadoEm": "2026-08-12T14:22:05Z",
  "localizacao": { "cidadeId": 3509502, "cidade": "Campinas", "uf": "SP" },
  "distanciaKm": 42,
  "fatos": {
    "origem": {
      "tipo": "origem",
      "rotulo": "Origem",
      "apresentacao": "conteudo",
      "conteudo": "Veículo de frota corporativa, único proprietário pessoa jurídica.",
      "fonte": "Contrato de cessão Localiza, 02/2026",
      "limitacaoDeclarada": null,
      "atualizadoEm": "2026-08-10T11:05:02Z"
    },
    "condicao": {
      "tipo": "condicao",
      "rotulo": "Condição",
      "apresentacao": "conteudo",
      "conteudo": "Revisões em concessionária até 40.000 km. Pneus dianteiros trocados em 03/2026.",
      "fonte": "Histórico de manutenção Honda",
      "limitacaoDeclarada": null,
      "atualizadoEm": "2026-08-10T11:07:40Z"
    },
    "historico": {
      "tipo": "historico",
      "rotulo": "Histórico",
      "apresentacao": "limitacao",
      "conteudo": null,
      "fonte": null,
      "limitacaoDeclarada": "Não foi possível obter o histórico de sinistros deste veículo junto às bases consultadas.",
      "atualizadoEm": "2026-08-10T11:09:12Z"
    }
  },
  "fichaTecnica": [
    { "codigo": "ano", "rotulo": "Ano", "valor": "2021/2022", "grupo": "Identificação" },
    { "codigo": "quilometragem", "rotulo": "Quilometragem", "valor": "48.300 km", "grupo": "Uso" },
    { "codigo": "cambio", "rotulo": "Câmbio", "valor": "Automático", "grupo": "Mecânica" },
    { "codigo": "combustivel", "rotulo": "Combustível", "valor": "Flex", "grupo": "Mecânica" },
    { "codigo": "cor", "rotulo": "Cor", "valor": null, "grupo": "Identificação" }
  ],
  "apresentacaoComercial": {
    "titulo": "Honda Civic EXL 2.0 — único dono, revisões em dia",
    "descricao": "…",
    "destaques": ["Único proprietário", "Revisões em concessionária"],
    "fotos": [{ "url": "…", "alt": "Honda Civic EXL prata, vista frontal" }]
  },
  "acoes": { "podeManifestarInteresse": true, "podeFavoritar": true, "podeComparar": true },
  "publicadoEm": "2026-08-06T10:00:00Z",
  "atualizadoEm": "2026-08-14T18:02:11Z"
}
```

Três campos existem para que a interface **não reimplemente regra de domínio**:
`acoes` (quais botões existem), `fatos.*.apresentacao` (qual dos dois formatos renderizar,
sem inferir por `conteudo != null`) e `avisoStatus` (o texto da tarja, pronto).

`"cor": null` na ficha técnica **é o RN-02 em funcionamento**: a linha existe, o valor
não. A interface renderiza `Não informado`. Se o servidor omitisse a linha, o comprador
concluiria que a plataforma não fala de cor — e não que ninguém apurou a cor deste carro.

**Response 200** — item vendido (tela T03-c)

```json
{
  "itemId": "4f2c8a17-3d6b-4e91-a5c0-7b8e1d9f2a34",
  "titulo": "Honda Civic EXL 2.0",
  "marca": "Honda", "modelo": "Civic", "versao": "EXL 2.0",
  "anoFabricacao": 2021, "anoModelo": 2022,
  "statusPublico": "vendido",
  "avisoStatus": "Este veículo foi vendido.",
  "fichaTecnica": [],
  "acoes": { "podeManifestarInteresse": false, "podeFavoritar": false, "podeComparar": false }
}
```

Sem preço, sem fotos, sem fatos, sem ficha (RN-09). **200, não 404**: o item existiu, e a
diferença entre "foi vendido" e "nunca existiu" é exatamente o que o comprador precisa
saber. 404 fica reservado a um `itemId` que nunca correspondeu a nada.

---

### `GET /itens/resumos` — Itens conhecidos pelo navegador

**Consumido por:** T05 — Favoritos, e a `BarraComparacao`.

```
GET /itens/resumos?itemIds=4f2c8a17-…&itemIds=8b1e9d02-…&cidadeReferenciaId=3509502
```

```json
{
  "items": [
    {
      "itemId": "4f2c8a17-3d6b-4e91-a5c0-7b8e1d9f2a34",
      "titulo": "Honda Civic EXL 2.0",
      "statusPublico": "disponivel",
      "acessivel": true,
      "aviso": null,
      "resumo": { "…": "ItemResumo completo" }
    },
    {
      "itemId": "8b1e9d02-4c7f-4a35-9e60-2d1b8f3c5a09",
      "titulo": "Toyota Corolla XEI 2.0",
      "statusPublico": "vendido",
      "acessivel": false,
      "aviso": "Este veículo foi vendido.",
      "resumo": null
    }
  ],
  "naoEncontrados": ["c3a7f918-2e5d-4b81-a034-9f6c1d8e7b52"]
}
```

`acessivel: false` **não é um estado de erro** — é a instrução para não criar link nem
mostrar foto. `naoEncontrados` cobre o `localStorage` antigo: identificadores que nunca
existiram nesta base não geram erro, apenas somem.

---

### `GET /filtros` — Facetas

**Consumido por:** `PainelFiltros`, em T01.

```json
{
  "marcas": [
    { "valor": "Honda", "rotulo": "Honda", "quantidade": 12,
      "modelos": [{ "valor": "Civic", "rotulo": "Civic", "quantidade": 5 }] }
  ],
  "combustiveis": [{ "valor": "Flex", "rotulo": "Flex", "quantidade": 98 }],
  "cambios": [{ "valor": "Automático", "rotulo": "Automático", "quantidade": 76 }],
  "ufs": [
    { "valor": "SP", "rotulo": "SP", "quantidade": 61,
      "cidades": [{ "cidadeId": 3509502, "rotulo": "Campinas", "quantidade": 7 }] }
  ],
  "ano": { "min": 2016, "max": 2024 },
  "preco": { "min": 3990000, "max": 21500000 },
  "quilometragem": { "min": 0, "max": 143000 },
  "filtrosIndisponiveis": [
    { "campo": "carroceria", "motivo": "D02 ainda não fornece este atributo (DP-07 do PRD)." }
  ]
}
```

`filtrosIndisponiveis` é **diagnóstico**, não conteúdo de tela. A interface não o
renderiza como campo desabilitado: um filtro cinza que nunca liga é ruído para o
comprador, e o RF-02 pede que o filtro "não seja oferecido", não que seja exibido
inerte.

As contagens ignoram os filtros já aplicados — são o universo, não a interseção.

---

### `GET /cidades` — Base de referência

**Consumido por:** M02 — Localização de referência, campo de escolha manual.

Devolve municípios da **base inteira**, não apenas os que têm veículo. O comprador mora
onde mora; restringir a busca às cidades com oferta o obrigaria a escolher uma cidade
falsa para ver distâncias verdadeiras.

A resposta **não** traz coordenadas: o cliente não precisa delas, e não entregá-las evita
transformar o endpoint em base geográfica raspável.

Origem dos dados: semente estática de municípios brasileiros (código IBGE, nome, UF,
latitude, longitude), carregada por migration do catalog-service — QA-02 da `ux-spec.md`,
respondendo à questão em aberto do PRD. Sem integração externa, como a restrição técnica
do PRD exige.

---

### `POST /interesses` — Início de interesse

**Consumido por:** T04 — Início de interesse.

**Este é o endpoint que a métrica primária do PRD atravessa.**

**Request**

```json
{
  "itemId": "4f2c8a17-3d6b-4e91-a5c0-7b8e1d9f2a34",
  "contexto": {
    "origem": "busca",
    "precoApresentadoCentavos": 8790000,
    "statusApresentado": "disponivel",
    "filtrosAplicados": "marca=Honda&precoMaxCentavos=9500000",
    "posicaoNoResultado": 3
  }
}
```

Com `X-Navegador-Id` no cabeçalho. **Nenhum campo é dado pessoal** — não há nome,
telefone, e-mail nem mensagem, e `additionalProperties: false` garante que um cliente não
possa enfiar um por conta própria (PD-001).

**Response 201**

```json
{
  "interesseId": "1d8f3b62-7c4a-40e5-9b21-6a0e5c8d3f97",
  "itemId": "4f2c8a17-3d6b-4e91-a5c0-7b8e1d9f2a34",
  "criadoEm": "2026-08-16T13:41:22Z",
  "divergencia": null
}
```

O `interesseId` é a referência que o formulário de D03 envia junto do contato.

#### A ordem importa: o evento sai antes da captação

```
1. Comprador clica "Tenho interesse"
2. POST /interesses  →  registra o contexto e emite `catalogo.interesse-solicitado`
3. A tela revela o formulário de D03, referenciando `interesseId`
4. POST /api/interest/…  (D03)  →  contato capturado
```

A conversão descoberta → interesse é medida no passo 2, no instante da intenção. Assim a
métrica primária é observável **mesmo antes de D03 existir**, e a Fase 1 não fica refém
de outro domínio para saber se funcionou. Se a etapa 4 falhar, o interesse **já foi
registrado** — a interface informa que a operação retomará o contato, e não sugere que
nada aconteceu.

#### Duas divergências, dois tratamentos

| Situação | Resposta |
|---|---|
| O item virou `vendido` ou `indisponivel` entre a exibição e o clique | **422**, com `statusPublico` no corpo |
| O preço mudou, o item continua ofertado | **201** com `divergencia` preenchida |

```json
{
  "type": "https://httpstatuses.io/422",
  "title": "Item não está mais em oferta.",
  "status": 422,
  "detail": "Este veículo foi vendido.",
  "instance": "/api/interesses",
  "traceId": "0af7651916cd43dd8448eb211c80319c",
  "statusPublico": "vendido"
}
```

Preço mudado não invalida o interesse — invalida o silêncio sobre ele. A interface
confirma o interesse e informa o valor vigente. Recusar seria punir o comprador por uma
alteração que ele não fez.

`statusPublico` no corpo do 422 permite escolher entre "Este veículo foi vendido" e "Este
veículo não está mais disponível" sem uma segunda chamada.

---

### `POST /favoritos` — Registro do favorito

**Consumido por:** `BotaoFavoritar`, em T01, T03 e T05. **Fase 2.**

```json
{ "itemId": "4f2c8a17-3d6b-4e91-a5c0-7b8e1d9f2a34" }
```

**Responde 202, não 201, porque nada é criado.** Por PD-002 a lista de favoritos vive no
`localStorage`; o servidor só emite `catalogo.item-favoritado`. O que fica retido é o
registro do evento — item, navegador, instante — e é o que torna calculável a métrica
"navegadores que favoritaram ou compararam e retornam em 7 dias" do PRD. Sem
`navegadorId` essa métrica não existe; com ele, e só com ele, nenhum dado de pessoa é
envolvido.

**Não existe endpoint de desfavoritar.** Remover é operação puramente local. O domain doc
não define evento de remoção, e inventar um criaria escrita anônima sem leitor.

A chamada acontece **depois** de a interface já ter reagido: o coração preenche no clique
e a rede fica fora do caminho da resposta visual. Falha aqui não é erro para o comprador —
o favorito dele existe do mesmo jeito.

---

### `POST /comparacoes` — Comparação

**Consumido por:** T06 — Comparação. **Fase 2.**

```json
{ "itemIds": ["4f2c8a17-…", "8b1e9d02-…", "b7d3f501-…"] }
```

**Response 200**

```json
{
  "comparacaoId": "6e2a9c48-1f75-4d03-8b6a-5c0d7e93f21b",
  "itens": [{ "…": "ItemResumo de cada veículo, na ordem das colunas" }],
  "linhas": [
    {
      "codigo": "quilometragem", "rotulo": "Quilometragem", "grupo": "Uso",
      "celulas": [
        { "itemId": "4f2c8a17-…", "valor": "48.300 km" },
        { "itemId": "8b1e9d02-…", "valor": null }
      ]
    }
  ],
  "itensRemovidos": [
    { "itemId": "b7d3f501-…", "motivo": "vendido",
      "aviso": "O Honda Civic EXL foi vendido e saiu da comparação." }
  ]
}
```

`valor: null` é o `Não informado` do RF-07, **com a linha preservada** — o alinhamento é
o ponto da comparação. `itensRemovidos` cobre o quinto critério do RF-07: o veículo
vendido sai e a alteração é informada, sem erro.

Menos de 2 ou mais de 4 itens comparáveis retorna 422 (RN-05), inclusive quando a
remoção é que derrubou o total abaixo de 2. A `BarraComparacao` impede chegar aqui; o 422
é rede de segurança para `localStorage` adulterado ou aba antiga.

---

## Schemas Principais

### ItemResumo

| Campo | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `itemId` | uuid | Sim | Identificador público do item |
| `titulo` | string | Sim | Comercial do PRD-B ou `Marca Modelo Versão` composto pelo servidor |
| `precoOficialCentavos` | integer | Sim | Preço vigente em D02 |
| `precoAtualizadoEm` | date-time | Sim | Exibido junto do valor (DUX-07) |
| `localizacao` | LocalizacaoPublica | Sim | Cidade e UF. **Sem CEP** |
| `distanciaKm` | integer\|null | Não | `null` sem localização de referência — e nesse caso a interface **oculta**, não mostra `Não informado` |
| `statusPublico` | enum | Sim | `disponivel`, `reservado`, `vendido`, `indisponivel` |
| `fotoPrincipal` | Foto\|null | Não | `null` enquanto o PRD-B não entregar mídia |
| `marca`, `modelo`, `versao`, `ano*`, `quilometragem`, `cor`, `combustivel`, `cambio` | \|null | Não | Cada `null` vira `Não informado` na ficha |

### ItemDetalhe

Modulado pelo status: em `vendido` e `indisponivel` vêm apenas `itemId`, `titulo`,
identificação, `statusPublico`, `avisoStatus` e `acoes`. É por isso que só esses campos
são obrigatórios no schema.

| Campo | Tipo | Descrição |
|---|---|---|
| `avisoStatus` | string\|null | Texto pronto da tarja |
| `fatos` | FatosPublicos\|null | Três blocos, sem evidência e sem autor |
| `fichaTecnica` | AtributoFicha[] | **Linhas com `valor: null` incluídas** |
| `apresentacaoComercial` | ApresentacaoComercial\|null | `null` = bloco **não renderizado**, nem vazio nem "em breve" |
| `acoes` | AcoesDisponiveis | `podeManifestarInteresse` decide se o botão existe |

### BlocoFatoPublico

| Campo | Tipo | Descrição |
|---|---|---|
| `tipo` | enum | `origem`, `condicao`, `historico` |
| `rotulo` | string | Pronto em PT-BR, para o cliente não manter mapa de tradução |
| `apresentacao` | enum | `conteudo`, `limitacao`, `ausente` |
| `conteudo` | string\|null | O que a operação apurou |
| `fonte` | string\|null | Em texto. O arquivo de evidência **nunca** é exposto |
| `limitacaoDeclarada` | string\|null | **Texto literal** de D02, nunca substituído |
| `atualizadoEm` | date-time | Quando. Nunca por quem |

`apresentacao: ausente` não deveria ocorrer — o critério CM-6 de D02 impede a
elegibilidade de uma oferta com bloco vazio e sem limitação. Está no enum porque a
interface precisa de um caminho definido caso ocorra: um dado que falta não pode derrubar
a página do veículo.

### ContextoDescoberta

| Campo | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `origem` | enum | Sim | `listagem`, `busca`, `favoritos`, `comparacao`, `link-direto` |
| `precoApresentadoCentavos` | integer | Sim | O que estava na tela |
| `statusApresentado` | enum | Sim | O que estava na tela |
| `filtrosAplicados` | string\|null | Não | Query string dos filtros. **Nunca inclui coordenadas** |
| `posicaoNoResultado` | integer\|null | Não | Posição do item no resultado |

---

## Códigos de Erro

| Status | Quando | Corpo |
|---|---|---|
| `400` | Requisição malformada, parâmetro inválido | `ProblemDetails` |
| `404` | `itemId` que nunca correspondeu a item algum. **Não** é a resposta de vendido ou retirado | `ProblemDetails` |
| `422` | `ordenar=proximidade` ou `raioKm` sem localização · `latitude`/`longitude` junto de `cidadeReferenciaId` · interesse em item fora de oferta · comparação fora de 2–4 | `ProblemDetails` (+ `statusPublico` no caso do interesse) |
| `429` | Limite por IP excedido. `Retry-After` traz os segundos | `ProblemDetails` |
| `500` | Erro inesperado — `traceId` correlaciona com o log | `ProblemDetails` |

**Não há 401 nem 403 nesta API.** Não é omissão: não há o que autenticar nem o que
autorizar (PD-002). Se um deles aparecer em produção, é bug de configuração de gateway, e
não um caso previsto.

O corpo de 429 é emitido pelo `OnRejected` de `RateLimitingExtensions`, que já produz
RFC 9457 com `traceId` — este contrato apenas documenta o que existe.

---

## Eventos publicados

Fora do escopo HTTP — Rebus/RabbitMQ como CloudEvents, via
`SbaCars.BuildingBlocks.Messaging`.

| Evento | Publicado quando | Já existe em `SbaCars.Contracts`? |
|---|---|---|
| `catalogo.item-publicado` | Uma oferta elegível de D02 se torna descobrível | **Sim** — `Catalogo/V1/ItemPublicadoIntegrationEvent` |
| `catalogo.item-atualizado` | Fatos, preço, status ou apresentação do item mudam | **Sim** — `Catalogo/V1/ItemAtualizadoIntegrationEvent` |
| `catalogo.interesse-solicitado` | `POST /interesses` | **Sim** — `Catalogo/V1/InteresseSolicitadoIntegrationEvent` |
| `catalogo.item-favoritado` | `POST /favoritos` | **Não** — ver AP-04 |
| `catalogo.comparacao-realizada` | `POST /comparacoes` | **Não** — ver AP-04 |

Os três primeiros já têm contrato definido e a assinatura de
`InteresseSolicitadoIntegrationEvent(InteresseId, ItemDoCatalogoId, OcorridoEm)` casa
exatamente com o que `POST /interesses` produz — o que é uma boa notícia: a fronteira
D01/D03 já foi desenhada assim na fundação.

---

## Rastreabilidade

| RF | Endpoints | Telas |
|---|---|---|
| RF-01 Publicação e detalhe | `GET /itens`, `GET /itens/{id}` | T01, T03, T03-b |
| RF-02 Busca, filtros, ordenação, proximidade | `GET /itens`, `GET /filtros`, `GET /cidades` | T01, T01-b, T01-c, M02 |
| RF-03 Apresentação transparente | `GET /itens/{id}` (`fatos`, `fichaTecnica`) | T03 |
| RF-04 Status público e preço | `GET /itens`, `GET /itens/{id}`, `GET /itens/resumos` | T01, T03, T03-c, T05 |
| RF-05 Início de interesse | `POST /interesses` | T03, T04 |
| RF-06 Favoritos | `POST /favoritos`, `GET /itens/resumos` | T05 |
| RF-07 Comparação | `POST /comparacoes`, `GET /itens/resumos` | T06 |

Nenhum RF ficou sem endpoint; nenhum endpoint ficou sem RF.

---

## Decisões resolvidas

Decididas em 16/08/2026. Todas já refletidas no `api-contract.yaml`.

| ID | Questão | Decisão |
|---|---|---|
| QC-01 | Como identificar o navegador sem criar identidade de pessoa? | **Cabeçalho `X-Navegador-Id`, UUID gerado no cliente.** Não é credencial: não autentica, não autoriza e não recupera nada. Sem ele a métrica de retorno em 7 dias do PRD não é calculável |
| QC-02 | O interesse é criado por D01 ou por D03? | **Por D01**, que emite o evento e devolve o `interesseId`. D03 completa com o contato. É o que torna a métrica primária observável antes de D03 existir |
| QC-03 | Item vendido responde 404 ou 200? | **200 com corpo reduzido.** A diferença entre "foi vendido" e "nunca existiu" é informação que o comprador precisa (RN-09) |
| QC-04 | Quem alinha a comparação? | **O servidor.** Alinhamento de dados ausentes é a regra do RF-07, e é exatamente o tipo que se degrada quando cada cliente a reimplementa |
| QC-05 | Como o filtro de carroceria "não é oferecido"? | **`GET /filtros` declara as facetas que existem.** Nada no frontend menciona carroceria; quando D02 fornecer, o filtro aparece sem deploy de frontend |
| QC-06 | Coordenadas de geolocalização são persistidas? | **Não.** Viajam como parâmetro de consulta, são usadas para calcular distância e não entram em banco, log ou `localStorage`. É a superfície mínima de LGPD compatível com DP-04, enquanto a questão jurídica do PRD segue aberta |
| QC-07 | Qual proteção substitui a autenticação? | **Limitador por IP.** O global já cobre tudo; `POST /interesses` opta pelo `sbacars-anonymous-strict`, exatamente como o comentário de `RateLimitingPolicies` antecipava |
| QC-08 | A placa aparece no catálogo? | **Não.** D02 a fornece, mas ela identifica o veículo em bases de terceiros e não ajuda o comprador a decidir |

### O limitador por IP tem um furo conhecido, e ele importa mais aqui

`RateLimitingExtensions` documenta que os contadores são **em memória, por réplica**: com
N réplicas, um cliente pode receber até N vezes o limite configurado. Em D02, atrás de
autenticação, isso é aceitável. Aqui o limitador é a **única** barreira entre a internet
aberta e a escrita anônima.

Não é bloqueante para o MVP — o mesmo arquivo registra que o contador distribuído em
Redis é a tarefa D6 e deixa a costura pronta. Mas é a razão pela qual `POST /interesses`
usa a política estrita e não a global, e é o item a reavaliar antes de escalar
`gateway-public` para mais de uma réplica.

---

## Ajustes de plataforma registrados (não executados)

Nenhum código de `backend/` ou `apps/` foi alterado neste planejamento. Estes itens viram
tasks na etapa de `tsg-flow-task-creator`.

| ID | Ajuste | Onde |
|---|---|---|
| AP-01 | Adicionar rota `catalog-interesse` ao gateway público: `POST /api/catalog/interesses`, com `RateLimiterPolicy: sbacars-anonymous-strict` | `backend/src/Gateways/SbaCars.Gateway.Public/appsettings.json` |
| AP-02 | Adicionar rota `catalog-engajamento`: `POST /api/catalog/{**rest}` para favoritos e comparações, sem política estrita | mesmo arquivo |
| AP-03 | Liberar CORS da origem do `apps/catalog` no gateway público, incluindo o cabeçalho `X-Navegador-Id` | `SbaCars.Gateway.Public` |
| AP-04 | Criar `ItemFavoritadoIntegrationEvent` (`catalogo.item-favoritado`) e `ComparacaoRealizadaIntegrationEvent` (`catalogo.comparacao-realizada`) | `backend/src/Contracts/SbaCars.Contracts/Catalogo/V1/` |
| AP-05 | Criar a aplicação de client credentials do catalog-service com `estoque:integrar`, para a reconciliação com D02 | Configuração do Logto (= AP-04 do `api-contract.md` de D02) |
| AP-06 | Agendar a reconciliação de 15 minutos contra `GET /ofertas-elegiveis` de D02 | catalog-service (= AP-07 do `api-contract.md` de D02) |
| AP-07 | Criar a semente estática de municípios (código IBGE, nome, UF, latitude, longitude) e a migration que a carrega | `backend/src/Catalog/SbaCars.Catalog.Infrastructure/Migrations/` |
| AP-08 | `apiFetch` precisa enviar `X-Navegador-Id` nos `POST` | `apps/catalog/src/shared/api/client.ts` (= AJ-09 do `ux-spec.md`) |

**AP-01 e AP-02 são pré-requisito de qualquer escrita.** A rota `catalog-read` atual
casa apenas `GET`, `HEAD` e `OPTIONS` — sem elas, os três `POST` deste contrato retornam
404 no gateway, e o sintoma (404 em endpoint que existe no serviço) não aponta para a
causa.

O YARP casa a rota mais específica primeiro, então o path literal `/api/catalog/interesses`
de AP-01 prevalece sobre o coringa de AP-02. A ordem no arquivo não importa; a
especificidade, sim.

---

## Próximos passos

| Para | Instrução |
|---|---|
| **Design** | `stitch-briefs.md` — os briefs já refletem os campos deste contrato |
| **Backend** | `tsg-flow-techspec-creator` referenciando `api-contract.yaml` como spec dos endpoints |
| **Frontend** | `tsg-flow-frontend-techspec-creator` — os schemas são a fonte de verdade para os tipos TypeScript |
| **Mocks** | `npx @stoplight/prism-cli mock tasks/prd-catalogo-publico-e-descoberta/api-contract.yaml` |
| **Lint** | `npx @stoplight/spectral-cli lint tasks/prd-catalogo-publico-e-descoberta/api-contract.yaml --ruleset .agents/skills/tsg-flow-contract-creator/rulesets/openapi.yaml --fail-severity=error` |
