# Catálogo Público e Descoberta

## Visão Geral

Esta funcionalidade transforma a oferta curada mantida por D02 em uma experiência pública de venda. O comprador final encontra veículos por marca, modelo, preço, quilometragem e proximidade, compreende o que a operação sabe e o que não sabe sobre cada carro, compara opções e manifesta interesse para que a operação central dê continuidade.

O PRD reúne F01, F02, F03, F05, F06, F07 e F08 do domínio D01 porque todas compartilham o mesmo usuário, a mesma superfície pública e um resultado único: levar o comprador da descoberta até uma manifestação de interesse com contexto suficiente para D03.

F04 (gestão de conteúdo comercial e mídia) e F09 (preço promocional) ficam fora deste documento e serão tratados em um PRD de backoffice, doravante **PRD-B**.

## Rastreabilidade

### Vision Doc

- **Objetivo atendido**: validar a jornada principal do comprador, da descoberta transparente até a demonstração de interesse (Fase 1 do roadmap).
- **Restrições aplicáveis**: alcance nacional com operação assistida e progressiva; catálogo curado ou simulado; nenhuma integração comercial obrigatória; requisitos de privacidade brasileiros ainda em aberto.
- **Non-Goals respeitados**: marketplace aberto, compra ou pagamento online, motos e outras categorias, certificação formal de condição ou histórico, substituição de legado.
- **Divergência resolvida**: o Vision registra "Autenticação: não aplicável neste momento". Este PRD mantém essa restrição — não haverá cadastro de comprador na Fase 1 (ver DP-02 e [PD-002](../../docs/product-decisions/PD-002-sem-identidade-de-comprador-na-fase-1.md)).

### Domain Doc

- **Domínio**: D01 — Catálogo e Descoberta.
- **Features**: F01 Publicação do catálogo e detalhe; F02 Busca, filtros, ordenação e localização; F03 Apresentação transparente; F05 Status e preço do item; F06 Favoritos; F07 Comparação de veículos; F08 Início de interesse.
- **Entidades**: Catálogo público, Item do catálogo, Conteúdo de apresentação, Ficha técnica, Preço apresentado, Favorito, Comparação, Status público do item.
- **Regras**: RN-01, RN-02, RN-05, RN-06, RN-07, RN-08, RN-09, RN-10, RN-11, RN-12.
- **Dependências upstream**: D02 Estoque Curado e Disponibilidade (obrigatória); PRD-B/F04 para conteúdo comercial, fotos e ficha técnica.
- **Dependências downstream**: D03 Interesse e Atendimento.
- **Eventos consumidos**: `estoque.oferta-incluida`, `estoque.oferta-atualizada`, `estoque.oferta-retirada`, `estoque.disponibilidade-alterada`.
- **Eventos produzidos**: `catalogo.item-publicado`, `catalogo.item-atualizado`, `catalogo.item-favoritado`, `catalogo.comparacao-realizada`, `catalogo.interesse-solicitado`.

### Alteração exigida em D02

O filtro por carroceria (F02) não pode ser atendido como está: a projeção `OfertaElegivel` de D02 entrega marca, modelo, versão, ano, quilometragem, cor, combustível, câmbio e localização, mas **não** carroceria. Por DP-07, carroceria passa a ser atributo de veículo mantido por D02. Requer atualizar o Domain Doc de D02 e acrescentar um campo opcional ao cadastro e à projeção. Enquanto não existir, o filtro por carroceria fica indisponível.

## Termos Canônicos

| Termo | Definição de negócio | Escopo/Fonte |
|---|---|---|
| Item do catálogo | Representação pública de uma oferta elegível recebida de D02, acrescida do conteúdo de apresentação mantido por D01. | Domain Doc D01 |
| Localização de referência | Ponto informado ou consentido pelo comprador, usado para calcular a distância até cada item. | Decisão desta feature |
| Distância apresentada | Distância aproximada entre a localização de referência e a cidade do veículo, exibida apenas quando a localização de referência existir. | Decisão desta feature |
| Navegador identificado | Marca local que permite reencontrar favoritos e comparações no mesmo dispositivo, sem constituir identidade de pessoa. | Domain Doc D01 (RN-07) |
| Contexto da descoberta | Conjunto formado pelo item, pelo preço e status exibidos no momento e pela origem da navegação, entregue a D03 ao iniciar um interesse. | Decisão desta feature |
| Não informado | Marcação exibida quando um dado não existe na origem, distinta de um dado existente e vazio. | Domain Doc D01 (RN-02) |

## Objetivos

- Permitir que uma pessoa encontre, compreenda e compare veículos do catálogo nacional curado sem contato prévio com a operação.
- Levar o comprador da descoberta a uma manifestação de interesse com contexto suficiente para D03 prosseguir, sem qualificação automática.
- Refletir preço, disponibilidade e retirada decididos por D02 em até uma hora após a alteração.
- Nunca apresentar um dado ausente como se fosse conhecido: toda lacuna aparece como `Não informado` e toda limitação declarada por D02 aparece junto do fato.
- Avaliar as metas nos primeiros 60 dias após o lançamento do MVP.

## Histórias de Usuário

- Como **comprador final**, quero filtrar por marca, modelo, preço, quilometragem e proximidade, para reduzir o catálogo nacional às opções viáveis para mim.
- Como **comprador final**, quero ver na página do veículo o que a operação sabe e o que não conseguiu apurar, para decidir com consciência das lacunas.
- Como **comprador final**, quero saber que um carro já está reservado antes de me interessar por ele, para não investir tempo em uma opção com alguém à frente.
- Como **comprador final**, quero salvar veículos e comparar até quatro deles lado a lado, para escolher sem reabrir várias páginas.
- Como **comprador final**, quero manifestar interesse a partir da página do veículo, para que a operação me procure já sabendo qual carro me interessou.
- Como **Operação central**, quero que o catálogo mostre apenas ofertas aprovadas como elegíveis, para não expor veículos incompletos ou retirados.

## Funcionalidades Principais

### RF-01: Publicação do catálogo e detalhe do item

**Descrição**: Publicar como item do catálogo toda oferta elegível recebida de D02 e permitir consultar sua apresentação completa.

**Critérios de Aceitação**:

- **Given** uma oferta aprovada como elegível em D02
  **When** o evento de inclusão for recebido
  **Then** o item se torna descobrível no catálogo e `catalogo.item-publicado` é emitido.

- **Given** uma oferta que D02 não fornece como elegível — em preparação, suspensa ou retirada
  **When** o comprador navegar ou buscar
  **Then** o item não aparece no catálogo e seu detalhe responde que não está disponível.

- **Given** um item publicado
  **When** D02 alterar fatos, preço ou dados do veículo
  **Then** a apresentação é atualizada em até uma hora e `catalogo.item-atualizado` é emitido.

- **Given** um item publicado sem conteúdo comercial mantido no PRD-B
  **When** o comprador abrir o detalhe
  **Then** os dados de D02 são exibidos normalmente e as áreas de título comercial, descrição, destaques e fotos ficam ausentes, sem bloquear a página.

**Prioridade**: Must Have
**Rastreabilidade**: RN-01, RN-03

### RF-02: Busca, filtros, ordenação e proximidade

**Descrição**: Permitir encontrar veículos por critérios objetivos e ordenar o resultado, com distância calculada a partir da localização de referência do comprador.

**Critérios de Aceitação**:

- **Given** o catálogo publicado
  **When** o comprador aplicar filtros de marca, modelo, ano, faixa de preço, faixa de quilometragem, combustível, câmbio, cidade ou UF
  **Then** o resultado contém apenas itens que satisfazem simultaneamente todos os critérios.

- **Given** um comprador que concedeu a permissão de localização do navegador
  **When** o resultado for exibido
  **Then** cada item mostra a distância aproximada até a cidade do veículo e a ordenação padrão é do mais próximo ao mais distante.

- **Given** um comprador que negou a permissão de localização ou cujo navegador não a suporta
  **When** o resultado for exibido
  **Then** a plataforma oferece a escolha manual de cidade e, enquanto não houver localização de referência, oculta a distância e ordena por publicação mais recente.

- **Given** um resultado exibido
  **When** o comprador escolher ordenar por preço, quilometragem, ano ou publicação mais recente
  **Then** a ordenação escolhida prevalece sobre a padrão.

- **Given** uma combinação de filtros sem nenhum item correspondente
  **When** o resultado for exibido
  **Then** a plataforma informa a ausência de resultados e indica quais filtros podem ser relaxados.

- **Given** que D02 ainda não fornece carroceria
  **When** o comprador acessar os filtros
  **Then** o filtro por carroceria não é oferecido.

**Prioridade**: Must Have
**Rastreabilidade**: RN-01, RN-10

### RF-03: Apresentação transparente

**Descrição**: Exibir os fatos conhecidos, suas limitações declaradas e a ficha técnica, marcando explicitamente o que não é conhecido.

**Critérios de Aceitação**:

- **Given** um fato de origem, condição ou histórico com conteúdo em D02
  **When** o detalhe for exibido
  **Then** o fato aparece acompanhado da fonte, quando houver, e da data de atualização.

- **Given** um fato marcado como indisponível em D02
  **When** o detalhe for exibido
  **Then** a limitação declarada pela operação é exibida no lugar do fato, sem substituí-la por texto genérico.

- **Given** um atributo de ficha técnica sem valor conhecido
  **When** o detalhe for exibido
  **Then** o atributo aparece como `Não informado`, e não é omitido nem preenchido por inferência.

- **Given** um item cuja apresentação não declara certificação
  **When** o comprador consultar o detalhe
  **Then** nenhuma linguagem de certificação, garantia formal ou vistoria aprovada é utilizada.

**Prioridade**: Must Have
**Rastreabilidade**: RN-02, RN-03, RN-12

### RF-04: Status público e preço apresentado

**Descrição**: Comunicar preço oficial, localização e os estados disponível, reservado e vendido conforme decidido por D02.

**Critérios de Aceitação**:

- **Given** um item disponível
  **When** o comprador consultá-lo
  **Then** o preço oficial vigente de D02 e a cidade/UF do veículo são exibidos, com a data da última atualização.

- **Given** um item que passou a reservado em D02
  **When** o comprador consultá-lo
  **Then** o item continua listável, favoritável, comparável e apto a gerar interesse, e informa que há alguém à frente.

- **Given** um item que passou a vendido em D02
  **When** o comprador buscar ou navegar
  **Then** o item deixa a listagem e a busca e seu detalhe responde que não está mais disponível.

- **Given** um item que passou a vendido e que já constava nos favoritos de um navegador identificado
  **When** esse comprador abrir seus favoritos
  **Then** o item permanece identificado exibindo somente o aviso de compra, sem fotos e sem acesso ao detalhe.

- **Given** um item retirado da oferta em D02
  **When** o evento de retirada for recebido
  **Then** o item deixa o catálogo em até uma hora.

**Prioridade**: Must Have
**Rastreabilidade**: RN-01, RN-08, RN-09

### RF-05: Início de interesse

**Descrição**: Oferecer a ação de manifestar interesse e entregar a D03 o contexto da descoberta, sem coletar nem reter dados pessoais em D01.

**Critérios de Aceitação**:

- **Given** um item disponível ou reservado
  **When** o comprador acionar a manifestação de interesse
  **Then** `catalogo.interesse-solicitado` é emitido com o item, o preço e o status exibidos naquele momento e a origem da navegação, e a continuidade é conduzida por D03.

- **Given** a manifestação encaminhada
  **When** D03 receber o contexto
  **Then** nenhum estado de qualificação é atribuído pelo catálogo.

- **Given** um item vendido ou retirado
  **When** o comprador tentar manifestar interesse
  **Then** a ação não é oferecida e a indisponibilidade é informada.

- **Given** qualquer manifestação
  **When** o encaminhamento ocorrer
  **Then** D01 não coleta, exibe nem retém nome, contato ou qualquer dado pessoal do comprador.

**Prioridade**: Must Have
**Rastreabilidade**: RN-11, RN-06

### RF-06: Favoritos por navegador

**Descrição**: Permitir salvar itens para consulta posterior sem cadastro, com persistência restrita ao navegador identificado.

**Critérios de Aceitação**:

- **Given** um item publicado
  **When** o comprador favoritá-lo
  **Then** o item passa a constar nos favoritos daquele navegador e `catalogo.item-favoritado` é emitido.

- **Given** favoritos existentes
  **When** o comprador acessá-los
  **Then** a plataforma informa explicitamente que os favoritos vivem apenas naquele navegador e podem ser perdidos.

- **Given** um item favoritado
  **When** o favorito for salvo
  **Then** nenhuma manifestação de interesse é criada e nenhum contato é iniciado.

- **Given** um item favoritado que foi retirado da oferta
  **When** o comprador abrir seus favoritos
  **Then** o item deixa de ser acessível e a ausência é informada.

**Prioridade**: Must Have
**Rastreabilidade**: RN-06, RN-07, RN-09

### RF-07: Comparação de veículos

**Descrição**: Permitir selecionar de 2 a 4 veículos e contrastar seus atributos disponíveis lado a lado.

**Critérios de Aceitação**:

- **Given** um único veículo selecionado
  **When** o comprador tentar comparar
  **Then** a comparação não é iniciada e a plataforma informa que são necessários pelo menos dois veículos.

- **Given** quatro veículos já selecionados
  **When** o comprador tentar adicionar um quinto
  **Then** a adição é recusada e o limite é informado.

- **Given** de 2 a 4 veículos selecionados
  **When** a comparação for exibida
  **Then** os atributos são contrastados linha a linha e `catalogo.comparacao-realizada` é emitido.

- **Given** um atributo ausente em um dos veículos comparados
  **When** a comparação for exibida
  **Then** a célula correspondente mostra `Não informado`, preservando o alinhamento das linhas.

- **Given** um veículo em comparação que passou a vendido
  **When** a comparação for reaberta
  **Then** o veículo é removido da comparação e a alteração é informada.

**Prioridade**: Must Have
**Rastreabilidade**: RN-05, RN-02, RN-09

## Experiência do Usuário

O comprador chega à listagem do catálogo nacional. Na primeira visita, a plataforma solicita a permissão de localização explicando que ela serve para mostrar os veículos mais próximos; se recusada, oferece a escolha manual de cidade e o catálogo continua plenamente utilizável, apenas sem distância nem ordenação por proximidade.

O resultado apresenta cada veículo com identificação, ano, quilometragem, preço, cidade/UF, distância quando conhecida e o status quando não for disponível. Filtros e ordenação ficam acessíveis sem perder a intenção de busca já expressa.

No detalhe, a apresentação separa visualmente o que vem da operação — origem, condição, histórico, preço, disponibilidade, sempre com data de atualização e limitações declaradas — do conteúdo comercial mantido no PRD-B. Lacunas aparecem como `Não informado`, nunca omitidas. Um veículo reservado exibe o aviso de que há alguém à frente sem perder nenhuma ação.

A manifestação de interesse parte do detalhe. D01 entrega o contexto e a jornada continua sob responsabilidade de D03; o comprador percebe uma continuidade única, ainda que a captação não pertença ao catálogo.

Favoritos e comparação vivem no navegador. A plataforma comunica essa limitação no momento em que ela importa — ao abrir a lista de favoritos — em vez de escondê-la.

Requisitos de acessibilidade: navegação completa por teclado, contraste suficiente para leitura dos avisos de status, e `Não informado` legível por leitores de tela como texto, não como ausência de conteúdo.

## Decisões de Produto

| ID | Decisão confirmada | Alternativas descartadas e motivo | Impacto no PRD | Registro |
|---|---|---|---|---|
| DP-01 | O PRD-A pressupõe o conteúdo comercial de F04 e especifica sua exibição; o PRD-B especifica sua criação. | Exibir só dados de D02, por contradizer RN-09 e enfraquecer a comparação; trazer F04 para cá, por misturar backoffice e jornada pública. | RF-01, RF-03; cria dependência PRD-B → PRD-A. | — |
| DP-02 | Não haverá cadastro de comprador na Fase 1; favoritos e comparação persistem por navegador identificado. | Cadastro no MVP, por adicionar identidade, retenção e LGPD que o Vision não resolveu; identidade emergindo no interesse, por misturar conversão com persistência. | RF-06, RF-07, rollout Fase 2. | [PD-002](../../docs/product-decisions/PD-002-sem-identidade-de-comprador-na-fase-1.md) |
| DP-03 | A localização é tratada como distância a partir do comprador, não apenas como cidade/UF. | Cidade e UF, recomendada por não exigir dado novo; região, por adicionar um nível de filtro sem resolver proximidade. | RF-02; exige coordenadas por cidade. | — |
| DP-04 | A localização de referência vem da geolocalização do navegador, com escolha manual de cidade quando negada. | Cidade em lista, recomendada por dispensar consentimento; CEP, por exigir base ou serviço externo fora da Fase 1. | RF-02; cria consentimento e risco LGPD. | — |
| DP-05 | Em F08, D01 entrega o contexto da descoberta e D03 capta o contato; D01 não coleta dado pessoal. | D01 coletar e encaminhar, por colocar dado pessoal em domínio de apresentação; canal direto, por perder rastreabilidade e esvaziar o evento. | RF-05. | [PD-001](../../docs/product-decisions/PD-001-dado-pessoal-do-comprador-pertence-a-d03.md) |
| DP-06 | Vendido sai da listagem e da busca, e sobrevive apenas nos favoritos de quem já o havia favoritado. | Visibilidade por período ou indefinida, por poluir o resultado com opções não seguíveis. | RF-04, RF-06. | — |
| DP-07 | Carroceria passa a ser atributo de veículo fornecido por D02. | Ficha técnica de D01, por separar carroceria de câmbio e combustível; remover o filtro, por retirar um critério central de busca. | RF-02; altera Domain Doc e contrato de D02. | — |
| DP-08 | A ordenação padrão é por proximidade, com recência quando não houver localização de referência. | Recência fixa, recomendada por estabilidade; menor preço, por enviesar o catálogo curado. | RF-02. | — |
| DP-09 | A métrica primária é a conversão de descoberta em interesse encaminhado a D03. | Transparência percebida, por depender do PRD-B; eficácia de descoberta, por medir o tamanho da oferta. | Objetivos, Métricas. | — |
| DP-10 | A jornada (RF-01 a RF-05) entra no MVP e o engajamento (RF-06, RF-07) na Fase 2. | Tudo em uma fase, por atrasar a validação até o fim; vertical sem busca, por tornar o catálogo pouco crível. | Rollout. | — |

## Restrições Técnicas de Alto Nível

- O catálogo apresenta exclusivamente ofertas fornecidas por D02 como elegíveis; D01 não é fonte de verdade sobre condição, histórico, disponibilidade ou preço oficial.
- Alterações aprovadas em D02 devem estar refletidas no catálogo em até uma hora.
- O cálculo de distância exige coordenadas por cidade. Essa base deve ser interna e estática; nenhuma integração externa obrigatória é autorizada na Fase 1.
- A geolocalização exige consentimento explícito e não deve ser retida além da sessão; a definição dos requisitos de LGPD permanece pendente.
- Dados curados ou simulados são aceitos na Fase 1.
- O catálogo é público e não exige autenticação de comprador.

## Não-Objetivos (Fora de Escopo)

- Gestão de conteúdo comercial, fotos, ficha técnica e materiais autorizados — F04, tratado no PRD-B.
- Preço promocional e sua vigência — F09, tratado no PRD-B.
- Cadastro, login, área logada e união de favoritos anônimos com identidade.
- Coleta, exibição ou retenção de dados pessoais do comprador — pertence a D03.
- Qualificação de interesse, atendimento, agendamento e test drive — D03.
- Compra assistida, financiamento, pagamento e documentação — D04.
- Marketplace aberto, motos e categorias fora de carros seminovos e usados.
- Certificação formal de condição ou histórico.
- Decisão sobre quais veículos entram ou saem da oferta — D02.
- Recomendação personalizada, ranking por relevância e alertas de novos veículos.

## Plano de Rollout Faseado

### MVP (Fase 1)

- **Funcionalidades incluídas**: RF-01, RF-02, RF-03, RF-04, RF-05.
- **Dependências para iniciar**: conteúdo comercial e fotos do PRD-B para os itens de vitrine; campo carroceria em D02 para o filtro correspondente.
- **Critérios de sucesso para avançar à Fase 2**: pelo menos 5% das sessões que abrem um detalhe terminam em manifestação de interesse encaminhada a D03; nenhuma divergência de preço ou disponibilidade entre catálogo e D02 acima de uma hora observada em 30 dias.

### Fase 2

- **Funcionalidades adicionais**: RF-06, RF-07.
- **Critérios de sucesso para avançar à Fase 3**: pelo menos 20% dos navegadores que favoritam ou comparam retornam ao catálogo dentro de 7 dias.

### Fase 3

- **Funcionalidades restantes**: identidade do comprador e favoritos persistentes com união dos favoritos anônimos, condicionada à definição de LGPD e ao resultado da Fase 2. Não faz parte deste PRD.

## Métricas de Sucesso

| Métrica | Definição | Alvo | Prazo |
|---|---|---|---|
| Conversão descoberta → interesse | Sessões que abrem um detalhe e emitem `catalogo.interesse-solicitado`, sobre o total de sessões que abrem um detalhe. | ≥ 5% | 60 dias após o MVP |
| Buscas com resultado | Buscas com ao menos um item retornado, sobre o total de buscas. | ≥ 85% | 60 dias após o MVP |
| Latência de reflexo de D02 | Tempo entre a aprovação em D02 e a alteração visível no catálogo, no percentil 95. | ≤ 1 hora | Contínuo |
| Completude da apresentação | Itens publicados cujos três blocos de fatos trazem conteúdo ou limitação declarada. | 100% | Contínuo |
| Adesão à localização | Sessões com localização de referência definida, por consentimento ou escolha manual. | ≥ 50% | 60 dias após o MVP |
| Retorno após engajamento | Navegadores que favoritaram ou compararam e retornam em 7 dias. | ≥ 20% | 60 dias após a Fase 2 |

## Riscos e Mitigações

| Risco | Mitigação |
|---|---|
| A recusa da geolocalização deixa a maioria das sessões sem distância, degradando a ordenação padrão. | Escolha manual de cidade sempre disponível e igualmente proeminente; ordenação por recência como comportamento pleno, não como estado degradado. |
| A dependência do PRD-B atrasa o MVP: um catálogo sem fotos não sustenta a jornada. | RF-01 exige que o detalhe funcione sem conteúdo comercial; a sequência de entrega prioriza o PRD-B para os itens de vitrine. |
| Favoritos anônimos são perdidos com limpeza de dados ou troca de dispositivo, frustrando o comprador. | Comunicar a limitação no momento de uso e medir o retorno em 7 dias antes de decidir sobre cadastro na Fase 3. |
| Status ou preço desatualizados em relação a D02 reduzem a confiança. | Consumo dos eventos como caminho quente, reconciliação periódica e exibição da data de atualização junto do preço. |
| A promessa de alcance nacional supera a capacidade de atendimento assistido. | Exibir sempre cidade/UF e distância; não prometer entrega, logística ou prazo no catálogo. |
| A geolocalização levanta exigências de LGPD ainda não definidas. | Consentimento explícito com finalidade declarada, sem retenção além da sessão, e definição jurídica antes da operação comercial real. |
| Carroceria depender de alteração em D02 já implementado. | O filtro não é oferecido enquanto o campo não existir; nenhum outro requisito depende dele. |

## Alternativas Consideradas

### Abordagem Escolhida: Jornada primeiro, engajamento depois

- **Descrição**: MVP com catálogo, busca com proximidade, transparência, status/preço e início de interesse; favoritos e comparação na Fase 2.
- **Por que foi escolhida**: coloca a métrica primária em campo já na primeira entrega e adia exatamente as funcionalidades cujo valor é limitado pela decisão de não ter identidade de comprador.

### Alternativa Rejeitada 1: Tudo em uma fase

- **Descrição**: as sete funcionalidades entregues juntas, espelhando o MoSCoW do Domain Doc e o padrão do PRD de D02.
- **Trade-offs**: jornada completa de uma vez, ao custo de nada ser validável até o fim.
- **Por que foi rejeitada**: a dependência do PRD-B para conteúdo e fotos atrasaria o conjunto inteiro, incluindo a parte que já poderia estar validando a conversão.

### Alternativa Rejeitada 2: Vertical mínimo sem busca

- **Descrição**: listagem simples, detalhe, status e interesse na primeira fase; busca e filtros na segunda.
- **Trade-offs**: feedback mais cedo, com escopo inicial muito menor.
- **Por que foi rejeitada**: um catálogo sem busca não é crível nem para validação, e a métrica primária ficaria contaminada pela impossibilidade de encontrar o veículo adequado.

## Questões em Aberto

- Qual a base de coordenadas por cidade e quem a mantém? Responsável: Product Owner com a Operação central. Prazo: antes da TechSpec. Impacto se não resolvido: RF-02 perde distância e ordenação padrão, caindo para recência.
- Quais são as exigências de LGPD para o consentimento de geolocalização e por quanto tempo a localização de referência pode ser mantida? Responsável: Product Owner com apoio jurídico. Prazo: antes do lançamento público. Impacto: bloqueia DP-04 e pode forçar o retorno à escolha manual de cidade.
- Qual o prazo de retenção dos favoritos no navegador? Responsável: Product Owner. Prazo: antes da Fase 2. Impacto: define o comportamento de expiração em RF-06.
- Quais atributos compõem a ficha técnica e quem autoriza sua origem? Responsável: PRD-B. Prazo: antes da Fase 2. Impacto: define as linhas da comparação em RF-07.
- Existem estados de disponibilidade além de disponível, reservado e vendido? Responsável: D02. Prazo: antes da TechSpec. Impacto: novos estados exigem nova regra de apresentação em RF-04.
- Quando D02 passará a fornecer carroceria? Responsável: time de D02. Prazo: antes do MVP. Impacto: o filtro correspondente permanece indisponível até lá.
- Qual a granularidade da origem da navegação entregue a D03 no contexto da descoberta? Responsável: PRD de D03. Prazo: antes da TechSpec. Impacto: define o conteúdo de `catalogo.interesse-solicitado`.

---

*PRD gerado com a skill `tsg-flow-prd-creator` em 2026-08-16, a partir de `docs/vision.md` e `domains/catalogo-descoberta/domain.md`.*
