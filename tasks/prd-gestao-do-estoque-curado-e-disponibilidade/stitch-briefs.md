# Briefs para o Stitch — Backoffice D02

> **Como usar:** cada bloco `▸ PROMPT` é para colar direto no Stitch. Cole sempre o
> **Preâmbulo (§1)** antes do prompt da tela — ele carrega o sistema visual e as regras
> que valem para todas.
>
> **Fonte:** `ux-spec.md` — estrutura, campos e estados vêm de lá. Os briefs não inventam
> conteúdo novo; personalize o visual à vontade, mas o que está em **negrito** nos prompts
> é contrato com o backend.
>
> **Destino do HTML:** salve em `.stitch/designs/<codigo>.html` (ex.: `t03-detalhe-oferta.html`).

---

## 1. Preâmbulo — colar antes de todo prompt

> ⚠️ **Pendente:** o bloco `SISTEMA VISUAL` abaixo está com os tokens *inferidos* de
> `packages/ui/src/tokens/tokens.css`. Quando você me passar o `DESIGN.md` do Stitch,
> substituo por ele. Se for gerar antes disso, use como está — a paleta bate com o código
> atual.

```
CONTEXTO
Backoffice web de uma plataforma de carros seminovos e usados chamada
"AutoTransparência". Usuário: operador de estoque profissional, uso diário e intenso,
desktop 1440px, sessões longas. Densidade informacional alta — priorize varredura
rápida e leitura de dados sobre respiro visual. Não é um app de consumidor.

IDIOMA
Todos os rótulos, textos e dados de exemplo em português do Brasil.
Moeda em BRL (R$ 87.900,00). Datas em DD/MM/AAAA.

SISTEMA VISUAL
- Fonte: Inter. Corpo 14px, rótulos de tabela 12px, títulos de página 30px semibold.
- Primária (ações, links, item ativo): #2563eb  · hover #1d4ed8
- Acento positivo (elegível, aprovado, disponível): #059669
- Perigo (suspensa, rejeitado, SLA estourado): #dc2626
- Atenção (pendente, reservado, limitação declarada): #d97706
- Neutro: fundo #ffffff, superfície #f8fafc, borda #e2e8f0, texto #0f172a,
  texto secundário #64748b
- Raio de canto 6px. Sombras sutis, apenas em cards elevados e modais.
- Badges: pill, fundo com 10% da cor, texto na cor cheia, 12px medium.

LAYOUT BASE (presente em todas as telas)
Sidebar fixa de 256px à esquerda, fundo #f8fafc, borda direita.
  Topo: "AutoTransparência" em azul semibold 18px, abaixo "Backoffice" em 12px cinza.
  Navegação: Painel · Estoque · Validação · Interesses · Compras
  O item ativo tem fundo azul #2563eb e texto branco.
  "Validação" exibe um badge circular vermelho com o número 7 à direita do rótulo.
Header de 64px no topo da área de conteúdo, fundo branco, borda inferior:
  à esquerda "Área de operação" em 14px cinza; à direita "Ana Souza" e um botão
  fantasma "Sair".
Conteúdo com padding de 32px.

NÃO FAZER
- Não inventar campos, colunas, filtros ou botões além dos listados.
- Não usar imagens ou fotos de veículos — este é o backoffice, não o catálogo público.
- Não usar gradientes, ilustrações decorativas ou ícones grandes coloridos.
- Sem modo escuro.
```

---

## 2. T01 — Lista do estoque

**Arquivo** `t01-lista-estoque.html` · **Rota** `/estoque` · Item ativo na sidebar: **Estoque**

▸ **PROMPT**

```
Tela "Estoque curado" — lista principal do estoque.

CABEÇALHO DA PÁGINA
Título "Estoque curado" (30px bold) e, abaixo, "142 veículos" em cinza.
À direita, botão primário azul "Cadastrar veículo" com ícone de +.

FAIXA DE FILTROS (card branco, borda, padding 16px, acima da tabela)
- Campo de busca com ícone de lupa, largura ~320px,
  placeholder "Buscar por placa, marca ou modelo"
- Grupo de chips de situação, selecionáveis, lado a lado:
  "Todas" (ativo) · "Em preparação" · "Elegível" · "Suspensa" · "Retirada"
- Select "Disponibilidade" com opções Todas / Disponível / Reservado / Vendido
- Select "UF"
- Link discreto "Limpar filtros" à direita

TABELA (card branco, cabeçalho cinza claro, linhas com borda inferior, hover cinza)
Colunas, nesta ordem:
1. VEÍCULO — duas linhas: marca + modelo + versão em 14px medium;
   abaixo a placa em 12px cinza monoespaçado
2. ANO — formato "2021/2022"
3. KM — "48.300 km"
4. LOCALIZAÇÃO — "Campinas/SP"
5. PREÇO OFICIAL — "R$ 87.900,00" alinhado à direita, medium
6. SITUAÇÃO — badge: Em preparação (cinza) · Elegível (verde) ·
   Suspensa (vermelho) · Retirada (cinza escuro)
7. DISPONIBILIDADE — badge: Disponível (verde) · Reservado (âmbar) · Vendido (cinza)
8. PENDÊNCIAS — quando houver, um pequeno badge âmbar com ícone de relógio e o
   tipo, ex. "Preço"; quando não houver, um traço cinza
9. ATUALIZADO EM — "14/08/2026" em 12px cinza

Mostre 8 linhas de exemplo cobrindo TODAS as combinações de situação e
disponibilidade, com carros brasileiros reais (Honda Civic EXL, Toyota Corolla XEI,
Jeep Compass Longitude, VW T-Cross Highline, Hyundai HB20 Comfort,
Chevrolet Onix LTZ, Fiat Pulse Drive, Renault Kwid Zen).
Pelo menos uma linha com pendência de "Preço" e uma com "Elegibilidade".

RODAPÉ DA TABELA
"Mostrando 1–8 de 142" à esquerda; paginação numérica à direita.
```

---

## 3. T02 — Cadastro de veículo

**Arquivo** `t02-cadastro-veiculo.html` · **Rota** `/estoque/novo` · Sidebar: **Estoque**

▸ **PROMPT**

```
Tela "Cadastrar veículo" — formulário de cadastro.

CABEÇALHO
Breadcrumb "Estoque / Cadastrar veículo".
Título "Cadastrar veículo".

AVISO INFORMATIVO (faixa azul clara #eff6ff, borda esquerda azul 3px, ícone ⓘ,
logo abaixo do título — NÃO é um erro, é informativo)
"Você pode salvar com dados parciais. Este cadastro ficará em preparação até que os
critérios mínimos sejam atendidos."

FORMULÁRIO em card branco, dividido em 4 seções com título de seção 16px semibold
e linha divisória. Campos em grade de 2 colunas. Nenhum campo marcado como
obrigatório com asterisco vermelho — em vez disso, os campos que compõem critério
mínimo levam um selo cinza discreto "critério mínimo" ao lado do rótulo.

1. IDENTIFICAÇÃO
   - Placa [critério mínimo] — placeholder "ABC1D23"
   - Chassi (VIN) — placeholder "opcional"

2. CATEGORIA
   - Tipo de veículo [critério mínimo] — select com apenas duas opções:
     "Carro seminovo" e "Carro usado".
     Abaixo do select, texto auxiliar cinza 12px:
     "Somente carros seminovos e usados compõem o estoque curado."

3. DADOS BÁSICOS
   - Marca [critério mínimo] · Modelo [critério mínimo]
   - Versão · Ano de fabricação [critério mínimo]
   - Ano modelo · Quilometragem [critério mínimo]
   - Cor · Combustível (select)
   - Câmbio (select) [critério mínimo]

4. LOCALIZAÇÃO
   - CEP · Cidade [critério mínimo] · UF (select) [critério mínimo]

BARRA DE AÇÕES fixa no rodapé do card, alinhada à direita:
botão fantasma "Cancelar" e botão primário azul "Salvar cadastro".
À esquerda da barra, em cinza 12px: "3 de 6 critérios atendidos".

Preencha o formulário parcialmente no exemplo: placa, tipo, marca e modelo
preenchidos; ano, quilometragem, cidade e UF vazios. É o estado real de uso.
```

---

## 4. T03 — Detalhe da oferta *(a tela mais importante)*

**Arquivo** `t03-detalhe-oferta.html` · **Rota** `/estoque/:id` · Sidebar: **Estoque**

▸ **PROMPT**

```
Tela "Detalhe da oferta" — painel de controle de um veículo. Layout de duas colunas:
principal 65%, lateral 35%, com 24px de espaçamento.

CABEÇALHO
Breadcrumb "Estoque / Honda Civic EXL 2.0".
Linha do título: "Honda Civic EXL 2.0" (30px bold) e, ao lado, dois badges:
"Elegível" (verde) e "Disponível" (verde).
Abaixo, em cinza 14px: "Placa ABC1D23 · 2021/2022 · 48.300 km · Campinas/SP".
À direita do cabeçalho, três ações:
botão primário azul "Solicitar elegibilidade",
botão secundário contornado "Solicitar retirada",
e um botão de ícone "⋯".

COLUNA PRINCIPAL (esquerda)

Card "Fatos conhecidos" — título com botão de texto "Editar fatos" à direita.
Três blocos empilhados, separados por linha divisória:

  ORIGEM — ✓ ícone verde
    "Veículo de frota corporativa, único proprietário pessoa jurídica."
    Linha cinza 12px: "Fonte: Contrato de cessão Localiza, 02/2026 · Ver evidência"

  CONDIÇÃO — ✓ ícone verde
    "Revisões em concessionária até 40.000 km. Pneus dianteiros trocados em 03/2026."
    Linha cinza 12px: "Fonte: Histórico de manutenção Honda · Ver evidência"

  HISTÓRICO — badge âmbar "Limitação declarada"
    Em itálico cinza: "Não foi possível obter o histórico de sinistros deste veículo
    junto às bases consultadas."
    Linha cinza 12px: "Declarado em 10/08/2026 por Ana Souza"

Card "Dados do veículo" — grade de leitura de 3 colunas, rótulo 12px cinza acima do
valor 14px: Placa, Chassi, Tipo, Marca, Modelo, Versão, Ano fab., Ano modelo,
Quilometragem, Cor, Combustível, Câmbio, Cidade, UF.
Botão de texto "Editar" no canto superior direito do card.

COLUNA LATERAL (direita), cards empilhados:

Card "Preço oficial"
  Valor "R$ 87.900,00" em 30px bold, cor escura.
  Abaixo, 12px cinza: "Atualizado em 12/08/2026 por Ana Souza".
  Botão contornado de largura total: "Solicitar alteração".

Card "Disponibilidade"
  Badge grande "Disponível" (verde).
  Abaixo, 12px cinza: "Desde 05/08/2026".
  Botão contornado de largura total: "Registrar reserva".
  Nota em 11px cinza no rodapé do card:
  "Retirar a oferta não altera a disponibilidade, e vice-versa."

Card "Critérios de elegibilidade"
  Cabeçalho com "5 de 6 atendidos" e uma barra de progresso fina.
  Lista de 6 itens, cada um com ícone à esquerda e rótulo:
  ✓ verde  Identificação
  ✓ verde  Dados básicos
  ✓ verde  Localização
  ✓ verde  Preço oficial
  ✓ verde  Disponibilidade conhecida
  ✗ vermelho  Transparência dos fatos — com sublinha em vermelho 12px:
     "Condição sem limitação declarada" e um link "Resolver"

Card "Pendências abertas"
  Uma entrada: badge âmbar "Preço", texto "R$ 87.900,00 → R$ 84.500,00",
  linha cinza 12px "Aberta por Carlos Lima há 4h".
```

---

## 5. T03-b — Variante: oferta suspensa

**Arquivo** `t03b-oferta-suspensa.html` — mesma tela, estado crítico

▸ **PROMPT**

```
Mesma tela do "Detalhe da oferta", com estas mudanças:

- O badge de situação no cabeçalho é "Suspensa" em VERMELHO.
- O botão primário "Solicitar elegibilidade" está DESABILITADO (cinza), com um
  tooltip visível apontando para ele: "Resolva o critério pendente para solicitar."
- No topo da coluna principal, ACIMA do card de fatos, um banner de alerta âmbar
  (fundo #fffbeb, borda esquerda âmbar 3px, ícone de triângulo de atenção):
    Título em semibold: "Elegibilidade suspensa"
    Corpo: "Em 15/08/2026, a remoção da fonte do bloco Condição fez esta oferta
    deixar de cumprir os critérios mínimos. Ela não está sendo fornecida ao catálogo.
    Corrija o critério e solicite nova validação."
- No card "Critérios de elegibilidade", o contador vira "5 de 6 atendidos" com a
  barra de progresso em vermelho.
- No bloco CONDIÇÃO do card de fatos, o ícone vira ✗ vermelho e o texto do bloco é
  substituído por, em vermelho 12px:
  "Sem conteúdo e sem limitação declarada."
```

---

## 6. T04 — Fatos conhecidos

**Arquivo** `t04-fatos-conhecidos.html` · **Rota** `/estoque/:id/fatos` · Sidebar: **Estoque**

▸ **PROMPT**

```
Tela "Fatos conhecidos" — edição dos fatos de um veículo.

CABEÇALHO
Breadcrumb "Estoque / Honda Civic EXL 2.0 / Fatos conhecidos".
Título "Fatos conhecidos".

AVISO PERMANENTE (faixa azul clara, ícone ⓘ, abaixo do título)
"Dado ausente não impede a elegibilidade — dado ausente sem limitação declarada, sim.
Nenhuma certificação formal é exigida nesta fase."

TRÊS CARDS empilhados, com a MESMA estrutura interna. Títulos:
"Origem", "Condição", "Histórico".

Estrutura de cada card:
  Linha do título: nome do bloco à esquerda; à direita, um switch com o rótulo
  "Informação indisponível".
  Campos abaixo:
    - Descrição — textarea de 4 linhas,
      placeholder "O que a operação sabe sobre este aspecto"
    - Fonte — input de texto,
      placeholder "Ex.: Laudo cautelar Auto Check, 03/2026"
    - Evidência — input de texto com ícone de link,
      placeholder "URL do documento ou laudo"

ESTADOS DIFERENTES POR CARD (importante — mostre os três):

  ORIGEM: switch DESLIGADO, campos preenchidos.
    Descrição: "Veículo de frota corporativa, único proprietário pessoa jurídica."
    Fonte: "Contrato de cessão Localiza, 02/2026"
    Evidência: "https://docs.autotransparencia.com.br/ev/8821"

  CONDIÇÃO: switch DESLIGADO, campos VAZIOS mostrando os placeholders.
    Abaixo do card, um texto de alerta vermelho 12px com ícone:
    "Sem conteúdo e sem limitação declarada, este bloco impede a elegibilidade."

  HISTÓRICO: switch LIGADO (azul). Os três campos acima estão colapsados/ocultos e,
    no lugar deles, aparece um único campo em destaque com fundo âmbar claro:
    - Limitação declarada (obrigatório) — textarea de 3 linhas, preenchida com:
      "Não foi possível obter o histórico de sinistros deste veículo junto às bases
      consultadas."
    Abaixo, em 12px cinza: "Esta limitação será exibida ao comprador no catálogo."

BARRA DE AÇÕES no rodapé, alinhada à direita:
botão fantasma "Cancelar" e botão primário azul "Salvar fatos".
```

---

## 7. M05 — Modal: solicitar alteração de preço

**Arquivo** `m05-modal-preco.html` — modal centrado sobre o T03 escurecido

▸ **PROMPT**

```
Modal centrado sobre a tela de detalhe da oferta, com o fundo escurecido a 50%.
Largura do modal: 520px. Card branco, raio 8px, sombra pronunciada.

CABEÇALHO DO MODAL
Título "Solicitar alteração de preço" e um botão × no canto direito.
Subtítulo cinza 14px: "Honda Civic EXL 2.0 · ABC1D23".

CORPO
Bloco de leitura com fundo cinza claro:
  Rótulo 12px cinza "PREÇO VIGENTE"
  Valor "R$ 87.900,00" em 24px semibold
  Linha 12px cinza: "Atualizado em 12/08/2026 por Ana Souza"

Campo "Novo preço oficial" — input com prefixo "R$", preenchido com "84.500,00",
fonte 18px.

Abaixo do input, uma linha de variação em cinza:
"Variação: −R$ 3.400,00 (−3,9%)"

Campo "Justificativa" — textarea de 3 linhas, com rótulo marcado como obrigatório,
preenchida com: "Ajuste para alinhar ao valor de mercado da região após 30 dias
sem manifestações de interesse."

NOTA (faixa azul clara, ícone ⓘ, abaixo dos campos)
"A alteração entra na fila de validação. O preço vigente continua valendo até a
aprovação."

RODAPÉ DO MODAL, alinhado à direita:
botão fantasma "Cancelar" e botão primário azul "Enviar para validação".
```

---

## 8. M06 — Modal: alterar disponibilidade

**Arquivo** `m06-modal-disponibilidade.html` — modal sobre o T03

▸ **PROMPT**

```
Modal centrado sobre a tela de detalhe da oferta, fundo escurecido a 50%.
Largura 520px.

Este modal mostra a transição "Registrar reserva".

CABEÇALHO
Título "Registrar reserva".
Subtítulo cinza: "Honda Civic EXL 2.0 · ABC1D23".

CORPO
Visualização da transição, centralizada e com destaque:
  badge verde "Disponível"  →  (seta cinza)  →  badge âmbar "Reservado"

Campo "Observação" — textarea de 3 linhas, rótulo com "(opcional)" em cinza,
placeholder "Contexto da reserva".

NOTA (faixa âmbar clara #fffbeb, ícone de atenção)
"A reserva não expira automaticamente. Liberar o veículo exigirá uma ação explícita
da operação."

NOTA SECUNDÁRIA em 11px cinza, abaixo:
"Retirar a oferta não altera a disponibilidade, e vice-versa."

RODAPÉ, alinhado à direita:
botão fantasma "Cancelar" e botão primário âmbar "Confirmar reserva".
```

---

## 9. T07 — Fila de validação

**Arquivo** `t07-fila-validacao.html` · **Rota** `/validacao` · Sidebar: **Validação** (ativo)

▸ **PROMPT**

```
Tela "Validação" — fila de trabalho do responsável por aprovar alterações.

CABEÇALHO
Título "Validação" e, abaixo, "7 solicitações pendentes" em cinza.

ABAS logo abaixo do título: "Pendentes (7)" (ativa, com sublinha azul) e
"Decididas".

FAIXA DE FILTROS (card branco)
Chips de tipo, selecionáveis, lado a lado:
"Todos" (ativo) · "Elegibilidade" · "Preço" · "Retirada" · "Reversão de venda"

TABELA (card branco), colunas:
1. VEÍCULO — marca + modelo em 14px medium; placa em 12px cinza monoespaçado abaixo
2. TIPO — badge colorido por tipo:
   Elegibilidade = azul · Preço = roxo · Retirada = cinza escuro ·
   Reversão de venda = vermelho
3. ALTERAÇÃO — mostra a transição em uma linha, com o valor vigente em cinza,
   uma seta, e o proposto em cor escura semibold. Exemplos:
   "Em preparação → Elegível" · "R$ 87.900,00 → R$ 84.500,00" ·
   "Elegível → Retirada" · "Vendido → Disponível"
4. SOLICITADO POR — nome em 14px; data e hora em 12px cinza abaixo
5. ABERTA HÁ — tempo decorrido. Até 24h em cinza normal ("4h", "18h").
   Acima de 24h, em VERMELHO semibold com um ícone de alerta ("1d 6h", "2d 3h").
6. AÇÕES — dois botões pequenos por linha: "Aprovar" (contornado verde) e
   "Rejeitar" (contornado vermelho)

Mostre 7 linhas cobrindo os quatro tipos, com pelo menos 2 linhas com o tempo em
vermelho estourando o SLA. Use carros brasileiros reais.

Abaixo da tabela, uma legenda discreta em 12px cinza com um quadrado vermelho:
"Acima de 1 dia útil — fora da meta de validação."
```

---

## 10. T08 — Detalhe da solicitação

**Arquivo** `t08-detalhe-solicitacao.html` · **Rota** `/validacao/:id` · Sidebar: **Validação**

▸ **PROMPT**

```
Tela "Detalhe da solicitação" — tela de decisão do responsável.
Coluna única centrada, largura máxima 880px.

CABEÇALHO
Breadcrumb "Validação / Solicitação #4821".
Linha do título: badge azul "Elegibilidade" e, ao lado, o título
"Honda Civic EXL 2.0" com um ícone de link externo.
Abaixo, em cinza 14px: "Placa ABC1D23 · Solicitada por Carlos Lima em 15/08/2026
às 09:12 · aberta há 1d 6h" — com "1d 6h" em vermelho semibold.

BLOCO DE COMPARAÇÃO (é o centro da tela — card com destaque, duas colunas)
Coluna esquerda, rótulo 12px cinza "VIGENTE":
  badge cinza "Em preparação"
Coluna direita, rótulo 12px cinza "PROPOSTO":
  badge verde "Elegível"
Entre as colunas, uma seta grande cinza.

Abaixo da comparação, dentro do mesmo card, o checklist de critérios em duas
colunas, todos com ✓ verde:
Identificação · Dados básicos · Localização · Preço oficial ·
Disponibilidade conhecida · Transparência dos fatos
Com o texto "6 de 6 critérios atendidos" em verde semibold acima.

CARD "Justificativa do solicitante"
Texto: "Cadastro completo, laudo de origem anexado e limitação de histórico
declarada. Veículo disponível no pátio de Campinas desde 05/08."

CARD "Contexto da oferta" — grade de leitura, 2 colunas:
  Preço oficial: R$ 87.900,00 (atualizado em 12/08/2026 por Ana Souza)
  Disponibilidade: badge verde "Disponível"
  Localização: Campinas/SP
  Fatos: "Origem e Condição preenchidos · Histórico com limitação declarada"
  — sendo "Histórico com limitação declarada" um badge âmbar.

CARD "Impacto ao aprovar" (fundo azul claro, borda esquerda azul 3px, ícone ⓘ)
"Ao aprovar, esta oferta passa a ser fornecida ao catálogo público em até 1 hora,
incluindo as limitações declaradas."

BARRA DE DECISÃO fixa no rodapé da tela, largura total, fundo branco, borda
superior, sombra para cima. À direita:
botão contornado vermelho "Rejeitar" e botão primário verde "Aprovar solicitação".
```

---

## 11. T08-b — Variante: rejeição com justificativa

**Arquivo** `t08b-rejeicao.html` — mesma tela, formulário de rejeição aberto

▸ **PROMPT**

```
Mesma tela do "Detalhe da solicitação", com o formulário de rejeição aberto.

Acima da barra de decisão, um card expandido com borda vermelha:
  Título "Rejeitar solicitação"
  Campo "Motivo da rejeição" — textarea de 4 linhas, rótulo marcado como
  obrigatório em vermelho, VAZIO, com placeholder
  "Explique o que precisa ser corrigido. O operador receberá esta mensagem."
  Abaixo, em 12px cinza: "A justificativa é obrigatória e será enviada a Carlos Lima."

Na barra de decisão, o botão "Aprovar solicitação" está desabilitado (cinza) e os
botões viram: fantasma "Cancelar" e primário vermelho
"Confirmar rejeição" — este último DESABILITADO, porque o campo está vazio.
```

---

## 12. Ordem sugerida de geração

| Ordem | Tela | Por quê |
|---|---|---|
| 1 | **T03** Detalhe da oferta | Define a linguagem de cards, badges e o card de critérios que reaparece no T08. Acerte essa primeiro. |
| 2 | **T01** Lista do estoque | Define a linguagem de tabela e filtros, reusada no T07. |
| 3 | **T07** Fila de validação | Herda a tabela do T01. |
| 4 | **T08** Detalhe da solicitação | Herda cards do T03 e o checklist. |
| 5 | T04, T02 | Formulários — herdam a linguagem de campos. |
| 6 | M05, M06 | Modais — os menores, herdam tudo. |
| 7 | T03-b, T08-b | Variantes de estado, geradas a partir das aprovadas. |

Total: **11 telas** (8 principais + 1 modal duplo + 2 variantes de estado).
