# Plano de Fundação do Backend — sbacars

> **Artefato de arquitetura.** Antecede os PRDs da Fase 1. Descreve a base técnica sobre a qual
> as features de D01, D02 e D03 serão implementadas. Não define regra de negócio — define
> fronteiras, contratos técnicos e o que precisa existir antes da primeira feature.

**Status:** `proposto`
**Última revisão:** 2026-08-15 (rev. 6 — Logto como IdP único, Keycloak removido)
**Contexto de entrada:** [`docs/vision.md`](../vision.md), [`context/domain-map.md`](../../context/domain-map.md), Domain Docs D01–D04
**Escopo:** a fundação provisiona os **quatro** serviços de domínio. As features de D01–D03 são da
Fase 1; as de D04 permanecem na Fase 2 conforme a visão — o serviço nasce vazio de regra, mas com
schema, autenticação, roteamento e pipeline prontos.

---

## 1. Objetivo

Entregar a fundação do backend em .NET com **Service-Based Architecture (SBA)**, com os serviços de
domínio já rodando, conectados aos seus respectivos schemas no PostgreSQL, validando o JWT emitido
pelo Logto e autorizando por permissão — mais a mensageria e o storage de objetos já plugados e
exercitados, para que nenhuma feature de negócio precise abrir uma frente de infraestrutura.

O produto é didático, mas a base é production-ready: a simplicidade fica nas regras de negócio,
nunca na arquitetura, na segurança ou na operabilidade.

**Critério de conclusão desta fundação:** com `docker compose up`, os seis processos .NET sobem,
respondem `/health/ready` com dependências verificadas, o backoffice autenticado no Logto chama
um endpoint protegido e recebe 200, um endpoint público responde sem token, um evento publicado por
um serviço é consumido por outro com outbox e idempotência, e um upload por URL pré-assinada
funciona — tudo coberto por testes automatizados no gate de CI.

---

## 2. Estilo arquitetural

### 2.1 Por que Service-Based Architecture

| Característica SBA | Como se materializa aqui |
|---|---|
| Poucos serviços de granularidade grossa | Um serviço por bounded context: quatro, um por Domain Doc |
| Deploy independente por serviço | Dockerfile, pipeline e ciclo de release por serviço |
| Banco de dados compartilhado, logicamente particionado | Uma instância PostgreSQL, um **schema por serviço**, com role de banco própria e sem grant cruzado |
| Camada de API na borda | Dois gateways YARP (roteamento e políticas de borda, sem regra de negócio) |
| Bibliotecas compartilhadas são característica, não violação | `BuildingBlocks` e `Contracts` por referência de projeto |
| Sem transação distribuída | Toda requisição de negócio é resolvida dentro de um serviço, em uma transação ACID |

Os três domínios da Fase 1 já têm linguagem, responsáveis e ciclos de mudança distintos e
documentados — decompor não é antecipação, é refletir a decomposição que já existe. Ao mesmo tempo,
não há requisito de escala, time separado por serviço nem esteira madura que justifique
microsserviços com banco físico por serviço. SBA é exatamente o ponto intermediário: fronteira de
deploy real, custo operacional baixo, consistência forte dentro do serviço.

> **Desvio deliberado da skill `dotnet-architecture`.** O exemplo `microservices.md` do repositório
> exige banco por serviço e contrato como pacote NuGet. Em SBA o banco é compartilhado e
> particionado por schema, e a biblioteca compartilhada é traço definidor do estilo. Mantemos a
> Clean Architecture *dentro* de cada serviço exatamente como a skill define; o que muda é a
> fronteira *entre* serviços. A regra "nenhum serviço lê o schema de outro" continua valendo — e
> aqui ela é imposta pelo banco, com `GRANT`, não por disciplina.

### 2.2 Interior de cada serviço

Clean Architecture conforme a skill `dotnet-architecture`, sem MediatR:

```text
Api ──► Application ──► Domain ◄── Infrastructure
```

- **Domain:** entidades, value objects, invariantes, eventos de domínio, portas. Zero dependência de
  ASP.NET Core, EF Core ou Rebus.
- **Application:** casos de uso, DTOs, validação (FluentValidation), orquestração. Começa com
  **Service Pattern simples**; migra caso a caso para CQRS nativo quando a complexidade real
  justificar (regra da skill, aplicada por caso de uso, nunca por hábito).
- **Api:** controllers finos, contratos HTTP, autenticação/autorização, OpenAPI.
- **Infrastructure:** EF Core, repositórios, Rebus, S3, integrações.

### 2.3 Topologia

```text
                     Logto :3001  (emissor do JWT · console :3002)
                            │ OIDC PKCE                    │ JWKS
                            ▼                              ▼
  catalog SPA :5173 ──► gateway-public :5000 ─────┬──► catalog-service  :5010 ──┐
                        (anônimo + rate limit)    └──► interest-service :5030 ──┤
                                                                                │
  backoffice SPA :5174 ► gateway-backoffice :5001 ┬──► inventory-service :5020 ──┤
                        (JWT obrigatório)         ├──► catalog-service  :5010 ──┤
                                                  ├──► interest-service :5030 ──┤
                                                  └──► purchase-service :5040 ──┤
                                                            (D04 — Fase 2)      ▼
                                                          PostgreSQL :5432  (1 instância)
                                                          ├─ schema inventory  ← svc_inventory
                                                          ├─ schema catalog    ← svc_catalog
                                                          ├─ schema interest   ← svc_interest
                                                          └─ schema purchase   ← svc_purchase

  RabbitMQ :5672   eventos de integração (outbox → exchange → fila por consumidor → inbox)
  MinIO    :9000   mídia do catálogo, documentos do estoque e dossiês de D04 (URLs pré-assinadas)
  Aspire Dashboard :18888   OTLP: traces, métricas e logs correlacionados
```

**Dois gateways, e não um.** O edge público e o edge administrativo têm posturas de segurança
opostas: um aceita tráfego anônimo da internet e precisa de rate limit agressivo; o outro nunca
deveria ver uma requisição sem token. Separar em dois processos torna impossível que uma rota
administrativa vaze por engano no edge público, permite escalar e endurecer cada um de forma
independente e preserva os ports já configurados nos dois SPAs (`runtimeConfig.ts`).

**Gateway não é BFF.** Ele roteia, aplica CORS, rate limit, correlation-id e rejeita o não
autenticado cedo. Não agrega respostas, não compõe chamadas, não conhece regra de negócio. Se
aparecer necessidade de agregação, ela é um sinal de fronteira errada entre serviços, não uma
feature do gateway.

### 2.4 Comunicação entre serviços

**Não há chamada síncrona entre serviços.** Toda dependência entre domínios é resolvida por evento
de integração assíncrono, o que é possível porque as dependências dos Domain Docs são
informacionais:

| Dependência do Domain Doc | Como se resolve |
|---|---|
| D02 → D01: ofertas elegíveis, fatos, preço, disponibilidade | Eventos `estoque.*`; `catalog` mantém sua própria projeção (`item do catálogo`) no schema `catalog` |
| D01 → D03: contexto do item e da descoberta | O contexto viaja no payload da manifestação; `interest` grava um **snapshot** no schema `interest` |
| D02 → D03: situação operacional do veículo | Evento `estoque.disponibilidade-alterada`; `interest` atualiza o snapshot |
| D03 → D04: interesse qualificado | Evento `interesse.qualificado`; **não abre jornada automaticamente** — RN-01 de D04 exige ato do vendedor |
| D02 → D04: fatos, preço e disponibilidade do veículo | Eventos `estoque.oferta-atualizada` e `estoque.disponibilidade-alterada`; `purchase` mantém seu próprio snapshot |
| D04 ↔ D02: **reserva de compra** | Conversa em duas etapas — ver §2.5 |

Isso implementa literalmente as regras de fronteira já escritas: D01 não é segunda fonte de verdade
(RN-01/RN-03 de D01), o preço oficial continua de D02 (RN-06 de D01), D03 preserva a referência
recebida sem virar dono dela (RN-01 de D03) e D04 não altera fato, preço ou disponibilidade
(RN-07 de D04). O snapshot é cópia por valor no momento do evento, não chave estrangeira — o schema
alheio nunca é lido.

Se no futuro uma leitura síncrona for inevitável, o caminho é `HttpClient` tipado com timeout
explícito e `AddStandardResilienceHandler`, autenticado por aplicação máquina-a-máquina do Keycloak
(`client_credentials`). Fica documentado como extensão, não implementado agora.

### 2.5 Reserva: a primeira interação bidirecional do sistema

D01→D03 e D02→tudo são fluxos de mão única: o downstream copia e segue. A reserva de compra (D04
F08) é diferente e merece desenho explícito, porque é o único ponto onde um serviço **pede uma
decisão** a outro e precisa do resultado:

```text
D04  compra.reserva-solicitada  ──►  D02  decide (é o dono da disponibilidade)
                                       │
D04  ◄── estoque.disponibilidade-alterada (reservado)  ─── aceita
D04  ◄── estoque.reserva-recusada                      ─── recusa
D04  compra.reserva-confirmada / jornada segue sem reserva
```

Três regras que saem daí e valem para todo o sistema:

1. **Quem decide é o dono do invariante.** D02 é a autoridade sobre disponibilidade (RN-10 de D04,
   RN-08 de D02). D04 solicita e aguarda; nunca decide reserva localmente, nem "otimisticamente".
   O invariante está decidido: **no máximo uma reserva ativa por veículo**, garantido por índice
   único parcial no schema `inventory` (único sobre o veículo, filtrado pelas reservas ativas) —
   banco, não código de aplicação, e muito menos coordenação entre dois bancos.
2. **Pedido pendente é estado, e estado pendente precisa de timeout.** A reserva dura cinco dias
   úteis (RN-08) e a extensão exige autorização gerencial (RN-09). Isso é um processo de longa
   duração com relógio: exige saga persistida e timeout durável, não um `Task.Delay`.
3. **A recusa é um caminho de negócio, não um erro.** O veículo pode ter sido reservado ou vendido
   entre a solicitação e a decisão. A jornada de D04 precisa de um estado para isso desde o
   primeiro dia.

Consequência para a fundação: a mensageria precisa suportar saga e timeout persistidos. `Rebus` com
`Rebus.PostgreSql` cobre os dois (persistência de saga e de timeouts no mesmo PostgreSQL), o que
reforça a escolha da §6.1 — era o critério que eu não tinha na mesa quando comparamos as
bibliotecas, e é justamente onde CAP e o client nativo ficariam devendo.

**Nada disso é implementado agora.** É desenho registrado para que a Fase 2 não descubra na metade
que a fundação não suporta processo de longa duração.

---

## 3. Mapa de serviços

| Serviço | Domínio | Schema | Role de app (DML) | Role de migração (DDL) | Port |
|---|---|---|---|---|---|
| `inventory-service` | D02 Estoque Curado e Disponibilidade | `inventory` | `svc_inventory` | `own_inventory` | 5020 |
| `catalog-service` | D01 Catálogo e Descoberta | `catalog` | `svc_catalog` | `own_catalog` | 5010 |
| `interest-service` | D03 Interesse e Atendimento | `interest` | `svc_interest` | `own_interest` | 5030 |
| `purchase-service` | D04 Compra Assistida e Financiamento | `purchase` | `svc_purchase` | `own_purchase` | 5040 |
| `gateway-public` | — | — | — | — | 5000 |
| `gateway-backoffice` | — | — | — | — | 5001 |

**Por que criar o serviço de D04 agora, se as features são da Fase 2.** Adicionar um serviço na
fundação custa uma pasta a mais em um template que já existe. Adicionar depois custa mexer em seis
lugares — solution, init do banco, rota do gateway, compose, stack file e pipeline — cada um com
risco próprio. Além disso, provisionar o quarto serviço prova que a fundação generaliza em vez de
estar moldada em torno de três casos. O serviço nasce com schema, migração inicial vazia, health,
JWT e rota; sem entidade, sem endpoint de negócio.

### 3.1 Layout do repositório

```text
backend/
├── SbaCars.sln
├── Directory.Build.props           # TFM net10.0, nullable, warnings-as-errors, LangVersion
├── Directory.Packages.props        # Central Package Management: versão única por pacote
├── .editorconfig                   # regras de estilo aplicadas no gate
├── src/
│   ├── BuildingBlocks/
│   │   ├── SbaCars.BuildingBlocks.Domain/
│   │   ├── SbaCars.BuildingBlocks.Application/
│   │   ├── SbaCars.BuildingBlocks.Persistence/
│   │   ├── SbaCars.BuildingBlocks.Messaging/
│   │   ├── SbaCars.BuildingBlocks.Storage/
│   │   ├── SbaCars.BuildingBlocks.Observability/
│   │   └── SbaCars.BuildingBlocks.Web/
│   ├── Contracts/
│   │   └── SbaCars.Contracts/       # eventos de integração, namespaces .V1
│   ├── Gateways/
│   │   ├── SbaCars.Gateway.Public/
│   │   ├── SbaCars.Gateway.Backoffice/
│   │   └── SbaCars.Gateway.Shared/     # fiação YARP comum aos dois edges — ver §3.3
│   ├── Inventory/
│   │   ├── SbaCars.Inventory.Api/
│   │   ├── SbaCars.Inventory.Application/
│   │   ├── SbaCars.Inventory.Domain/
│   │   ├── SbaCars.Inventory.Infrastructure/
│   │   └── SbaCars.Inventory.Migrator/
│   ├── Catalog/        # mesma estrutura
│   ├── Interest/       # mesma estrutura
│   └── Purchase/       # mesma estrutura (D04 — vazio de regra na fundação)
├── tests/
│   ├── SbaCars.Architecture.Tests/          # fronteiras SBA verificadas em CI
│   ├── SbaCars.Inventory.UnitTests/
│   ├── SbaCars.Inventory.IntegrationTests/
│   ├── SbaCars.Catalog.*/  SbaCars.Interest.*/
│   └── SbaCars.TestKit/                     # fixtures Testcontainers, JWT de teste
└── docker/
    ├── postgres/init/                       # schemas, roles e grants
    └── Dockerfile.<service>
```

Uma solution única com deploy independente por serviço: build, refactor e gate são únicos, mas cada
serviço tem seu Dockerfile e sua imagem. As pastas `1-Services`..`5-Tests` da skill viram
`src/<Serviço>/` porque aqui existem múltiplos serviços; a ordem de referência entre camadas
permanece idêntica e é verificada por teste de arquitetura.

### 3.2 Convenção de nomes (híbrida)

- **Técnico em inglês:** projeto, namespace, camada, `Repository`, `Service`, `Controller`,
  `Handler`, `Options`, `Extensions`.
- **Negócio em português:** entidades, value objects, enums e propriedades que carregam a linguagem
  ubíqua dos Domain Docs — `OfertaCurada`, `Veiculo`, `PrecoOficial`, `DisponibilidadeOperacional`,
  `InteresseQualificado`, `ItemDoCatalogo`, `SolicitacaoDeTestDrive`, `JornadaDeCompraAssistida`,
  `DossieDeAnaliseDeCredito`, `ReservaDeCompra`.
- **Banco:** `snake_case` derivado do nome da entidade — `inventory.oferta_curada`,
  `interest.interesse_qualificado`.
- **Eventos:** mantêm o nome já definido nos Domain Docs (`estoque.oferta-incluida`), com o tipo C#
  em `SbaCars.Contracts.Estoque.V1.OfertaIncluidaIntegrationEvent`.

Nunca traduza um termo do glossário. Se um conceito novo aparecer no código sem estar no Domain Doc,
o Domain Doc é que está desatualizado.

### 3.3 BuildingBlocks — regra de contenção

| Projeto | Contém |
|---|---|
| `.Domain` | `Entity`, `AggregateRoot`, `ValueObject`, `IDomainEvent`, `DomainException` — todas **abstratas ou interfaces**, sem estado próprio além de identidade e coleção de eventos |
| `.Application` | `IUnitOfWork`, `IClock`, `ICurrentUser`, primitivas de paginação, comportamento de validação |
| `.Persistence` | Convenções EF (snake_case, `timestamptz`, schema, histórico de migração), interceptor de outbox, repositório base |
| `.Messaging` | Configuração Rebus, envelope CloudEvents, inbox/idempotência, topologia |
| `.Storage` | `IObjectStorage` com URLs pré-assinadas |
| `.Observability` | OpenTelemetry, `ActivitySource`, convenções de health check |
| `.Web` | JwtBearer + políticas, `IExceptionHandler` + ProblemDetails, correlation-id, rate limit, CORS, OpenAPI |

**Regra:** nada específico de domínio entra em BuildingBlocks, e nada é extraído para lá antes do
**segundo** serviço precisar. BuildingBlocks que vira framework é dívida, não fundação.

**A regra tem um segundo lado, descoberto na A7:** dois consumidores justificam extrair o código,
mas não justificam extrair para BuildingBlocks. A fiação do YARP tem exatamente dois consumidores —
os dois gateways — e a primeira versão da A7 a colocou em `.Web`. Como os quatro `Api` referenciam
`.Web`, todos passaram a carregar `Yarp.ReverseProxy`: um serviço de domínio empacotando a
maquinaria de chamar outro serviço, que é o que a §2.4 proíbe. A extração correta é lateral —
`SbaCars.Gateway.Shared`, ao lado dos seus dois únicos consumidores — e a linha do `.Web` na tabela
acima é a lista fechada do que entra ali, não uma sugestão. Um teste de arquitetura
(`ReverseProxyContainmentTests`) falha o build se o pacote reaparecer fora de `src/Gateways/`.

**A motivação não é evitar repetição, é garantir comportamento idêntico.** Quatro serviços devem
responder o mesmo ProblemDetails, validar o mesmo token, gravar o outbox do mesmo jeito e emitir o
mesmo trace. Tratar BuildingBlocks como "biblioteca do que se repete" é exatamente o que a
transforma em framework interno: código vai para lá por parecer duplicado, não por precisar ser
uniforme. Duas ocorrências parecidas em serviços diferentes muitas vezes devem divergir — e devem
poder divergir.

**Sobre as bases do `.Domain`:** `Entity` carrega identidade e igualdade por identidade;
`AggregateRoot` herda de `Entity` e acumula os eventos de domínio pendentes; `ValueObject` traz
igualdade estrutural; `IDomainEvent` é marcador; `DomainException` é a raiz das falhas de negócio.
Nada além disso. A tentação clássica é colocar `CreatedAt`, `UpdatedBy`, soft delete ou tenant na
base — no momento em que isso acontece, todo agregado dos quatro serviços passa a carregar uma
decisão que talvez só um deles precisasse, e remover depois é breaking change em cascata. Auditoria
e timestamps entram por interceptor de persistência, não por herança.

**Sobre virar pacote NuGet:** é exatamente o caminho de evolução se os repositórios se separarem, e
o desenho já está preparado — são projetos independentes, sem dependência de serviço. O que muda
não é o código, é o custo de mudança: hoje uma alteração em BuildingBlocks atinge os quatro
serviços no mesmo build, o que é a garantia de uniformidade e, ao mesmo tempo, um raio de impacto
grande. Como pacote versionado, cada serviço fixa uma versão e sobe quando quiser — ganha-se
autonomia e perde-se a garantia de que todos estão iguais, além de passar a existir um ciclo de
release interno (feed, versionamento semântico, período de convivência entre versões). Fazer isso
enquanto há um único repositório seria pagar o custo sem receber o benefício.

---

## 4. Persistência

### 4.1 Particionamento e privilégio mínimo

O isolamento entre serviços é imposto pelo PostgreSQL, não por convenção. Script em
`docker/postgres/init/`, e o equivalente versionado para os demais ambientes:

```sql
CREATE SCHEMA inventory AUTHORIZATION own_inventory;

-- app: só DML no próprio schema
GRANT USAGE ON SCHEMA inventory TO svc_inventory;
ALTER DEFAULT PRIVILEGES FOR ROLE own_inventory IN SCHEMA inventory
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO svc_inventory;

-- nenhum GRANT de inventory para svc_catalog / svc_interest: a fronteira é física
REVOKE ALL ON SCHEMA public FROM PUBLIC;
```

Efeito prático: se alguém escrever um `JOIN` cruzando schemas, o teste de integração falha com
erro de permissão do banco. A fronteira arquitetural passa a ser verificável, não uma boa intenção.

### 4.2 EF Core

- Um `DbContext` por serviço, `HasDefaultSchema("<schema>")`, histórico de migração
  `__ef_migrations_history` **dentro do próprio schema**.
- `snake_case` por convenção (validar compatibilidade de `EFCore.NamingConventions` com EF 10 na
  task de setup; se houver defasagem, aplicar a convenção manualmente em `ConfigureConventions`).
- `Guid` v7 como PK, gerado na aplicação com `Guid.CreateVersion7()` — ordenável no tempo, evita
  fragmentação de índice e não depende de round-trip ao banco.
- `DateTimeOffset` → `timestamptz`, sempre UTC, via `IClock` injetado (nunca `DateTime.Now`).
- `QueryTrackingBehavior.NoTracking` como padrão; tracking explícito no caminho de escrita.
- Sem lazy loading. Sem `DbContext` fora da Infrastructure — verificado por teste de arquitetura.
- `EnableRetryOnFailure` no Npgsql para falhas transitórias, com o cuidado de não misturar
  estratégia de retry com transação explícita sem `ExecutionStrategy`.

### 4.3 Migrações

Migração **não roda no startup da API** fora de Development. Cada serviço tem um
`SbaCars.<Serviço>.Migrator` (console) que:

1. conecta com a role **owner** (DDL), separada da role da aplicação (DML);
2. aplica as migrações pendentes e sai com código 0/1;
3. roda como job/init-container antes do rollout da API.

Isso elimina a corrida entre réplicas, impede que a aplicação em produção tenha privilégio de DDL e
torna a migração um passo observável do deploy.

### 4.4 Segredos e configuração

`appsettings.json` guarda apenas defaults não sensíveis. Connection string, credenciais de broker e
chaves de storage vêm de `dotnet user-secrets` em desenvolvimento e de variáveis de ambiente no
container — com um secret manager em produção. Toda seção de configuração é bindada por Options
Pattern com `ValidateDataAnnotations().ValidateOnStart()`: config faltando derruba o processo no
boot, nunca na primeira requisição.

---

## 5. Autenticação e autorização

### 5.0 Um único provedor de identidade: Logto

**Logto em todos os ambientes** — self-hosted no compose local e no Swarm, gerenciado quando
produção existir. O Keycloak sai do desenho.

A revisão anterior mantinha Keycloak local e Logto remoto, e pagava por isso o pior tipo de dívida:
uma diferença de comportamento que só aparece no ambiente onde ela custa caro. Os dois emitem
formatos de claim diferentes — Keycloak entrega realm roles, Logto entrega `scope` de um API
resource — e sem staging (§11.0) o primeiro deploy produtivo seria o primeiro teste real do caminho
Logto. Rodar o mesmo IdP em todo lugar elimina a divergência na origem, em vez de administrá-la.

O momento é o mais barato possível: nenhum código de backend existe ainda, e o único consumidor do
Keycloak é o fluxo OIDC do backoffice, construído para exercitar a mecânica do login.

| | O que se ganha | O que se paga |
|---|---|---|
| Formato de claim | Um só, idêntico em toda parte | — |
| Teste de contrato de claim duplo | Deixa de ser necessário | — |
| Configuração | — | Logto não tem equivalente ao `realm.json`: aplicações, API resource, scopes, papéis e usuários são criados pela Management API |
| Infra local | — | Logto exige um banco PostgreSQL próprio e um passo de *seed* |
| Frontend | — | `oidcConfig.ts` do backoffice repontado: autoridade, `client_id` e o recurso solicitado |

A troca é boa: sai uma divergência de runtime entre ambientes, entra um script de bootstrap
determinístico e versionado. Script de setup é reproduzível; diferença de formato de token é
descoberta em produção.

### 5.1 Configuração do Logto

Tudo abaixo é criado por um **script de bootstrap idempotente** em `infra/logto/`, usando a
Management API — versionado no repositório, executável em base limpa e seguro de reexecutar.

> **Um passo manual é inevitável, por desenho do Logto.** O Logto self-hosted não expõe API para
> criar a primeira aplicação máquina-a-máquina, porque isso equivaleria a auto-conceder acesso
> administrativo. Criar essa aplicação e atribuir a ela o papel *Logto Management API access* é feito
> uma única vez pelo console (`:3002`), e as credenciais vão para `infra/logto/.env`. O passo está
> documentado em `infra/logto/README.md`. Detalhe que custa tempo: o indicador da Management API é o
> identificador lógico `https://default.logto.app/api`, **não** a URL do deployment — a documentação
> do Logto Cloud usa `$TENANT_ENDPOINT/api`, que lá coincide com o indicador e aqui não.

1. **API resource** `https://api.sbacars.app`, com os scopes iguais às permissões da §5.4:
   `estoque:gerenciar`, `estoque:ler`, `catalogo:gerenciar`, `atendimento:gerenciar` e, na Fase 2,
   `compra:gerenciar` e `reserva:estender`. O indicador não precisa resolver em DNS — é
   identificador, não endereço.
2. **Aplicação SPA `backoffice`** (Authorization Code + PKCE), com as redirect URIs de
   desenvolvimento e as do Swarm.
3. **Papéis** `estoque` e `operacao`, cada um concedendo o conjunto de scopes da tabela da §5.4;
   `gerencia` entra na Fase 2 com `reserva:estender`, porque a RN-09 de D04 exige autorização
   gerencial registrada — é a primeira decisão que um operador comum não pode tomar. "Vendedor" não
   vira papel: o Domain Doc de D04 diz que ele é um papel da Operação central, e `operacao` já o
   representa.
4. **Usuários de desenvolvimento** `ana` (operacao) e `bruno` (estoque), com a mesma função dos
   atuais: exercitar a mecânica do OIDC.
5. **Frontend:** `oidcConfig.ts` passa a apontar para a autoridade do Logto e a solicitar o recurso
   `https://api.sbacars.app`, para que o access token venha com `aud` e `scope` corretos — hoje o
   token do Keycloak sai com `aud: account` e não serviria para nenhum backend.

O compose local ganha o serviço `logto` (`ghcr.io/logto-io/logto`, tag fixa — nunca `latest`, e
alinhada periodicamente com a versão gerenciada), um banco `logto` dedicado na mesma instância
PostgreSQL e um passo único de seed (`npx @logto/cli db seed`). O banco é separado do `sbacars`: o
IdP não participa do particionamento por schema da §4.1 e não deve compartilhar ciclo de vida com os
dados de negócio. **Não usamos o `docker-compose.yml` oficial do Logto**, que embute o próprio
Postgres e perde os dados a cada recriação.

> **Armadilha de rede, resolvida em A5.** A variável `ENDPOINT` do Logto define o *issuer* do token.
> Com `ENDPOINT=http://localhost:3001` o SPA no navegador funciona, mas um serviço em container não
> alcança `localhost` — e apontar o serviço para `http://logto:3001` faria a validação falhar,
> porque o issuer do token não bateria com a autoridade consultada.
>
> **Decisão:** `ENDPOINT=http://localhost:3001`, e em A6 os serviços em container configuram
> `MetadataAddress` com a URL interna (`http://logto:3001/oidc/.well-known/openid-configuration`)
> enquanto o issuer validado continua o externo, vindo do próprio documento de descoberta. Preferida
> à alternativa de `ENDPOINT=http://logto:3001` mais `127.0.0.1 logto` no `/etc/hosts` porque não
> exige preparação de máquina por desenvolvedor e funciona em CI sem etapa extra. Serviço rodando
> fora de container (F5 na IDE) usa `Authority` direto, sem `MetadataAddress`.

### 5.2 Validação do JWT nos serviços

Extensão única em `BuildingBlocks.Web`, usada de forma idêntica por todos os serviços e gateways:

- `Authority` por ambiente — a única coisa que varia entre local, dev e produção.
- Descoberta de JWKS automática, com cache e rotação de chave.
- `RequireHttpsMetadata = true` fora de Development.
- Validação de emissor, audience, tempo de vida e assinatura; `ClockSkew = 30s` (o default de 5
  minutos é largo demais). A audience é `https://api.sbacars.app` em todos os ambientes.
- **Não se configura `RoleClaimType`:** a autorização não usa role claim (§5.6).
- **Default deny:** `FallbackPolicy = RequireAuthenticatedUser`. Endpoint público é a exceção
  explícita, marcada com `[AllowAnonymous]` — nunca o contrário.
- `ICurrentUser` (id, nome, permissões) exposto para a Application, para que caso de uso não leia
  `HttpContext`.

Uma **audience única** para toda a superfície de API. Audience por serviço seria cerimônia sem
ganho: é a mesma sessão, o mesmo usuário e o mesmo limite de confiança; a separação real de
permissão é feita por scope e por política.

O gateway de backoffice valida o token e rejeita na borda; o serviço **revalida**. Redundância
proposital: um serviço nunca confia na borda para sua própria autorização.

O gateway de backoffice valida o token e rejeita na borda; o serviço **revalida**. Redundância
proposital: um serviço nunca confia na borda para sua própria autorização.

### 5.4 Políticas

| Política (= permissão) | Concedida a | Aplica em |
|---|---|---|
| `estoque:gerenciar` | `estoque` | escrita em `inventory-service` |
| `estoque:ler` | `estoque`, `operacao` | leitura operacional em `inventory-service` |
| `catalogo:gerenciar` | `operacao` | conteúdo comercial e mídia em `catalog-service` |
| `atendimento:gerenciar` | `operacao` | painel e continuidade em `interest-service` |
| `compra:gerenciar` *(Fase 2)* | `operacao` | jornada, dossiê e proposta em `purchase-service` |
| `reserva:estender` *(Fase 2)* | `gerencia` | extensão de reserva — RN-09 de D04 |
| — | anônimo | `GET` público do catálogo, `POST` de manifestação de interesse |

Os papéis saem direto dos Domain Docs: D02 é da operação central com curadoria da oferta, D01 tem a
operação mantendo conteúdo comercial, D03 tem a operação conduzindo o atendimento, D04 acrescenta a
única decisão que exige gerência.

Repare que a coluna do meio é **onde o papel vira permissão** — e é o único lugar do sistema que
conhece papéis. Endpoint, caso de uso e teste falam apenas a linguagem da primeira coluna.

### 5.5 Superfície anônima

O catálogo público e a captura de interesse são anônimos por definição de produto — o comprador da
Fase 1 não faz login. Isso exige, na base:

- rate limit por IP no `gateway-public` (janela deslizante), mais estrito no `POST` de manifestação,
  com contador no Redis para continuar correto com múltiplas réplicas (§11.7);
- validação de payload rigorosa e limite de tamanho de corpo;
- nenhum dado pessoal em log (o consentimento de RN-03 do D03 é dado sensível);
- CORS restrito às origens configuradas, sem credenciais (bearer token, não cookie).

Quando D01 F06 exigir favoritos persistentes por cadastro, entra uma segunda aplicação SPA
(`catalog`) no Logto, com auto-registro habilitado e um papel de comprador sem nenhum scope
administrativo; a mesma cadeia de validação já suporta isso sem mudança estrutural.

### 5.6 Permissão como moeda da autorização

**O IdP continua dono dos papéis. A aplicação nunca os enxerga.**

```text
Logto: papel ─► scopes do API resource ─► claim scope
                                              │
                                              ▼
                                    ClaimsTransformation ─► claim permission (n×) ─► política ─► endpoint
                                     (BuildingBlocks.Web)
```

Três regras não negociáveis sustentam isso:

1. **Nunca `[Authorize(Roles = "...")]`.** Sempre `[Authorize(Policy = Permissoes.EstoqueGerenciar)]`,
   com a constante sendo a string `estoque:gerenciar`.
2. **`ICurrentUser` expõe permissões, não papéis.** Nenhum caso de uso chama `User.IsInRole`. Um
   teste de arquitetura proíbe as duas construções acima em todo o `src/`.
3. **O nome da permissão é de negócio, não do IdP.** `reserva:estender` descreve o que se pode
   fazer; `gerencia` descreve quem é. A primeira sobrevive a mudança de modelo de identidade.

Com um IdP só, a transformação é fina de propósito: o formato de permissão escolhido, `recurso:acao`,
é deliberadamente o mesmo de um scope OAuth, então normalizar é dividir a string `scope` e projetar
em claims. A camada continua existindo por dois motivos que não dependem do IdP — ela é o ponto onde
a fonte das permissões pode mudar (token → banco) sem tocar em nada, e é o que impede caso de uso e
controller de conhecerem o vocabulário do provedor de identidade.

O **mapa papel → permissões** (a coluna do meio da tabela acima) vive no Logto, que é quem concede
scopes a papéis — mas é criado pelo **script de bootstrap versionado** da §5.1, nunca clicado no
console. Assim a política de acesso continua passando por code review e viajando junto com o deploy,
que era o ganho real de mantê-la em arquivo. Quando a gestão de papéis virar feature, a transformação
troca de fonte e nada mais muda.

#### Custo de trocar para papéis geridos pela aplicação

A pergunta certa não é "quanto custa depois", é "o que precisa ser verdade hoje para que depois seja
barato". Com as três regras acima valendo, migrar para um modelo em que a operação cria os próprios
papéis exige:

| O que muda | Esforço |
|---|---|
| Tabelas `role`, `permission`, `role_permission`, `user_role` num schema de identidade | Novo, contido |
| A `ClaimsTransformation` passa a carregar permissões do banco em vez do token, com cache | Uma classe |
| Telas de administração de papéis | Feature de produto, com PRD próprio |
| **Endpoints, políticas, casos de uso e testes** | **Nenhuma mudança** |

O que ficaria caro é justamente o que as três regras impedem: papel espalhado por atributo de
controller e por `if` de caso de uso. Aí a migração vira varredura em todo o código, com risco de
esquecer um ponto — e um ponto esquecido em autorização é falha de segurança, não bug de tela.

**Recomendação: não construir gestão de papéis agora.** Nada nos Domain Docs pede papel
customizável; o Logto já faz isso bem; e o seguro que torna a migração barata custa
uma classe de transformação, uma tabela de mapeamento em configuração e um teste de arquitetura —
tudo já previsto na Fase A. Construir CRUD de papéis hoje seria adicionar tabelas, telas,
invalidação de cache e superfície de segurança para resolver um problema que ainda não existe.

O gatilho para reabrir a decisão é concreto: quando a operação precisar criar um papel sem abrir o
console do IdP, ou quando a autorização passar a depender de **qual registro**, e não só de qual
ação — o caso descrito a seguir.

#### Autorização por recurso: o eixo que nenhum modelo de papel resolve

A distinção não é sobre localização — localização foi só um exemplo. É sobre o que a pergunta de
autorização precisa saber:

| Tipo | A pergunta | Onde a resposta vive |
|---|---|---|
| Por permissão (o que temos) | "Este usuário pode gerenciar estoque?" | No token. Não depende de nenhum registro. |
| Por recurso | "Este usuário pode gerenciar **este** veículo?" | Nos dados. Depende do registro e da relação do usuário com ele. |

Exemplos plausíveis neste produto, todos ainda não pedidos por nenhum Domain Doc: um operador que só
gerencia veículos de uma praça; um vendedor que só enxerga as jornadas das quais é responsável; uma
gerência que só estende reservas do próprio time.

Vale reparar que isso já está **latente** nos documentos: D03 tem `responsável` no Atendimento e D04
tem `responsável` na Jornada. Hoje o campo é informativo. No dia em que o PO disser "só o
responsável pode atualizar", a autorização deixa de ser respondível por claim.

Por que muda o desenho: a permissão não cabe no token, porque depende de dado que só existe depois
de carregar o registro. No ASP.NET Core isso é `IAuthorizationService.AuthorizeAsync(user, recurso,
requisito)`, avaliado por registro — e o modelo por permissão continua valendo como primeira porta
("pode gerenciar estoque?"), com a checagem de recurso vindo depois ("pode gerenciar este?"). Os
dois convivem; um não substitui o outro.

O caso difícil não é o registro único, é a **listagem**: não se autoriza mil linhas uma a uma, então
a regra vira filtro na consulta. Aí a autorização deixa de ser um atributo no controller e passa a
ser parte do repositório, com o risco clássico de uma consulta esquecer o filtro. Quando esse dia
chegar, a resposta é centralizar o filtro em um único ponto de acesso a dados e cobri-lo com teste —
nunca replicar a condição em cada consulta.

Nada disso entra agora. Está registrado para que a decisão de hoje seja tomada sabendo qual é o
limite dela.

Há um limite físico que também deve disparar a revisão: permissão viaja no token. Dezenas de
permissões por usuário incham o JWT e o header de toda requisição. Quando isso incomodar, a
transformação passa a carregar do banco com cache — e nada além dela muda.

### 5.7 Dado sensível: o que D04 obriga a decidir cedo

D03 já trata dado pessoal (nome, contato, consentimento). D04 eleva a categoria: CPF, endereço,
renda declarada e documentos comprobatórios, sob a RN-12 e a LGPD. Três desses requisitos custam
pouco agora e são caros de retrofitar depois, então entram na fundação mesmo com D04 vazio:

- **Trilha de auditoria de acesso.** Quem leu o dossiê de quem, e quando. Um dado sensível lido sem
  registro é um vazamento que ninguém consegue investigar. A tabela de auditoria e o interceptor que
  a alimenta ficam em `BuildingBlocks.Persistence` desde a Fase A, usados por D03 e, depois, por D04.

  > **Pendência aberta em A4b, fechada em A6b.** Auditar leitura em EF Core exige interceptar a
  > materialização da entidade, e não é possível gravar no banco de dentro desse ponto — o reader
  > ainda está aberto na conexão. A implementação bufferiza as leituras por `DbContext` e as grava
  > no próximo `SaveChanges` ou em um flush explícito. Consequência: uma operação **puramente de
  > leitura** não gerava linha de auditoria se ninguém desse flush — e abrir a tela de um dossiê é
  > exatamente isso. O `Repository.FindAsync` já fazia o flush, mas consulta LINQ ad hoc fora dele
  > perdia o registro.
  >
  > **A correção, em A6b:** um `SensitiveDataAuditFlushMiddleware` em `BuildingBlocks.Web`,
  > registrado depois de `UseExceptionHandler` e antes de `UseSbaCarsAuth`, que flusha em um bloco
  > `finally` ao redor do resto do pipeline — cobrindo tanto a resposta bem-sucedida quanto a
  > requisição que termina em exceção, porque o `finally` roda nos dois casos antes de o handler de
  > exceção converter a falha em ProblemDetails. O middleware não conhece EF Core nem `DbContext`:
  > depende só de `ISensitiveDataAuditFlusher` (`BuildingBlocks.Application`), uma abstração de uma
  > operação — `FlushAsync` — que `BuildingBlocks.Persistence` implementa por `DbContext` e cada
  > serviço registra em seu `Add<Serviço>Infrastructure` com
  > `AddSbaCarsSensitiveDataAuditFlusher<TContext>()`. Como o `DbContext` é scoped por requisição,
  > o flusher resolvido pelo middleware é sempre o mesmo contexto que a requisição usou para ler.
  > Falha ao gravar a auditoria é capturada e registrada em log de erro com o `traceId` da
  > requisição, mas nunca propagada: uma auditoria quebrada não pode transformar uma resposta já
  > bem-sucedida em 500, mas também não pode desaparecer em silêncio — vira um incidente operável
  > via log, não um gap mudo. Os quatro serviços já chamam `AddSbaCarsSensitiveDataAuditFlusher`
  > (hoje um no-op, porque nenhum dos quatro tem `ISensitiveDataEntity` ainda), então nenhum serviço
  > futuro precisa lembrar de ligar o mecanismo no dia em que introduzir sua primeira entidade
  > sensível — só marcar a entidade e passar o interceptor para o `DbContext`.
  >
  > **O que continua descoberto, por natureza da abordagem — não fechado por A6b:** projeção com
  > `Select` anônimo e SQL cru lido como escalar não materializam a entidade e, portanto, nunca
  > passam pelo `IMaterializationInterceptor` nem geram auditoria, flush ou não. Isso está
  > documentado no código (`SensitiveDataAuditInterceptor`) e continua sendo responsabilidade de
  > quem escrever esse tipo de consulta sobre uma entidade sensível auditar o acesso manualmente.
- **Sanitização em log, trace e evento.** A regra "nenhum dado pessoal em log" já existia para D03;
  com CPF e renda ela vira mecanismo, não recomendação: atributo de marcação nos DTOs sensíveis e um
  processador que os remove antes de sair para o exportador OTLP. Vale também para o **payload dos
  eventos de integração** — evento é dado em trânsito e persistido no outbox.
- **Retenção e expurgo.** Prazos definidos por categoria:

  | Dado | Prazo | Contado a partir de |
  |---|---|---|
  | Jornada de compra, dossiê, documentos (D04) | **6 anos** | encerramento da jornada |
  | Interesse, contexto do comprador, atendimento (D03) | **1 ano** | encerramento do atendimento |

  Os 6 anos têm base legal de guarda para defesa em processo judicial; 1 ano é retenção operacional.
  Duas coisas que costumam ser esquecidas: o prazo conta do **encerramento**, não da criação; e o
  expurgo precisa suportar **suspensão por litígio** — registro sob disputa não é apagado pelo
  relógio. Revogação de consentimento (RN-03 de D03) apaga antes do prazo, não depois.

  Prazos diferentes por serviço funcionam sem coordenação justamente por causa da §2.4: D04 guarda um
  **snapshot** do contexto que recebeu, não uma referência ao registro de D03. Expurgar o interesse
  ao fim de um ano não deixa a jornada de compra com referência pendurada. É um benefício concreto da
  cópia por valor que só aparece quando as políticas de retenção divergem.

**Criptografia de coluna fica fora do escopo:** controle de acesso mais auditoria bastam por
decisão do PO. A decisão é defensável, e vale registrar exatamente o que ela deixa descoberto:
auditoria responde "quem leu", não impede quem tem acesso ao banco ou a um backup de ler tudo. Com
guarda de 6 anos, existirão backups antigos contendo CPF e renda por muito tempo — então duas
compensações passam a ser obrigatórias, e são baratas: **backup criptografado** e **acesso ao backup
restrito e auditado**. O schema `purchase` nasce sabendo que essas colunas existirão, para que uma
eventual reversão da decisão não force migração destrutiva.

O **bucket separado com política própria** para documentos do dossiê continua valendo (§7), com
regra de ciclo de vida coerente com os 6 anos.

---

## 6. Mensageria

### 6.1 Escolha e licença

**Rebus (MIT) sobre RabbitMQ**, pacotes `Rebus`, `Rebus.RabbitMq` e `Rebus.PostgreSql`.

A escolha foi decidida por três critérios, nesta ordem:

1. **Longevidade da licença.** MassTransit v9 é comercial e a v8 (Apache-2.0) tem fim de manutenção
   no final de 2026 — adotá-la em agosto de 2026 seria assumir uma dependência de infraestrutura
   crítica com meses de vida útil. Rebus é MIT e mantido desde 2012.
2. **Aderência ao desenho.** Handlers são `IHandleMessages<T>`, resolvidos por tipo e por DI — o que
   a skill `dotnet-architecture` exige e o oposto de roteamento por lookup nominal.
3. **O mecanismo difícil já resolvido.** `Rebus.PostgreSql` traz outbox transacional real
   (`Outbox(o => o.StoreInPostgreSql(...))` e `UseOutbox(NpgsqlConnection, NpgsqlTransaction)`),
   que é justamente a parte cuja implementação artesanal erra de forma sutil e cara.

Alternativas avaliadas e descartadas: **CAP** (MIT, outbox e inbox prontos, integração com EF mais
ergonômica — perde por roteamento via string e por mais comportamento implícito em background);
**RabbitMQ.Client puro** (aprendizado máximo, mas nos torna donos de recuperação de conexão, ciclo
de channel, prefetch, DLX com backoff, serialização e propagação de span — infraestrutura não
diferenciada onde frameworks levaram anos corrigindo bugs); **MassTransit v9 comercial** (melhor DX
do ecossistema, custo recorrente injustificável aqui).

Todas as opções livres de mensageria no .NET são projetos de uma a duas pessoas — o ecossistema com
time grande foi exatamente o que fechou. Por isso publicação e consumo continuam atrás de
`IIntegrationEventPublisher` e de consumidores próprios em `BuildingBlocks.Messaging`: a troca de
biblioteca precisa ser um projeto, não um refactor no domínio.

### 6.2 Outbox transacional

Sem outbox, `SaveChanges` e `Publish` são duas falhas independentes: ou se perde evento, ou se
publica evento de transação revertida. Com Rebus:

1. O agregado registra eventos de domínio em memória.
2. O caso de uso abre uma `RebusTransactionScope` e enlista nela a conexão e a transação do próprio
   `DbContext` (`GetDbConnection()` / `CurrentTransaction.GetDbTransaction()` como tipos Npgsql).
   As mensagens de saída são gravadas na tabela de outbox **pela mesma transação** que grava o
   agregado.
3. O forwarder do Rebus lê o outbox e publica no broker depois do commit.

Consequência: ou o fato de negócio e o evento existem juntos, ou nenhum dos dois existe.

O enlace conexão/transação é o único glue explícito da escolha e vive em um único lugar —
`BuildingBlocks.Persistence` expõe um `IUnitOfWork` que abre a transação, cria a scope e completa as
duas na ordem certa. Nenhum caso de uso escreve esse acoplamento à mão.

A tabela de outbox fica **no schema do próprio serviço** (`inventory.outbox`, etc.), o que mantém a
regra de particionamento intacta e faz a transação ser local por construção.

### 6.3 Entrega e idempotência

- Entrega é **at-least-once**. Rebus não traz inbox, então a idempotência é nossa: tabela
  `inbox_message` (`message_id`, `consumer`, `processed_at`) com chave única, verificada por um
  step do pipeline de entrada em `BuildingBlocks.Messaging`. Mensagem repetida é descartada e
  contabilizada. Onde o consumidor for naturalmente idempotente por chave de negócio (upsert de
  projeção pelo id da oferta), a tabela é rede de segurança, não o mecanismo principal.
- Topologia RabbitMQ: exchange por tipo de evento (`estoque.oferta-incluida`), durável; fila por par
  consumidor/evento (`catalog.estoque.oferta-incluida`).
- Retry: política do Rebus com `maxDeliveryAttempts` e second-level retries habilitados — a falha
  persistente reaparece como `IFailed<T>`, que é onde decidimos entre descartar, compensar ou
  encaminhar. Esgotado, vai para a error queue nomeada por serviço (`inventory.error`), monitorada:
  mensagem envenenada é incidente, não silêncio.
- Envelope CloudEvents com `traceparent` propagado, para que o trace atravesse o broker.

### 6.3.1 CloudAMQP: o que muda por ser gerenciado com TLS

Plano de desenvolvimento: **Loyal Lemming — 2M mensagens/mês, limite de 40 conexões.** Os dois
números são restrições de arquitetura, não detalhes de fatura.

**Orçamento de conexões.** Rebus abre de uma a duas conexões por processo (confirmar na B1;
orçamos duas). Só os quatro serviços de domínio falam com o broker — gateways não:

| Cenário | Conexões |
|---|---|
| 4 serviços × 1 réplica × 2 | 8 |
| 4 serviços × 2 réplicas × 2 | 16 |
| Durante rolling update `start-first` (réplica velha + nova coexistem) | até 32 |
| 4 serviços × **3 réplicas** × 2, durante deploy | **48 — estoura o plano** |

Conclusões que viram regra: **duas réplicas por serviço é o teto do ambiente de desenvolvimento**, o
deploy é serviço a serviço e não simultâneo nos quatro, e há alerta em 30 conexões. Concorrência se
regula por `prefetch`, nunca abrindo mais conexões.

**Orçamento de mensagens.** 2M/mês são cerca de 66 mil por dia — folgado para desenvolvimento, com
duas ressalvas. A primeira é que fan-out multiplica: um `estoque.oferta-atualizada` consumido por
três serviços não é uma mensagem, e o desenho de topologia precisa ser lido com isso em mente. A
segunda é que **retry ilimitado queimaria a cota inteira em horas** — nossa política tem
`maxDeliveryAttempts` com error queue no fim, e isso deixa de ser só higiene operacional para virar
proteção de cota. Alerta de consumo em 70% do mês.

**Demais cuidados:**

- **Conexão `amqps://` com validação de certificado ligada.** Desligar validação "para funcionar" é
  o erro clássico aqui e anula o TLS; se falhar, o problema é cadeia de certificados, não a
  validação.
- **Sem depender de plugin.** Nossa política de retry usa timeouts persistidos no PostgreSQL, não o
  plugin de delayed message — o que mantém o desenho válido em qualquer plano.
- **Heartbeat e reconexão** configurados explicitamente: broker gerenciado derruba conexão ociosa, e
  reconexão silenciosa é o comportamento esperado, não um incidente.
- O RabbitMQ local do compose e o dos Testcontainers continuam sem TLS; a diferença é configuração,
  e o teste que valida a string de conexão do ambiente remoto é de configuração, não de integração.

### 6.3.2 Retenção de outbox e inbox

Expurgo a cada **7 dias**, por job com bloqueio para rodar em uma réplica só.

O prazo do inbox é o que importa para a correção: ele precisa ser **maior que a maior janela
possível de reentrega**, senão uma mensagem antiga reentregue depois do expurgo deixa de ser
reconhecida como duplicada e o efeito acontece duas vezes. Sete dias são folgados contra qualquer
retry nosso — e essa relação precisa ser reavaliada se a política de retry mudar.

O Swarm não tem cron. O job roda como `IHostedService` dentro do serviço, com **advisory lock do
PostgreSQL** para garantir que só uma réplica execute — sem Redis, sem agendador externo, e usando
um mecanismo que o banco já oferece.
- **A verificar na Fase B:** instrumentação OpenTelemetry do Rebus. Se não houver pacote oficial
  adequado, os spans de publicação e consumo entram como step de pipeline próprio — a correlação
  ponta a ponta é requisito, não opcional.

### 6.4 Contratos

`SbaCars.Contracts` é a **linguagem publicada** entre serviços — hoje, na prática, só integração
assíncrona, porque não existe chamada síncrona entre serviços. Contém apenas `record`s de evento,
versionados por namespace (`.V1`). Se algum dia surgir um cliente HTTP tipado entre serviços, os
DTOs de request/response dele moram aqui também, pelo mesmo motivo: é o que atravessa a fronteira.

Evolução é **aditiva**: campo novo é opcional; mudar significado de campo existente é breaking change
e exige `.V2` com período de convivência. Nunca entidade de domínio, nunca `DbContext`, nunca tipo
que só faça sentido dentro de um serviço.

Eventos já definidos pelos Domain Docs, criados como contrato nesta fundação (sem publicador de
negócio ainda):

```text
estoque.oferta-incluida · estoque.oferta-atualizada · estoque.oferta-retirada
estoque.disponibilidade-alterada
catalogo.item-publicado · catalogo.item-atualizado · catalogo.interesse-solicitado
interesse.manifestado · interesse.qualificado · atendimento.iniciado · atendimento.atualizado
testdrive.solicitado · testdrive.agendado
```

Os eventos de D04 (`compra.*` e `financiamento.*`, quinze no Domain Doc) entram como contrato na
Fase 2, com uma exceção: `compra.reserva-solicitada` e a resposta de D02 são desenhadas junto com a
saga da §2.5, porque definem uma capacidade da fundação e não apenas um payload.

### 6.5 Prova da fundação

Um evento técnico `foundation.ping`, publicado por `inventory` e consumido por `catalog`, com teste
de integração assertando outbox → broker → inbox → idempotência em reentrega. Exercita a
infraestrutura inteira sem inventar regra de negócio, e é removido quando o primeiro evento real
existir.

---

## 7. Storage de objetos

S3 gerenciado em produção; MinIO no compose local. `AWSSDK.S3` com `ForcePathStyle` e `ServiceURL`
configuráveis — o mesmo código roda contra os dois, e a diferença entre ambientes é só configuração
vinda de segredo (§11.3).

- Abstração `IObjectStorage`: `CreateUploadUrlAsync`, `CreateDownloadUrlAsync`, `DeleteAsync`.
- **URL pré-assinada nos dois sentidos:** o binário nunca trafega pela API. O SPA faz `PUT` direto
  no storage com URL de vida curta e o download usa `GET` pré-assinado. A API fica fora do caminho
  do byte — o que muda completamente o perfil de memória, timeout e custo dos serviços.
- Buckets: `sbacars-catalog-media` (fotos e mídia de D01 F04), `sbacars-inventory-docs` (documentos
  e evidências de D02 F03) e, na Fase 2, `sbacars-purchase-dossier` (comprovantes de D04 F03).
  Privados, sem leitura anônima, CORS restrito à origem do SPA.
- O bucket de dossiê é o único com dado sensível de pessoa física: bucket separado desde já, com
  política, criptografia e janela de URL próprias, e acesso sempre passando pela trilha de auditoria
  da §5.7. Separar depois significaria migrar objeto e reescrever chave.
- **Regra de ciclo de vida** coerente com a guarda de 6 anos: transição para classe de armazenamento
  mais barata depois dos primeiros meses e expiração ao fim do prazo, com a mesma exceção de
  suspensão por litígio que vale no banco. Sem isso, "guardar 6 anos" vira custo crescente e
  indefinido em vez de política.
- No PostgreSQL só metadado: chave, content-type, tamanho, checksum, quem enviou, quando. Nunca
  bytes.
- Restrições declaradas na base: tipos MIME permitidos, tamanho máximo, verificação de checksum.
  Moderação e consentimento de foto (risco levantado em D01) são regra de negócio da feature, não
  desta fundação.

---

## 8. Observabilidade e prontidão

| Concern | Decisão |
|---|---|
| Tracing | OpenTelemetry com instrumentação de ASP.NET Core, HttpClient e Npgsql; spans de publicação/consumo do Rebus (pacote oficial se houver, senão step de pipeline próprio); `ActivitySource` por serviço; exportação OTLP |
| Correlação ponta a ponta | `traceparent` W3C do SPA (skill `react-observability`) → gateway → serviço → broker → consumidor |
| Métricas | Contadores e histogramas de requisição, consumo de mensagem e latência de banco, via OTel |
| Logs | `ILogger` estruturado com `TraceId` e `CorrelationId`; **nenhum dado pessoal do comprador em log** |
| Backend local | Aspire Dashboard como coletor OTLP (traces, métricas e logs em um lugar, sem stack de observabilidade completa) |
| Health | `/health/live` (self), `/health/ready` (Postgres, RabbitMQ, S3, JWKS do Logto), `/health/startup`; gateways agregam |
| Erros | `IExceptionHandler` global + ProblemDetails RFC 9457, com `traceId` e sem stack trace |
| Resiliência HTTP | `AddStandardResilienceHandler` nas rotas do gateway, timeout explícito |
| Runtime | Graceful shutdown, `RequestTimeouts`, `ForwardedHeaders` atrás do gateway |
| OpenAPI | `Microsoft.AspNetCore.OpenApi` por serviço + UI; documento versionado no repositório e comparado no gate para detectar breaking change |

---

## 9. Testes e gate

| Camada | Ferramenta | Alvo |
|---|---|---|
| Unitário | xUnit + AwesomeAssertions + NSubstitute | Domain e Application |
| Integração | `WebApplicationFactory` + Testcontainers (`postgres:18`, `rabbitmq:4.3`, MinIO) | Repositório, migração, consumidor, storage |
| Autenticação (cadeia real) | Logto em Testcontainers + PostgreSQL, provisionado pelo **mesmo** script de bootstrap da §5.1 | Token de verdade: audience, scopes, papéis e políticas — poucos testes, um fixture compartilhado |
| Autorização (volume) | `TestKit` com JWT assinado por chave de teste e JWKS em memória | Casos de política, sem custo de subir o Logto. Passa a valer mais aqui: o Logto exige banco e seed, então o fixture real é caro e fica reservado para a validação da cadeia |
| Arquitetura | NetArchTest/ArchUnitNET | Domain sem EF/ASP.NET; `DbContext` só na Infrastructure; nenhum serviço referenciando projeto de outro serviço |
| Contrato de evento | Snapshot de schema dos records de `Contracts` | Mudança breaking em evento quebra o build |

Os testes de arquitetura são o que impede a erosão: em SBA, a fronteira entre serviços é a única
coisa segurando o desenho, e uma referência de projeto indevida a dissolve em silêncio.

`scripts/ai-flow/gate.sh` (skill `tsg-flow-gate-creator`, referência `gate.dotnet.sh`) passa a cobrir
backend e frontend: `dotnet format --verify-no-changes`, `dotnet build -warnaserror`,
`dotnet test`, mais `npm run typecheck/test/build` já existentes.

---

## 10. Ambiente local

`docker-compose.yml` estendido, com perfis para não obrigar a subir tudo:

| Serviço | Imagem | Perfil |
|---|---|---|
| logto | `ghcr.io/logto-io/logto:<tag fixa>` (`:3001` OIDC, `:3002` console) | infra |
| logto-bootstrap | job único: `db seed` + script de provisionamento da §5.1 | infra |
| postgres | `postgres:18` (+ init de schemas/roles + banco `logto`) | infra |
| rabbitmq | `rabbitmq:4.3-management-alpine` | infra |
| minio | `minio/minio` (+ job de criação de bucket) | infra |
| aspire-dashboard | dashboard OTLP standalone | infra |
| os 6 processos .NET (4 serviços + 2 gateways) | Dockerfiles do repositório | backend |

Some-se `valkey/valkey:8.1-alpine` ao perfil `infra` como equivalente local do Redis gerenciado
(mesmo protocolo, licença BSD-3). Em produção, broker, cache e storage são gerenciados e só o
PostgreSQL e os processos .NET rodam no cluster — o compose local existe para reproduzir as
dependências, não a topologia de produção.

`docker compose --profile infra up` para desenvolver com F5 na IDE; `--profile backend` para o
ambiente completo. Versões de imagem seguem a tabela da skill `dotnet-dependency-config`, e os
Testcontainers usam as **mesmas majors** — divergência entre local e teste é bug que só aparece em
produção.

O serviço `keycloak` sai do compose, junto com `infra/keycloak/`. O que o substitui não é
equivalente ponto a ponto: o Logto guarda estado em banco e é provisionado por script, então a
fonte de verdade deixa de ser um JSON importado no boot e passa a ser o bootstrap idempotente da
§5.1. A disciplina muda de "exportar o realm depois de mexer na console" para "mudar o script e
reexecutar" — que é mais confiável, porque o script roda em CI e o export manual depende de alguém
lembrar.

Dockerfiles: multi-stage, runtime `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled`, usuário
não-root, sem shell na imagem final, `.dockerignore` cobrindo `bin/`, `obj/`, `node_modules/`.

---

## 11. Deploy e ambientes (Docker Swarm)

### 11.0 Ambientes

Dois ambientes, sem staging: **local** (compose, com Logto, RabbitMQ e MinIO em container) e **dev**
(Swarm, com CloudAMQP, Redis, S3 e Logto self-hosted). Produção será construída depois que os
recursos planejados estiverem prontos, reaproveitando o mesmo stack file com outra configuração.

Sem staging, cada diferença entre ambientes vira risco que só aparece no primeiro deploy produtivo —
razão pela qual o mesmo IdP roda em todo lugar (§5.0) e a audience é a mesma string em toda parte
(§5.2). O que sobra de diferente entre local e dev é o que não dá para eliminar: broker gerenciado
com TLS, storage gerenciado e cache gerenciado. Todos ficam cobertos por configuração validada no
boot (§4.4), não por código condicional.

Quando produção existir, a decisão a tomar é se o Logto continua self-hosted no Swarm ou passa para
o gerenciado. As duas opções valem, e a escolha não muda código: o token é o mesmo, muda a
autoridade.

### 11.1 Divisão entre gerenciado e cluster

| Componente | Onde roda | Consequência para a base |
|---|---|---|
| Serviços .NET e gateways | Swarm | Imagem por serviço em registry acessível pelos nós; réplicas independentes |
| PostgreSQL | Swarm | Estado crítico sob nossa responsabilidade operacional — ver §11.4. Guarda também saga e timeouts do Rebus (§2.5) |
| RabbitMQ | Gerenciado — **CloudAMQP com TLS** | `amqps://`, orçamento de conexões por plano, sem plugin — ver §6.3.1 |
| Redis | Gerenciado | Habilita rate limit distribuído e cache de leitura — ver §11.7 |
| S3 | Gerenciado | Nenhuma mudança de código: `IObjectStorage` já abstrai; MinIO fica só no compose local |
| Identidade | **Logto self-hosted** no Swarm (gerenciado é opção futura) | Precisa de banco próprio, volume e do bootstrap da §5.1 no pipeline |

O critério: middleware sem valor diferenciado sai do nosso escopo operacional; o estado que é o
coração do produto fica onde temos controle e backup próprio.

**Registry:** Docker Hub e GitHub Container Registry. Dois detalhes operacionais que costumam custar
um deploy para serem descobertos: o `docker stack deploy` precisa de `--with-registry-auth` para
propagar a credencial aos nós, senão eles não conseguem puxar imagem privada; e o Docker Hub aplica
limite de pull por origem, o que atinge as imagens base de infraestrutura (`postgres`, `rabbitmq`,
`valkey`) em pipeline que reconstrói com frequência — o runtime .NET vem do `mcr.microsoft.com` e
está fora desse limite.

### 11.2 Rede, publicação de porta e TLS

Uma overlay network interna (`sbacars-net`). **Apenas os dois gateways publicam porta**; os três
serviços de domínio são alcançáveis só pela overlay, resolvidos por DNS interno do Swarm
(`tasks.inventory-service`). Isso transforma a fronteira arquitetural em fronteira de rede: o SPA
não consegue pular o gateway nem por engano nem por curiosidade.

TLS termina antes do Swarm (load balancer ou proxy de borda). Os serviços rodam HTTP na overlay com
`ForwardedHeaders` configurado, e `RequireHttpsMetadata = true` continua valendo para o metadata do
Logto, que é tráfego de saída.

O routing mesh do Swarm distribui entre réplicas — o que torna qualquer estado em memória do
processo (rate limit local, cache local, chave de Data Protection) incorreto por construção assim
que houver mais de uma réplica.

### 11.3 Segredos e configuração

Segredo em variável de ambiente vaza em `docker inspect`, em log de orquestrador e em crash dump.
Usamos **Docker Swarm secrets**, montados como arquivo em `/run/secrets/`, lidos nativamente pelo
.NET:

```csharp
builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);
```

O nome do arquivo é a chave de configuração — `ConnectionStrings__Inventory`,
`Rabbit__Password`, `S3__SecretKey` — então o binding por Options Pattern com `ValidateOnStart`
funciona igual em desenvolvimento (user-secrets) e em produção (arquivo montado), sem `#if`.

### 11.4 PostgreSQL no Swarm — o ponto sensível

Em SBA o banco compartilhado é característica, não defeito; mas ele concentra o estado dos três
serviços, e agora está sob nossa operação em vez de um serviço gerenciado. Isso torna obrigatórios,
como parte da fundação e não como melhoria futura:

- **Placement fixo:** `deploy.placement.constraints` prendendo o serviço ao nó que tem o volume, ou
  volume em storage de rede. Sem isso, um reschedule sobe um Postgres vazio.
- **Backup automatizado com restore testado.** Se a infraestrutura do servidor já fornece snapshot
  de volume, isso cobre **uma** parte do problema: recuperação do banco inteiro até o instante do
  snapshot. Não cobre restaurar apenas o schema `interest` sem tocar nos outros, nem recuperar um
  ponto no tempo entre dois snapshots — que são justamente os cenários de erro humano e de bug de
  migração. Por isso o snapshot **complementa**, e não substitui, dump lógico por schema mais
  arquivamento de WAL para PITR. Backup sem restore drill documentado não é backup.
  Com a guarda de 6 anos da §5.7, esses backups também precisam ser criptografados e ter acesso
  restrito: um backup antigo é um dossiê com CPF e renda esperando leitor.
- **Orçamento de conexões:** três serviços × pool + migrator + forwarder de outbox do Rebus.
  `Maximum Pool Size` explícito por serviço e `max_connections` dimensionado; PgBouncer entra se e
  quando o número justificar, não antes.
- **Alertas:** disco, conexões em uso, idade da transação mais antiga, lag do outbox (linhas não
  despachadas).

### 11.5 Migração sem primitiva de Job

O Swarm não tem Job nem init-container. O Migrator de cada serviço roda **a partir do pipeline de
deploy**, contra o banco, antes do `docker stack deploy`:

```text
build → push imagem → migrator (credencial DDL, sai 0/1) → stack deploy → healthcheck
```

O pipeline é o único lugar que segura a credencial de DDL; a aplicação em produção nunca tem
privilégio de alterar schema. A alternativa — `docker service create --restart-condition=none` como
serviço efêmero — fica documentada para operação manual, mas não é o caminho padrão.

Consequência de desenho que vale explicitar: **toda migração precisa ser compatível com a versão
anterior da aplicação**, porque durante o rolling update as duas versões convivem. Coluna nova é
nullable ou tem default; renomear é expandir, migrar e contrair em três deploys, nunca em um.

### 11.6 Rolling update e health

Os endpoints de health da §8 viram contrato do orquestrador:

- `healthcheck` do serviço aponta para `/health/ready` — o Swarm só coloca a réplica no routing mesh
  quando as dependências (Postgres, RabbitMQ, S3, JWKS) respondem;
- `deploy.update_config` com `order: start-first` e `failure_action: rollback` — sobe a nova réplica
  antes de derrubar a antiga e reverte sozinho se ela não ficar saudável;
- `stop_grace_period` maior que o tempo de shutdown gracioso, para que requisição em voo e mensagem
  em processamento terminem antes do `SIGKILL`.

### 11.7 Onde o Redis gerenciado entra

Ele não estava no desenho original e passa a resolver três problemas criados pelas réplicas:

1. **Rate limit distribuído no `gateway-public`.** Com N réplicas, um limite em memória vira N vezes
   o limite pretendido. O contador vai para o Redis.
2. **Cache de leitura do catálogo público.** A projeção de D01 é lida por tráfego anônimo e muda por
   evento — cache com invalidação no consumo do evento, não por TTL cego. Fica desenhado agora e
   implementado quando a feature de catálogo existir e houver número de latência para justificar.
3. **Chaves de Data Protection**, se alguma coisa passar a usar antiforgery ou cookie. Hoje a
   autenticação é bearer e isso não é necessário — registrado para não virar bug silencioso depois.

## 12. Sequência de implementação

Cada passo termina com algo verificável. Nada de "passo de infraestrutura sem prova".

### Fase A — Núcleo

| # | Entrega | Pronto quando | Status |
|---|---|---|---|
| A1 | `backend/` com solution, `Directory.Build.props`/`Packages.props`, `.editorconfig`, projeto vazio para os **quatro** serviços | `dotnet build` limpo com warnings-as-errors | ✅ concluída |
| A2 | `BuildingBlocks.Domain` e `.Application` (Entity, ValueObject, IDomainEvent, IUnitOfWork, IClock, ICurrentUser) | Testes unitários das primitivas passam | ✅ concluída |
| A3 | `BuildingBlocks.Web`: ProblemDetails + `IExceptionHandler`, correlation-id, OpenAPI, CORS, rate limit | Erro não tratado devolve ProblemDetails com `traceId`, sem stack | ✅ concluída |
| A4 | Postgres com os quatro schemas, roles e grants; `DbContext` + convenções por serviço; migração inicial vazia; Migrator | Migrator aplica em base limpa; teste prova que `svc_catalog` **não** lê `inventory` | ✅ concluída |
| A4b | Trilha de auditoria de acesso a dado sensível + sanitização em log/trace/evento (§5.7) | Leitura de registro marcado como sensível gera linha de auditoria; campo marcado não aparece no exportador OTLP | ✅ concluída |
| A5 | Logto no compose (+ banco `logto` e seed), script de bootstrap idempotente da §5.1, `oidcConfig.ts` repontado; `infra/keycloak/` removido | Base limpa: `compose up` + bootstrap deixam o backoffice logando, e o token traz `aud: https://api.sbacars.app` com os scopes do papel | ✅ concluída |
| A6 | JwtBearer + default-deny em todos os serviços; `ClaimsTransformation` projetando `scope` em permissões; `ICurrentUser` com permissões | Endpoint protegido: 401 sem token, 403 sem permissão, 200 com permissão. Teste de arquitetura falha se aparecer `[Authorize(Roles=...)]` ou `IsInRole` | ✅ concluída |
| A6b | Ligar `Infrastructure` na DI dos quatro `Api` e fechar a pendência da §5.7: flush da auditoria no fim da requisição | Leitura puramente de leitura, sem `SaveChanges`, gera linha de auditoria — provado por teste | ✅ concluída |
| A7 | Gateways YARP: rotas, CORS, rate limit, validação de token no edge de backoffice | Os dois SPAs alcançam o backend pelos ports atuais, sem mudar `runtimeConfig` | ✅ concluída |
| A8 | Observabilidade: OTel, health checks, Aspire Dashboard | Requisição do SPA aparece como um trace único atravessando gateway e serviço | ⬜ pendente |
| A9 | `TestKit`, testes de arquitetura, gate de CI atualizado | Referência de projeto indevida entre serviços quebra o build | ⬜ pendente |

Dois débitos pequenos a endereçar em A9, ambos de código de teste: cobrir o executável do `Migrator`
no gate (hoje o teste chama `MigrateAsync` direto, e o binário só foi verificado à mão), e dar ao
`DbContext` de sondagem um `IModelCacheKeyFactory` que inclua o schema — o cache de modelo do EF é
indexado pelo tipo CLR, então dois testes paralelos com schemas diferentes sobre o mesmo contexto
disputam o modelo compilado. A6b contornou usando o mesmo schema nos dois, o que resolve hoje e
volta a quebrar no primeiro teste que precisar de outro.

**Estado em 2026-08-15:** A1 a A7 entregues e verificadas — 35 projetos, `dotnet build` e
`dotnet format` limpos, 114 testes passando. Logto provisionado e com login do backoffice validado
ponta a ponta; o bootstrap foi executado duas vezes para provar idempotência. Os quatro `Api` têm
endpoints de prova (`/api/_probe/whoami`) marcados como andaimes de A6, a remover quando as features
reais chegarem. `BuildingBlocks.Observability` contém só o mecanismo de
sanitização; o OpenTelemetry em si é A8. Os quatro `Api` agora referenciam sua própria
`Infrastructure` e chamam `Add<Serviço>Infrastructure` no boot, com a connection string da role
`svc_*` vinda de `appsettings.Development.json` e validada com `ValidateOnStart` — nenhum roda
migração fora de Development. O flush de auditoria de A6b (`SensitiveDataAuditFlushMiddleware` +
`ISensitiveDataAuditFlusher`) está no pipeline dos quatro serviços e é hoje um no-op: nenhum dos
quatro tem `ISensitiveDataEntity` ainda, então nem o interceptor nem a tabela de auditoria foram
mapeados nos seus `DbContext` — isso continua correto, por §3.3 da SensitiveDataAuditModelBuilderExtensions,
que só entra no dia em que um serviço introduz sua primeira entidade sensível (D03, depois D04).
`ICurrentUser` tem registro concreto desde A6; `IClock` continua sem registro concreto nos quatro
serviços reais (só nos testes), porque nada nos quatro serviços o resolve ainda — entra junto com o
interceptor no dia em que um deles precisar. O `Migrator` de cada serviço foi verificado à mão
contra o Postgres do compose, mas o teste automatizado chama `MigrateAsync` direto — cobrir o
executável no gate fica para A9.

A7 ligou YARP nos dois gateways sobre o pipeline que já existia. Convenção de path única para os
dois edges: `/api/<serviço>/{**rest}` no gateway vira `/api/{**rest}` no serviço, via
`PathRemovePrefix`/`PathPrefix` — o gateway é dono do mapa de path público, o serviço não sabe em
qual edge está publicado. O `gateway-public` só tem rota para `catalog` (métodos `GET`/`HEAD`/
`OPTIONS`) e `interest` (`POST`/`OPTIONS`, com `RateLimiterPolicy` = `sbacars-anonymous-strict`) —
nunca `inventory` nem `purchase`, e isso é verificado por teste de tabela de rotas, não só por
convenção. O `gateway-backoffice` tem rota para os quatro serviços, todos os métodos, cada uma com
`AuthorizationPolicy: "Default"` explícito — redundante com o `FallbackPolicy` que `AddSbaCarsAuth`
já registra, de propósito. Cada cluster define `HttpRequest.ActivityTimeout` explicitamente (30s)
em `appsettings.json`; os `Destinations` ficam só em `appsettings.Development.json`, e uma
validação de boot (`ReverseProxyExtensions.UseSbaCarsReverseProxy`, em `SbaCars.Gateway.Shared`)
lança se alguma rota apontar para um cluster inexistente ou se um cluster referenciado por rota
ficar sem destino — sem isso, um ambiente mal configurado degradaria em 503 silencioso em vez de
falhar no boot. Essa fiação nasceu em `BuildingBlocks.Web` e foi movida para um projeto próprio ao
lado dos gateways: por `.Web`, os quatro `Api` passavam a carregar `Yarp.ReverseProxy` (ver §3.3).
Catalog e interest ganharam `/api/_probe/ping` `[AllowAnonymous]`, andaime de A7 para provar a rota
pública fim a fim, path reescrito incluso, a remover quando a feature real chegar — `GET` no
catalog e `POST` no interest, cada um no método que o seu edge de fato roteia; inventory e purchase
não foram tocados. O rate limit continua em memória por processo — D6 é quem
troca por contador no Redis quando houver mais de uma réplica; nada nessa entrega mudou isso. OTel,
health checks e Redis ficam fora, são A8 e D6.

### Fase B — Mensageria

| # | Entrega | Pronto quando |
|---|---|---|
| B1 | `BuildingBlocks.Messaging`: Rebus + RabbitMQ, topologia, envelope CloudEvents, retry/second-level/error queue, spans OTel | Serviço sobe, declara a topologia e a publicação aparece no trace |
| B2 | Outbox do `Rebus.PostgreSql` por serviço + `IUnitOfWork` que enlista a transação do EF | Rollback de transação não publica evento — provado por teste |
| B3 | Inbox/idempotência própria (step de pipeline + tabela `inbox_message`) | Reentrega do mesmo `message_id` não duplica efeito — provado por teste |
| B4 | `SbaCars.Contracts` com os eventos dos Domain Docs + snapshot de schema | Mudança breaking em contrato quebra o build |
| B5 | Prova `foundation.ping` inventory → catalog | Teste de integração cobre outbox → broker → inbox, com trace correlacionado |
| B6 | Saga e timeout persistidos no PostgreSQL habilitados e provados (capacidade exigida pela reserva de D04, §2.5) | Saga sobrevive a restart do processo e um timeout dispara depois de reinício — provado por teste |
| B7 | Job de expurgo de outbox/inbox (7 dias) com advisory lock | Com duas réplicas, o expurgo executa uma vez só — provado por teste |

### Fase C — Storage

| # | Entrega | Pronto quando |
|---|---|---|
| C1 | `BuildingBlocks.Storage` + `IObjectStorage` sobre S3/MinIO | Teste de integração com MinIO em Testcontainers |
| C2 | Buckets, política privada, CORS, criação idempotente no compose | `docker compose up` deixa os buckets prontos |
| C3 | Endpoint de URL pré-assinada (upload e download) protegido por política | Upload direto do browser funciona; acesso anônimo ao bucket é negado |

### Fase D — Deploy no Swarm

| # | Entrega | Pronto quando |
|---|---|---|
| D1 | Dockerfiles + build/push das imagens para o registry dos nós | `docker stack deploy` sobe os seis processos |
| D2 | Stack file: overlay `sbacars-net`, porta publicada só nos gateways, `update_config` start-first com rollback, `healthcheck` em `/health/ready` | Réplica não saudável não entra no routing mesh e o deploy reverte sozinho |
| D3 | Swarm secrets + `AddKeyPerFile`; nenhum segredo em env | `docker inspect` do serviço não revela credencial |
| D4 | Migrator no pipeline com credencial DDL separada | Deploy aplica migração antes do rollout; app em produção sem privilégio de DDL |
| D5 | PostgreSQL: placement, backup + PITR, **restore drill executado**, alertas de disco/conexão/lag de outbox | Restauração completa reproduzida em ambiente separado, com tempo medido |
| D6 | Rate limit distribuído no `gateway-public` via Redis | Limite se mantém correto com 2+ réplicas — provado por teste de carga simples |

### Depois da fundação

O caminho volta para o pipeline TSG Flow: **PRD da Fase 1** → API Contract (OpenAPI) → TechSpec de
backend e de frontend → Tasks. Este documento é insumo técnico dessas etapas, não substitui nenhuma
delas. Ordem sugerida de features, herdada dos Domain Docs: D02 F01–F06 → D01 F01–F05 → D03 F01–F05.

---

## 13. Decisões (resumo para virar ADRs)

| # | Decisão | Alternativa descartada | Motivo |
|---|---|---|---|
| 01 | Service-Based Architecture | Monolito modular; microsserviços | Fronteiras de domínio já são reais; escala e time separado por serviço, não |
| 02 | Uma instância Postgres, schema + role por serviço | Banco físico por serviço | Isolamento suficiente e verificável por `GRANT`, sem custo operacional de N bancos |
| 03 | Dois gateways YARP | Gateway único; acesso direto | Posturas de segurança opostas em processos separados; preserva os ports dos SPAs |
| 04 | Solution única, deploy por serviço | Solution por serviço + NuGet interno | Biblioteca compartilhada é traço de SBA; evita feed e ciclo de release interno |
| 05 | Só eventos entre serviços na Fase 1 | HTTP síncrono entre serviços | Dependências dos Domain Docs são informacionais; evita cadeia síncrona acoplada |
| 06 | Outbox transacional + inbox idempotente | Publicar direto no `SaveChanges` | Elimina evento perdido e evento fantasma; entrega é at-least-once |
| 07 | Audience única `sbacars-api` | Audience por serviço | Mesma sessão e mesmo limite de confiança; permissão se resolve por role |
| 08 | Default deny + `[AllowAnonymous]` explícito | Proteger endpoint a endpoint | Endpoint novo nasce protegido; esquecimento falha fechado |
| 09 | Migração por Migrator dedicado com role DDL | `Migrate()` no startup | Sem corrida entre réplicas; app em produção sem privilégio de DDL |
| 10 | URL pré-assinada nos dois sentidos | Upload/download via API | Binário fora do processo .NET: memória, timeout e custo |
| 11 | Nomes híbridos (técnico inglês, negócio português) | Tudo em inglês; tudo em português | Preserva a linguagem ubíqua sem quebrar convenção .NET |
| 12 | Rebus (MIT) sobre RabbitMQ, atrás de abstração | MassTransit 8.x/9; CAP; `RabbitMQ.Client` puro | MT8 tem fim de manutenção no final de 2026 e a v9 é comercial; Rebus resolve o outbox em PG e usa handler por tipo/DI |
| 13 | Deploy em Docker Swarm com Postgres no cluster e broker/cache/S3 gerenciados | Tudo gerenciado; tudo no cluster | Estado crítico com backup próprio; middleware sem valor diferenciado sai do nosso escopo operacional |
| 14 | Segredos por Docker Swarm secret + `AddKeyPerFile` | Variável de ambiente | Segredo em env vaza em `docker inspect`, log e crash dump |
| 15 | Provisionar os quatro serviços na fundação; features de D04 na Fase 2 | Criar só três e adicionar D04 depois | Custo linear agora, custo em seis lugares depois; e provisionar o quarto prova que a fundação generaliza |
| 16 | A decisão de reserva é de D02, com constraint única no schema `inventory` | D04 decidir e avisar D02 | O dono do invariante decide; coordenação entre dois bancos não garante unicidade |
| 17 | Auditoria de acesso e sanitização de dado sensível desde a Fase A | Implementar junto com D04 na Fase 2 | Retrofitar auditoria significa não saber quem leu o quê no período anterior |
| 18 | Autorizar por permissão (`recurso:acao`), nunca por role ou scope | `[Authorize(Roles=...)]` direto | Keycloak e Logto emitem claims diferentes; e a migração para papéis geridos pela aplicação passa a não tocar endpoint algum |
| 19 | Papéis e scopes criados por script de bootstrap versionado | Configurar no console do IdP | Política de acesso passa por code review, viaja com o deploy e roda em CI |
| 19b | **Logto como IdP único**, self-hosted em todos os ambientes | Keycloak local + Logto remoto | Elimina divergência de formato de claim entre ambientes; sem staging, ela só apareceria em produção |
| 20 | Não construir gestão de papéis agora | CRUD de papéis na fundação | Nenhum Domain Doc pede papel customizável; o seguro que torna a migração barata custa uma classe |
| 21 | Sem criptografia de coluna; auditoria + acesso restrito, com backup criptografado | Criptografar CPF e renda | Decisão do PO; a compensação obrigatória é backup, porque a guarda é de 6 anos |
| 22 | Audience `https://api.sbacars.app` idêntica em todos os ambientes | Audience por ambiente | Só a autoridade varia; um parâmetro a menos para divergir |
| 23 | Teto de duas réplicas por serviço no ambiente dev | Escalar livremente | 40 conexões do plano CloudAMQP; três réplicas estouram durante rolling update |

---

## 14. Riscos e questões em aberto

| Risco | Impacto | Mitigação |
|---|---|---|
| PostgreSQL único no Swarm é ponto único de falha dos três serviços | Alto | Backup automatizado + PITR, restore drill periódico, placement fixo com volume, alerta de disco e de conexões (ver §11.4) |
| Rebus depende essencialmente de um mantenedor | Médio | MIT com código pequeno e auditável; `IIntegrationEventPublisher` mantém a troca contida; fork é viável se necessário |
| Estouro silencioso do limite de 40 conexões do CloudAMQP durante deploy | Médio | Teto de duas réplicas, deploy serviço a serviço, alerta em 30 conexões (§6.3.1) |
| Logto self-hosted vira mais um serviço com estado para operar (banco, volume, backup, upgrade) | Médio | Banco próprio separado do `sbacars`, tag de imagem fixa, bootstrap idempotente reexecutável; migrar para o gerenciado continua sendo opção sem mudança de código |
| Configuração do Logto divergir entre ambientes por alteração feita na console | Médio | O script de bootstrap é a fonte de verdade e roda em CI; alteração manual é sobrescrita na próxima execução |
| BuildingBlocks virar framework interno | Alto | Nada de domínio lá dentro; só extrai no segundo consumidor real |
| Projeção do catálogo divergir do estoque (consistência eventual) | Alto | Outbox + inbox + reprocessamento; a UI comunica disponibilidade como informação de D02, conforme RN-08/RN-09 de D01 |
| Dado pessoal do comprador (D03) e dado sensível de D04 (CPF, renda, documentos) em log, trace, evento ou outbox | Alto | Sanitização como mecanismo, não recomendação (§5.7); auditoria de acesso desde a Fase A |
| Duas reservas ativas para o mesmo veículo por corrida entre D04 e D02 | Alto | Decisão centralizada em D02 com constraint única no schema `inventory`; D04 nunca reserva otimisticamente (§2.5) |
| Reserva expirar sem que a jornada de D04 reaja, prendendo o veículo | Médio | Timeout durável em saga persistida, testado contra restart do processo (B6) |
| Compartilhar instância de banco virar desculpa para query cruzada | Alto | Grants por role + teste de integração que prova a negação |

**Questões a fechar antes ou durante a Fase A:**

- [ ] Confirmar na B1 quantas conexões o Rebus abre por processo; o orçamento da §6.3.1 assume duas,
      e o teto de duas réplicas por serviço depende disso.
- [ ] Definir a cadência de atualização da imagem do Logto (a tag está fixada em `1.42.0`).

**Resolvidas nas revisões 2 a 5:** mensageria (Rebus MIT, §6.1); papéis do realm (§5.2); ambiente
alvo (Docker Swarm, §11); escopo de D04 na fundação (§3); broker gerenciado (CloudAMQP Loyal
Lemming, 40 conexões e 2M mensagens/mês, §6.3.1); expurgo de outbox/inbox (7 dias com advisory lock,
§6.3.2); **IdP único: Logto self-hosted em todos os ambientes, Keycloak removido (§5.0)**; audience
única `https://api.sbacars.app` (§5.2);
registry (Docker Hub e GHCR, §11.1); reserva única por veículo (§2.5); retenção (6 anos em D04, 1 ano
em D03, §5.7); criptografia de coluna (fora de escopo, com backup criptografado como compensação,
§5.7); modelo de autorização, custo de adiar gestão de papéis e limite dele (§5.6); ambientes (local
e dev, sem staging, §11.0); granularidade de localização (fora da fundação, tratada no PRD da
feature).

---

*Próximo artefato recomendado: **PRD da Fase 1**, com `docs/vision.md`, os Domain Docs e este
documento como contexto.*
