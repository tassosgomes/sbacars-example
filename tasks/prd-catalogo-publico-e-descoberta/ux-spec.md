# UX Spec — Catálogo Público e Descoberta

> **Etapa 1 do planejamento.** Define o *contrato de tela* do D01: inventário, estados,
> campos, ações e transições. Alimenta os briefs do Stitch (`stitch-briefs.md`) e o
> API Contract (`api-contract.yaml`).
>
> **Fonte:** `prd.md` (RF-01 a RF-07) · `domains/catalogo-descoberta/domain.md` (RN-01 a RN-12)
> **Decisões de produto:** [PD-001](../../docs/product-decisions/PD-001-dado-pessoal-do-comprador-pertence-a-d03.md) · [PD-002](../../docs/product-decisions/PD-002-sem-identidade-de-comprador-na-fase-1.md)
> **App de destino:** `apps/catalog` — features `catalog`, `interest`
> **Idioma da UI:** PT-BR
> **Última revisão:** 2026-08-16

---

## 1. Escopo

Todas as telas deste documento pertencem à **superfície pública do comprador final**
(`apps/catalog`, atrás do `SbaCars.Gateway.Public`). O backoffice da Operação central é
de D02 (`apps/backoffice`) e está fora deste PRD.

**Um único papel opera estas telas:**

| Papel | Autenticação | O que faz |
|---|---|---|
| Comprador final | **Nenhuma** (PD-002) | Busca, filtra, compara, favorita, consulta a apresentação e manifesta interesse. |

Não há login, área logada nem cadastro. O comprador é reconhecido apenas como
**navegador identificado** — um UUID gerado no cliente e guardado em `localStorage`,
que não constitui identidade de pessoa.

**Duas fronteiras que a interface precisa respeitar sem que o comprador as perceba:**

| Fronteira | O que D01 faz | O que D01 **não** faz |
|---|---|---|
| D02 → D01 | Apresenta fatos, preço, disponibilidade e localização recebidos. | Não edita, não infere, não completa nenhum deles (RN-03). |
| D01 → D03 | Oferece a ação de interesse e entrega o contexto da descoberta. | Não coleta, não exibe e não retém nome, contato ou mensagem (PD-001). |

---

## 2. Decisões de UX

| ID | Decisão | Motivo |
|---|---|---|
| DUX-01 | **`/` é a própria listagem do catálogo.** Não existe home editorial na Fase 1. | Nenhum RF pede uma home, e o conteúdo que a justificaria (vitrine, destaques, fotos) pertence ao PRD-B. Uma home sem esse conteúdo seria uma tela de passagem que só adia o primeiro resultado. **Precisa do seu aval** — ver §8, QA-01. |
| DUX-02 | O pedido de localização é uma **pré-permissão explicada** (M02), nunca o prompt nativo disparado no carregamento. As duas saídas — usar a localização do navegador e escolher a cidade — têm o mesmo peso visual. | DP-04 e o risco nº 1 do PRD. Prompt nativo sem contexto é negado por reflexo, e a meta de adesão é ≥ 50%. Recusar não pode parecer erro: sem localização o catálogo é pleno, apenas ordenado por recência (DP-08). |
| DUX-03 | **`Não informado` é um valor renderizado, nunca uma linha omitida.** Componente próprio, com texto real no DOM. | RN-02 e o requisito de acessibilidade do PRD: leitor de tela precisa anunciar a lacuna, não pular a linha. Omitir o atributo faria o comprador supor que ele não existe, em vez de saber que não foi apurado. |
| DUX-04 | O detalhe separa visualmente **o que a operação apurou** (bloco de D02, sempre com data e fonte) do **conteúdo comercial** (bloco do PRD-B). Os fatos vêm primeiro na ordem de leitura. | RN-03 e DP-01. O comprador precisa distinguir fato apurado de texto de venda; se as duas coisas dividirem o mesmo espaço visual, a transparência vira retórica. |
| DUX-05 | **Nenhuma linguagem de certificação, garantia formal ou vistoria aprovada** em qualquer tela. Selos, "verificado", "aprovado" e ✓ verdes ao lado de fatos estão proibidos. | RN-12 e RF-03. O sistema visual usa Trust Green para "verificado" — aqui ele só pode marcar *estado de disponibilidade*, nunca *qualidade do veículo*. |
| DUX-06 | **Reservado é uma tarja informativa que não remove nenhuma ação.** O item continua listável, favoritável, comparável e apto a gerar interesse. | RN-08 e RF-04. Esconder ou desabilitar o reservado transformaria uma informação honesta em perda de oportunidade para os dois lados. |
| DUX-07 | Todo dado vindo de D02 mostra **quando foi atualizado**; nenhum mostra **por quem**. | A data sustenta a confiança (RF-04) e é a mitigação do risco de desatualização. O nome do operador é dado pessoal de funcionário e não tem função na jornada pública. |
| DUX-08 | O aviso de que favoritos e comparações **vivem só neste navegador** aparece na tela de favoritos, no momento de uso — não como toast global nem como banner de primeira visita. | Texto do PRD: "comunicar essa limitação no momento em que ela importa, em vez de escondê-la". Avisar antes de existir favorito é ruído; avisar ao abrir a lista é informação. |
| DUX-09 | O interesse é uma **página endereçável** (`/veiculos/:itemId/interesse`), não um modal. | A jornada continua em D03 dentro da mesma página; um modal tornaria a transição de domínio um beco sem URL e impediria medir a métrica primária por página. |
| DUX-10 | A distância é sempre **aproximada e até a cidade do veículo**, rotulada como tal (`~ 42 km · Campinas/SP`). | Termo canônico "Distância apresentada". Um número exato sugeriria porta a porta, que a plataforma não sabe e não promete (risco de alcance nacional do PRD). |
| DUX-11 | **A placa não é exibida em nenhuma tela pública.** | D02 fornece `placa` na projeção `OfertaElegivel`, mas ela identifica o veículo em bases de terceiros e não ajuda o comprador a decidir. Exibi-la seria vazar dado operacional sem contrapartida. |

---

## 3. Modelo de estados

### 3.1 Status público do item

D02 tem dois eixos independentes — situação da oferta (`em-preparacao`, `elegivel`,
`suspensa`, `retirada`) e disponibilidade (`disponivel`, `reservado`, `vendido`). O
comprador não deve ver essa matriz. **D01 projeta os dois eixos em quatro estados
públicos**:

```
   D02: situação × disponibilidade                     D01: status público
   ─────────────────────────────────────────────────────────────────────────
   elegivel   × disponivel  ──────────────────────►    disponivel
   elegivel   × reservado   ──────────────────────►    reservado
   qualquer   × vendido     ──────────────────────►    vendido
   em-preparacao | suspensa | retirada ───────────►    indisponivel
```

| Status público | Aparece na listagem e na busca | Detalhe | Favoritar | Comparar | Interesse |
|---|---|---|---|---|---|
| `disponivel` | **Sim** | Completo | Sim | Sim | **Sim** |
| `reservado` | **Sim**, com tarja | Completo, com tarja | Sim | Sim | **Sim** |
| `vendido` | Não | "Não está mais disponível" | Permanece no favorito **já existente**, sem foto e sem link | Removido da comparação, com aviso | Não |
| `indisponivel` | Não | "Não está disponível" | Sai dos favoritos, com a ausência informada | Removido da comparação, com aviso | Não |

**A diferença entre `vendido` e `indisponivel` nos favoritos é deliberada e vem do PRD.**
Vendido conta uma história ao comprador — *este carro foi comprado* (RF-04, RN-09).
Indisponível não conta nada — a oferta simplesmente saiu (RF-06). Tratar os dois igual
apagaria a informação mais útil dos favoritos.

`disponivel` **não tem badge**. É o estado normal; marcá-lo com um selo verde gastaria
atenção no caso comum e colidiria com a DUX-05.

### 3.2 Localização de referência

Quatro estados, e nenhum deles é erro.

```
                    ┌──────────────┐
                    │   ausente    │  ordenação: publicação mais recente
                    └──────────────┘  distância: oculta em toda a interface
                       │          │
       permissão       │          │   escolha manual
       concedida       ▼          ▼   de cidade
              ┌──────────────┐  ┌──────────────┐
              │  geolocaliz. │  │    manual    │  ordenação padrão: proximidade
              └──────────────┘  └──────────────┘  distância: exibida
                       │
       permissão       ▼
       negada    ┌──────────────┐
                 │   negada     │ ─── oferece escolha manual ──► manual
                 └──────────────┘     nunca volta a pedir sozinha
```

| Estado | Origem | Distância | Ordenação padrão | Persistência |
|---|---|---|---|---|
| `ausente` | Primeira visita, antes de decidir | Oculta | `publicacao:desc` | — |
| `geolocalizacao` | `navigator.geolocation` concedida | Exibida | `proximidade` | **Só na sessão.** Coordenadas nunca são gravadas |
| `manual` | Cidade escolhida em M02 | Exibida | `proximidade` | `cidadeReferenciaId` em `localStorage` |
| `negada` | Permissão recusada ou indisponível | Oculta | `publicacao:desc` | Marca de "já perguntei", para não repetir |

A coordenada obtida do navegador **viaja como parâmetro de consulta e não é persistida
em lugar nenhum** — nem em banco, nem em log, nem em `localStorage`. É a redução máxima
de superfície de LGPD compatível com DP-04, enquanto a pergunta jurídica do PRD segue
aberta (§9).

### 3.3 Transparência de um fato

Cada um dos três blocos que vêm de D02 — Origem, Condição, Histórico — chega em uma de
duas formas válidas, e nunca em uma terceira:

```
   ┌────────────────────┐        ┌────────────────────────────┐
   │  com conteúdo      │   ou   │  com limitação declarada   │
   │  + fonte + data    │        │  (texto literal da operação)│
   └────────────────────┘        └────────────────────────────┘
                    │                       │
                    └───── ambos exibidos ──┘
                            ao comprador

   ┌────────────────────────────────────────┐
   │  sem conteúdo e sem limitação          │  ← não deve chegar a D01:
   │                                        │     o CM-6 de D02 impede a
   └────────────────────────────────────────┘     elegibilidade nesse caso
```

O terceiro caso é impossível pelo contrato de D02, mas a interface **não pode confiar
nisso**: se chegar, o bloco exibe `Não informado` e a tela continua. Um dado que falta
nunca pode derrubar a página do veículo.

A **limitação declarada é exibida com o texto literal da operação**, jamais substituída
por frase genérica (RF-03). "Não foi possível obter o histórico de sinistros junto às
bases consultadas" diz o que foi tentado; "informação indisponível" não diz nada.

### 3.4 Presença de conteúdo comercial (PRD-B)

| Estado | O que existe | Como a tela se comporta |
|---|---|---|
| `ausente` | Nada do PRD-B | Título derivado de marca + modelo + versão; galeria substituída por placeholder neutro; blocos de descrição e destaques **não são renderizados** |
| `parcial` | Fotos sem texto, ou texto sem fotos | Renderiza o que existe; o que falta some, sem espaço vazio nem "em breve" |
| `completo` | Título, descrição, destaques e fotos | Layout pleno |

**A página do veículo precisa ser boa no estado `ausente`.** É a condição do RF-01 e a
mitigação do risco de o PRD-B atrasar o MVP: um bloco vazio com "conteúdo em breve"
transformaria uma dependência de backlog em promessa quebrada na cara do comprador.

### 3.5 Comparação

```
   0 itens ──► 1 item ──► 2 itens ──► 3 ──► 4 itens ──╳── 5º recusado
      │           │          │                  │          com o motivo
      │           │          └── comparação habilitada ────┘
      │           └── barra visível, botão desabilitado com o motivo
      └── barra oculta
```

Com 1 item a barra já aparece: o comprador precisa saber que a seleção existe e quanto
falta. O botão desabilitado carrega o motivo (`Selecione pelo menos 2 veículos`), e a
recusa do quinto informa o limite (RF-07) — nunca falha em silêncio.

---

## 4. Navegação e rotas

```
Cabeçalho público          Rota                              RF        Fase
──────────────────────────────────────────────────────────────────────────────
Catálogo                   /                                 01,02,04   MVP
  └ detalhe                /veiculos/:itemId                  01,03,04   MVP
  └ interesse              /veiculos/:itemId/interesse        05         MVP
Favoritos          (N)     /favoritos                         06         Fase 2
Comparar           (N)     /comparar                          07         Fase 2
```

Os badges `(N)` mostram a contagem lida do `localStorage`, sem chamada de rede.

**O estado da busca vive na URL.** Filtros, ordenação e página entram na query string
(`/?marca=Honda&precoMax=90000&ordenar=preco:asc&page=2`), para que um resultado seja
compartilhável, sobreviva ao *refresh* e ao botão voltar. A localização de referência
**não** entra na URL: coordenada em link compartilhado é vazamento de dado de quem
compartilhou.

**Ajuste necessário no código existente:** `apps/catalog/src/app/router.tsx` hoje usa
`/vehicles`, `/vehicles/:id`, `/vehicles/:id/interest` e uma `HomePage` em `/`, com
rótulos em inglês em `PublicLayout.tsx`. Ver §9.

---

## 5. Inventário de telas

### T01 — Resultado do catálogo

**Rota** `/` · **RF** 01, 02, 04 · **Fase** MVP · **É a tela de maior tráfego**

Porta de entrada. Precisa entregar resultado antes de pedir qualquer coisa ao comprador.

**Conteúdo**

- Cabeçalho público: marca, links `Favoritos (N)` e `Comparar (N)`
- **Faixa de localização** — estado atual e como mudá-lo:
  - com localização: `Mostrando distâncias a partir de Campinas/SP` + link `Alterar`
  - sem localização: convite discreto `Ver os veículos mais próximos de você` → M02
- Barra de busca livre (marca, modelo, versão) + botão `Filtros (3)` com a contagem de
  filtros ativos
- Painel de filtros (lateral no desktop, sheet no mobile): marca, modelo, faixa de ano,
  faixa de preço, quilometragem máxima, combustível, câmbio, UF, cidade, raio de
  distância. **Carroceria não é oferecida** enquanto D02 não fornecer o atributo (RF-02)
- Linha de resultado: contagem (`142 veículos`), chips dos filtros ativos com `×`,
  select de ordenação
- Grade de cards de veículo, 3 colunas em 1280px
- Paginação

**Card de veículo** — a unidade de informação mais repetida do produto:

| Elemento | Regra |
|---|---|
| Foto | Do PRD-B. Sem foto, placeholder neutro com a silhueta — nunca "sem imagem" |
| Título | Título comercial do PRD-B ou, na ausência, `Marca Modelo Versão` |
| Linha técnica | `2021/2022 · 48.300 km · Automático · Flex` |
| Preço | Destacado, tabular, com `Atualizado em DD/MM/AAAA` em texto auxiliar (DUX-07) |
| Localização | `Campinas/SP` · e `~ 42 km` quando houver localização de referência |
| Status | Tarja `Reservado` quando for o caso. `Disponível` não recebe badge (§3.1) |
| Ações | Favoritar (coração) e Comparar (checkbox) — nunca cobrem a foto nem o preço |

**Ações**: abrir detalhe · favoritar · adicionar à comparação · filtrar · ordenar · definir localização

**Estados**: com localização (T01) · sem localização (T01-c) · sem resultados (T01-b) ·
carregando (skeleton de 6 cards) · erro de carga com repetição · página além do fim

---

### T01-b — Variante: sem resultados

**RF** 02 · Terceiro critério de aceitação do RF-02.

Não basta dizer que não há nada. A tela **indica quais filtros relaxar**, em ordem de
impacto, cada um como ação de um clique:

> Nenhum veículo com todos esses critérios.
> · Ampliar o preço até R$ 95.000 → **12 veículos**
> · Remover o filtro de câmbio automático → **8 veículos**
> · Ampliar o raio para 200 km → **31 veículos**
> [ Limpar todos os filtros ]

As contagens vêm do servidor. Sugerir sem contar seria empurrar o comprador para outro
resultado vazio — e "buscas com resultado ≥ 85%" é métrica do PRD.

---

### T01-c — Variante: sem localização de referência

**RF** 02 · Quarto critério de aceitação do RF-02.

Mesma tela, com três diferenças e **nenhuma degradação visível**:

- A coluna de distância some dos cards. Não vira `—`, não vira `Não informado`: a
  distância não é um atributo do veículo, é uma relação com o comprador que ainda não
  existe
- A ordenação padrão é `Publicação mais recente`, apresentada como escolha normal
- A faixa de localização traz o convite, com as duas saídas lado a lado

---

### M02 — Localização de referência *(sheet sobre T01)*

**RF** 02 · **Aparece:** na primeira visita e ao clicar `Alterar`

**Conteúdo**

- Título: `Ver os veículos mais próximos de você`
- Finalidade declarada, em uma frase: `Usamos sua localização apenas para calcular a
  distância até a cidade de cada veículo. Ela não é armazenada.`
- Duas ações de mesmo peso:
  - `Usar minha localização` → dispara o prompt nativo
  - `Escolher uma cidade` → revela o campo de busca de cidade com autocompletar
- Saída explícita: `Agora não` — o catálogo continua utilizável

**Estados**: convite · buscando localização · permissão negada (mensagem sem culpa, foco
transferido para a escolha manual) · escolha manual · cidade selecionada · navegador sem
suporte a geolocalização

A frase sobre não armazenar é uma promessa que o contrato precisa cumprir: nenhuma
coordenada em banco ou log (§3.2).

---

### T03 — Detalhe do veículo

**Rota** `/veiculos/:itemId` · **RF** 01, 03, 04, 05 · **É onde a métrica primária acontece**

Duas colunas: conteúdo à esquerda (~65%), coluna de decisão fixa à direita (~35%).

**Cabeçalho**

- Título (comercial do PRD-B, ou `Marca Modelo Versão`)
- Linha técnica: `2021/2022 · 48.300 km · Automático · Flex · Campinas/SP · ~ 42 km`
- Tarja `Reservado` quando for o caso

**Coluna de conteúdo**

1. **Galeria** — fotos do PRD-B; sem elas, placeholder neutro (§3.4)
2. **Card `O que a operação apurou`** — três blocos, cada um com conteúdo + fonte + data,
   ou o selo `Limitação declarada` com o texto literal (§3.3). É o primeiro bloco de
   texto da página, por DUX-04
3. **Card `Ficha técnica`** — grade chave/valor. Todo atributo sem valor aparece como
   `Não informado`, jamais omitido (DUX-03). Na Fase 1 a ficha é composta pelos atributos
   que D02 fornece; PRD-B a amplia depois
4. **Card `Sobre este veículo`** — descrição e destaques do PRD-B. **Não renderizado**
   quando não houver conteúdo

**Coluna de decisão** (fixa no rolar, no desktop)

5. **Card de preço e ação**
   - Preço oficial em destaque, com `Preço atualizado em DD/MM/AAAA`
   - Cidade/UF e distância aproximada
   - Tarja de reserva quando `reservado`: `Há alguém à frente neste veículo. Você ainda
     pode demonstrar interesse.`
   - Botão primário `Tenho interesse` → T04
   - Ações secundárias: `Favoritar` · `Comparar`
6. **Nota de transparência** — texto fixo, curto: `Não realizamos certificação nem
   vistoria formal. Publicamos o que a operação apurou e declaramos o que não foi
   possível apurar.` (RN-12, DUX-05)

**Regras de habilitação**

- `Tenho interesse` existe apenas em `disponivel` e `reservado`. Em `vendido` ou
  `indisponivel` a tela inteira vira T03-c — o botão não aparece desabilitado, porque não
  há nada a habilitar (RF-05)

**Estados**: disponível com conteúdo comercial (T03) · reservado sem conteúdo comercial
(T03-b) · indisponível (T03-c) · carregando · erro de carga

---

### T03-b — Variante: reservado e sem conteúdo comercial

**RF** 01, 04 · Prova simultânea de dois critérios de aceitação.

Mesma tela, com:

- Tarja `Reservado` no cabeçalho e no card de decisão, **sem nenhuma ação removida**
  (DUX-06)
- Galeria substituída por placeholder neutro
- Card `Sobre este veículo` ausente — não vazio, ausente
- Os cards de fatos e ficha técnica **inalterados**: são de D02 e não dependem do PRD-B

É a tela que prova que o MVP sobrevive ao atraso do PRD-B. Se ela parecer quebrada, o
RF-01 não está atendido.

---

### T03-c — Variante: item indisponível

**RF** 04, 05 · Resposta de `vendido` e `indisponivel`.

Página curta, honesta, sem beco sem saída:

- `Este veículo foi vendido.` ou `Este veículo não está mais disponível.`
- Identificação do veículo (marca, modelo, versão, ano) — **sem fotos, sem preço, sem
  ficha técnica, sem ação de interesse** (RN-09)
- Saída útil: `Ver veículos semelhantes` → T01 pré-filtrada por marca e modelo

Sem preço porque o preço de um carro vendido não é informação de catálogo, é dado de
transação — e não pertence a D01.

---

### T04 — Início de interesse

**Rota** `/veiculos/:itemId/interesse` · **RF** 05 · **A fronteira D01/D03 mora aqui**

Uma página, dois donos. A costura precisa ser invisível ao comprador e explícita no
código.

**Bloco de D01 — contexto da descoberta (leitura)**

- Card do veículo: título, foto (se houver), ano, quilometragem, cidade/UF
- Preço e status **exatos que estavam na tela** quando o comprador clicou
- Frase de continuidade: `A operação vai retomar o contato sobre este veículo.`

**Bloco de D03 — captação do contato**

- Formulário de contato: os campos, a validação, o consentimento e a retenção são
  **contrato de D03** e não estão especificados aqui (PD-001)
- Renderizado como região delimitada. Nenhum campo deste bloco transita pelo
  catalog-service: o `POST` vai direto ao gateway público → interest-service

**Sequência**

```
1. Comprador clica "Tenho interesse" em T03
2. D01 registra a manifestação com o contexto da descoberta
   e emite `catalogo.interesse-solicitado`          ← a métrica primária é medida aqui
3. A tela revela o formulário de D03, referenciando o interesse criado
4. D03 recebe o contato e conduz a continuidade
```

**O passo 2 acontece antes do passo 3, e isso é deliberado.** A conversão
descoberta → interesse é medida no momento em que o comprador demonstra a intenção, não
quando D03 conclui a captação. Assim a métrica primária do PRD é observável **antes de
D03 existir**, e a Fase 1 não fica refém de outro domínio para saber se funcionou.

**Estados**: contexto carregado com formulário de D03 · veículo ficou indisponível entre
o clique e a página (mensagem + volta ao catálogo, RF-05) · preço mudou desde a
visualização (aviso não bloqueante com o valor vigente) · interesse encaminhado
(confirmação) · D03 indisponível (o interesse **já foi registrado**; a tela informa que a
operação retomará o contato)

O último estado importa: se D03 cair, o comprador não pode receber um erro que sugira que
nada aconteceu. A intenção já foi registrada em D01.

---

### T05 — Favoritos

**Rota** `/favoritos` · **RF** 06 · **Fase 2**

**Conteúdo**

- Título `Seus favoritos` + contagem
- **Aviso de escopo, permanente e no topo** (DUX-08): `Seus favoritos ficam guardados
  neste navegador. Limpar os dados do navegador ou trocar de aparelho faz com que se
  percam.`
- Grade dos mesmos cards de T01, com três variações:

| Situação do item | Card |
|---|---|
| `disponivel` / `reservado` | Card normal, com todas as ações |
| `vendido` | Card apagado, sem foto e sem link: `Vendido` + identificação. Ação única: remover |
| `indisponivel` | Card apagado: `Este veículo não está mais disponível.` Ação única: remover |

**Ações**: abrir detalhe · desfavoritar · adicionar à comparação · limpar os indisponíveis

**Estados**: vazio (com convite ao catálogo) · com itens · com itens vendidos ou
indisponíveis · carregando

Favoritar **não** cria interesse nem inicia contato — a tela não sugere o contrário em
nenhum texto (RN-06).

---

### T06 — Comparação

**Rota** `/comparar` · **RF** 07 · **Fase 2**

Tabela de 2 a 4 colunas, uma por veículo, linhas por atributo.

**Conteúdo**

- Cabeçalho fixo por coluna: foto pequena, título, preço, `Remover`
- Linhas agrupadas: **Identificação** (ano, versão) · **Uso** (quilometragem) ·
  **Mecânica** (combustível, câmbio) · **Localização** (cidade/UF, distância) ·
  **Preço** · **Transparência** (uma linha por bloco de fato, com conteúdo resumido ou o
  selo de limitação)
- Toda célula sem valor mostra `Não informado`, **preservando o alinhamento da linha**
  (RF-07). Nenhuma linha é suprimida por estar vazia em todos os itens
- Ação por coluna: `Tenho interesse`

**Estados**: 2 a 4 itens · aviso de item removido por ter sido vendido (`O Honda Civic
EXL foi vendido e saiu da comparação.`) · menos de 2 itens após remoção (volta ao
catálogo com o motivo) · vazio

A barra de comparação (§6) é o que leva o comprador até aqui, e vive em T01, T03 e T05.

---

## 6. Componentes compartilhados

Vão para `packages/ui` ou `apps/catalog/src/shared/components`:

| Componente | Uso |
|---|---|
| `CardVeiculo` | Unidade da grade em T01 e T05. Absorve as variações de foto ausente e status |
| `TarjaStatus` | `Reservado`, `Vendido`, `Indisponível`. `Disponível` não renderiza nada (§3.1) |
| `ValorOuNaoInformado` | Renderiza o valor ou `Não informado` como texto real (DUX-03) |
| `BlocoFato` | Conteúdo + fonte + data, **ou** selo `Limitação declarada` + texto literal |
| `SeloLimitacao` | Marca uma limitação declarada. Tom informativo, nunca de erro |
| `PrecoComAtualizacao` | Preço tabular + `Atualizado em DD/MM/AAAA` (DUX-07) |
| `DistanciaAproximada` | `~ 42 km · Campinas/SP`. Não renderiza sem localização de referência |
| `SeletorLocalizacao` | Corpo do M02: pré-permissão, busca de cidade, estados de recusa |
| `BotaoFavoritar` | Coração com estado local. Sem rede no caminho de resposta ao clique |
| `BarraComparacao` | Dock persistente 0–4 itens, com limite e motivo (§3.5) |
| `PainelFiltros` | Lateral no desktop, sheet no mobile. Monta-se a partir das facetas do servidor |
| `EstadoVazio` | **Já existe** em `apps/catalog/src/shared/components/EmptyState.tsx` |

---

## 7. Rastreabilidade

| RF | Telas | Regras |
|---|---|---|
| RF-01 Publicação e detalhe | T01, T03, T03-b | RN-01, RN-03 |
| RF-02 Busca, filtros, ordenação, proximidade | T01, T01-b, T01-c, M02 | RN-01, RN-10 |
| RF-03 Apresentação transparente | T03 | RN-02, RN-03, RN-12 |
| RF-04 Status público e preço | T01, T03, T03-b, T03-c, T05 | RN-01, RN-08, RN-09 |
| RF-05 Início de interesse | T03, T04, T03-c | RN-06, RN-11 |
| RF-06 Favoritos | T05 | RN-06, RN-07, RN-09 |
| RF-07 Comparação | T06, `BarraComparacao` | RN-02, RN-05, RN-09 |

Nenhum RF ficou sem tela; nenhuma tela ficou sem RF.

---

## 8. Decisões resolvidas

Eram questões em aberto do PRD ou do Domain Doc; decididas em 16/08/2026. Todas entram no
API Contract.

| ID | Questão | Decisão |
|---|---|---|
| QA-01 | Existe home editorial? | **Não na Fase 1.** `/` é a listagem (DUX-01). Uma home volta à pauta quando o PRD-B entregar conteúdo que a justifique. **Aval necessário.** |
| QA-02 | Qual a base de coordenadas por cidade e quem a mantém? *(aberta no PRD)* | **Semente estática de municípios brasileiros (código IBGE, nome, UF, latitude, longitude), versionada no repositório e carregada por migration do catalog-service.** Sem integração externa, como a restrição técnica do PRD exige. Atualização é mudança de código, revisada como qualquer outra. |
| QA-03 | Qual o prazo de retenção dos favoritos no navegador? *(aberta no PRD)* | **Nenhum prazo programado.** Quem apaga é o comprador ou o navegador; a plataforma não expira favoritos. Limite técnico de 100 itens por navegador, para não estourar o `localStorage`. O aviso da DUX-08 é a mitigação, não a expiração. |
| QA-04 | Qual a granularidade da origem da navegação entregue a D03? *(aberta no PRD)* | **Enum `origem`** (`listagem`, `busca`, `favoritos`, `comparacao`, `link-direto`) + resumo dos filtros aplicados + posição no resultado. Sem histórico de navegação e sem qualquer identificador de pessoa (PD-001). Sujeito a confirmação pelo PRD de D03. |
| QA-05 | Quais atributos compõem a ficha técnica e a comparação? *(aberta no PRD)* | **Na Fase 1, apenas os atributos que D02 fornece** — ano de fabricação e modelo, quilometragem, cor, combustível, câmbio, cidade/UF — mais preço, distância e a transparência dos três blocos. A ficha ampliada é do PRD-B e entra sem quebrar o layout, porque as linhas vêm do servidor. |
| QA-06 | Existem estados de disponibilidade além dos três? *(aberta no PRD)* | **Não.** Confirmado no `api-contract.yaml` de D02: `EstadoDisponibilidade` é `[disponivel, reservado, vendido]`. D01 acrescenta apenas `indisponivel`, que é projeção da *situação* da oferta, não um estado novo de D02 (§3.1). |
| QA-07 | Quando D02 passará a fornecer carroceria? *(aberta no PRD)* | **Sem data.** A interface não decide isso: **o servidor declara quais filtros existem** e o cliente monta o painel a partir dessa resposta. No dia em que D02 fornecer carroceria, o filtro aparece sem alteração no frontend. |
| QA-08 | A geolocalização pode ser retida? *(depende da questão jurídica aberta)* | **Não é retida em lugar nenhum** — nem banco, nem log, nem `localStorage`. Só a cidade escolhida manualmente persiste, no navegador. Reduz a superfície ao mínimo enquanto a definição de LGPD do PRD segue pendente (§9). |
| QA-09 | A URL do veículo é amigável para busca orgânica? | **Não na Fase 1**: a rota usa o identificador do item. Slug e SEO dependem do título comercial, que é do PRD-B — construí-los agora significaria gerar URLs que mudariam quando o conteúdo real chegasse. |

---

## 9. O que continua em aberto

Duas coisas não foram decididas aqui porque não são decisões de UX:

| Questão | Dono | Impacto se não resolvida |
|---|---|---|
| Requisitos de LGPD para o consentimento de geolocalização | Product Owner com apoio jurídico | QA-08 é a postura mais conservadora possível e provavelmente sobrevive a qualquer parecer. Um parecer restritivo forçaria remover a geolocalização e manter só a escolha manual de cidade — o que a interface já suporta como caminho pleno (§3.2), sem retrabalho de tela |
| Contrato de captação de D03 (campos, consentimento, retenção) | PRD de D03 | O bloco de D03 em T04 fica como região delimitada. O registro do interesse em D01 **não** depende disso e já emite o evento |

---

## 10. Acessibilidade e responsividade

O PRD pede navegação por teclado, contraste suficiente e `Não informado` legível por
leitores de tela. Concretamente:

| Requisito | Como se cumpre |
|---|---|
| Navegação por teclado | Toda ação é `button` ou `a` real. Card de veículo tem um único alvo primário; favoritar e comparar são alvos próprios, na ordem de tabulação, nunca sobrepostos |
| Contraste | Mínimo AA (4.5:1) para texto e 3:1 para tarjas de status. Os pares tonais do §2 dos briefs foram escolhidos para isso |
| `Não informado` | Texto real no DOM, não `aria-hidden`, não pseudo-elemento CSS (DUX-03) |
| Tarja de reserva | Nunca comunicada só por cor: sempre acompanha o texto `Reservado` |
| Contagem de resultados | Região `aria-live="polite"` — filtrar sem recarregar precisa anunciar quantos itens restaram |
| Foco após navegação | Ao aplicar filtro, o foco vai para a contagem de resultados, não volta ao topo |
| Fotos | `alt` descritivo com marca, modelo e ângulo; placeholder é decorativo (`alt=""`) |

**Breakpoints**

| Faixa | Layout |
|---|---|
| ≥ 1280px | 3 colunas de card; filtros em coluna lateral fixa; T03 em duas colunas com decisão fixa |
| 768–1279px | 2 colunas; filtros em sheet |
| < 768px | 1 coluna; filtros em sheet de tela cheia; em T03 o card de preço e ação vira barra fixa no rodapé; M02 vira bottom sheet |

A barra fixa de preço e ação no mobile é o que preserva a métrica primária: sem ela,
`Tenho interesse` fica abaixo de três cards de conteúdo e o comprador precisa voltar a
rolar para agir.

---

## 11. Ajustes de frontend registrados (não executados)

Nada de `apps/` foi alterado neste planejamento. Estes itens viram tasks na etapa de
`tsg-flow-task-creator`.

| ID | Ajuste | Arquivo | Origem |
|---|---|---|---|
| AJ-01 | Traduzir o shell de EN para PT-BR: nav (`Home`, `Vehicles`), rodapé (`curated vehicle catalog`) | `apps/catalog/src/app/layouts/PublicLayout.tsx` | §1 |
| AJ-02 | Renomear rotas: `/vehicles` → `/`, `/vehicles/:id` → `/veiculos/:itemId`, `/vehicles/:id/interest` → `/veiculos/:itemId/interesse`; adicionar `/favoritos` e `/comparar` | `apps/catalog/src/app/router.tsx` | §4 |
| AJ-03 | Remover a `HomePage` e apontar `/` para a listagem | `apps/catalog/src/features/catalog/pages/HomePage.tsx`, `router.tsx` | DUX-01 / QA-01 |
| AJ-04 | Adicionar `Favoritos` e `Comparar` ao cabeçalho, com contagem lida do `localStorage` | `PublicLayout.tsx` | §4 |
| AJ-05 | **Substituir os tokens inferidos pelos do `DESIGN.md`.** Os valores atuais do `packages/ui` (primária azul `#2563eb`, superfície `#f8fafc`) contradizem o sistema real (Deep Navy `#2E2E3A`, Action Orange `#FC8422`, fundo `#f9f9ff`). Inclui a escala tipográfica, `data-tabular` e `label-caps` | `packages/ui/src/tokens/tokens.css`, `packages/ui/tailwind.preset.ts` | `DESIGN.md` |
| AJ-06 | Atualizar `.stitch/metadata.json`: `tokensSource` deixa de ser `inferred-minimal` | `.stitch/metadata.json` | `DESIGN.md` |
| AJ-07 | Criar os componentes compartilhados do §6 | `packages/ui`, `apps/catalog/src/shared/components` | §6 |
| AJ-08 | Criar o módulo do navegador identificado: gera e persiste o UUID, guarda favoritos, comparação e cidade de referência | `apps/catalog/src/shared/navegador/` | PD-002, §3.2 |
| AJ-09 | `apiFetch` precisa enviar o cabeçalho `X-Navegador-Id` nos `POST` de engajamento e interesse | `apps/catalog/src/shared/api/client.ts` | `api-contract.yaml` |

**AJ-05 é o mesmo ajuste registrado como AJ-04 no `ux-spec.md` de D02, e continua
pendente.** Enquanto não for feito, o HTML que sair do Stitch e o código de `apps/catalog`
usam paletas diferentes — o Stitch em Deep Navy e laranja, o código em azul. Não quebra
nada, e é exatamente por isso que passa despercebido até o primeiro componente parecer
"fora do lugar" sem motivo aparente.

---

*Próxima etapa: `api-contract.yaml` (OpenAPI 3.1).*
