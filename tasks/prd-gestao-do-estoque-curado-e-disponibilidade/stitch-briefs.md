# Briefs para o Stitch — Backoffice D02

> **Como usar:** cada bloco `▸ PROMPT` é para colar direto no Stitch. Cole sempre o
> **Preâmbulo (§2)** antes do prompt da tela — ele carrega o sistema visual e as regras
> que valem para todas.
>
> **Fonte:** `ux-spec.md` (estrutura, campos, estados) + `DESIGN.md` (sistema visual).
> Os briefs não inventam conteúdo novo; personalize o visual à vontade, mas o que está
> em **negrito** nos prompts é contrato com o backend.
>
> **Destino do HTML:** salve em `.stitch/designs/<codigo>.html`.

---

## 1. Como o DESIGN.md foi adaptado

O `DESIGN.md` ("Autentico Brazil") descreve o sistema do **catálogo público (D01)**:
fotografia de veículos, *vehicle listing cards*, *lead capture forms*, CTAs de conversão
("Buy Now", "Schedule Inspection"), glassmorphism em overlay mobile.

O backoffice herda os **fundamentos** e descarta as **diretrizes de componente de consumidor**:

| Do DESIGN.md | No backoffice |
|---|---|
| Paleta completa (Deep Navy / Trust Green / Action Orange / neutros) | ✅ Herdada integralmente |
| Inter + escala tipográfica + `data-tabular` + `label-caps` | ✅ Herdada — `data-tabular` é ideal para as tabelas do estoque |
| Elevação por **outline de 1px `#CDCDDB`, sem sombra** | ✅ Herdada — é o que dá o ar "documento oficial" ao backoffice |
| Raio 4px em botões/inputs, 8px em cards | ✅ Herdado |
| Grid de 8px, gutter 24px, `stack-lg` entre seções | ✅ Herdado |
| Zebra-striping + bordas horizontais em tabelas de spec | ✅ Herdado nas tabelas de T01 e T07 |
| Fotografia de veículo como pilar da interface | ❌ Não se aplica — backoffice não exibe fotos |
| *Vehicle cards* com imagem e badge "Verified" | ❌ Substituídos por linhas de tabela densas |
| *Lead capture forms* flutuantes | ❌ Não existem aqui |
| CTA laranja para "conversão" | ⚠️ Reinterpretado — ver abaixo |

### Duas decisões de adaptação que precisam do seu aval

**DA-01 — Escassez do Action Orange.** O DESIGN.md reserva `#FC8422` "exclusivamente para
pontos primários de conversão". O backoffice não converte ninguém. Reinterpretei como:
**uma única ação laranja por tela** — aquela que compromete a operação (`Salvar cadastro`,
`Solicitar elegibilidade`, `Enviar para validação`). Todo o resto é outline navy ou ghost.
Se o laranja aparecer em dois botões da mesma tela, o brief está errado.

**DA-02 — Badges tonais vs. botão sólido.** O laranja de ação e o âmbar de status
("Reservado", "Pendente", "Limitação declarada") colidiriam. Resolvi por **peso**: badges
usam os pares *container* do sistema (fundo pálido + texto escuro), botões usam a cor
cheia. Um badge nunca é sólido saturado; um botão primário nunca é pálido.

**DA-03 — Aprovar é verde, não laranja.** Na tela de decisão (T08), o botão `Aprovar`
usa Trust Green `#018444`, não o laranja. É um desvio consciente: no sistema, verde
significa "verificado / confiança", que é exatamente o ato de aprovar. Me avise se
preferir o laranja por consistência de "ação primária".

---

## 2. Preâmbulo — colar antes de todo prompt

```
CONTEXTO
Backoffice web da "AutoTransparência", plataforma brasileira de carros seminovos e
usados. Usuário: operador de estoque profissional, uso diário e intenso, desktop
1440px, sessões longas. Densidade informacional alta — priorize varredura rápida e
leitura de dados. Não é um app de consumidor: não há fotos de veículos, não há
captura de lead, não há conversão. A marca é "transparência, expertise profissional e
qualidade curada" — o tom é de documento oficial, calmo e preciso, nunca promocional.

IDIOMA
Todos os rótulos, textos e dados de exemplo em português do Brasil.
Moeda em BRL (R$ 87.900,00). Datas em DD/MM/AAAA.

PALETA (usar exatamente estes valores)
- Deep Navy (estrutura, sidebar, texto principal): #2E2E3A
  variantes: #191925 (mais escuro), #464652 (texto navy secundário)
- Trust Green (verificado, elegível, disponível, aprovar): #018444
- Action Orange (ação primária, UMA por tela): #FC8422, texto branco
- Erro (suspensa, rejeitado, SLA estourado): #ba1a1a
- Fundo da página: #f9f9ff (off-white, nunca branco puro)
- Cards e superfícies elevadas: #ffffff
- Zebra de tabela / superfície sutil: #f3f3fb
- Borda: #CDCDDB
- Texto principal: #2E2E3A · Texto secundário: #4A4A4A · Texto sutil: #78767c

PARES TONAIS PARA BADGES (fundo pálido + texto escuro, NUNCA sólido saturado)
- Positivo (Elegível, Disponível, Aprovada):        fundo #8ef9ab, texto #00743b
- Atenção (Reservado, Pendente, Limitação):         fundo #ffdbc7, texto #733600
- Erro (Suspensa, Rejeitada, Reversão de venda):    fundo #ffdad6, texto #93000a
- Neutro (Em preparação, Retirada, Vendido):        fundo #e2e2ea, texto #47464c
- Navy (tipo "Elegibilidade"):                      fundo #e3e1f1, texto #464652

TIPOGRAFIA — Inter em tudo
- Título de página: 32px / peso 700 / entrelinha 40px / letter-spacing -0.01em
- Título de card:   16px / peso 600
- Corpo:            14px / peso 400 / entrelinha 20px
- Dados em tabela:  14px / peso 500, com algarismos tabulares (tabular figures) —
                    obrigatório em preços, quilometragem, datas e horas
- Rótulos de campo e cabeçalho de tabela: 12px / peso 700 / letter-spacing 0.05em /
                    CAIXA ALTA. Ex.: "PLACA", "QUILOMETRAGEM", "PREÇO OFICIAL"
- Texto auxiliar:   12px / peso 400 / cor #78767c

FORMA E ELEVAÇÃO
- Botões e inputs: raio 4px. Cards e modais: raio 8px.
- Cards NÃO têm sombra. Usam borda de 1px #CDCDDB sobre o fundo #f9f9ff.
  A profundidade vem do contraste entre o card branco e o fundo off-white.
- Apenas modais têm sombra: ambiente, larga, suave, baixa opacidade.
- Tabelas: bordas HORIZONTAIS apenas, sem linhas verticais. Zebra com #f3f3fb.

BOTÕES
- Primário (UMA ocorrência por tela): fundo #FC8422, texto branco, raio 4px
- Secundário: fundo transparente, borda 1px #2E2E3A, texto #2E2E3A
- Fantasma: sem borda nem fundo, texto #4A4A4A
- Destrutivo: borda 1px #ba1a1a, texto #ba1a1a

ESPAÇAMENTO
Base de 8px em tudo. Padding do conteúdo: 40px. Espaço entre cards: 24px.
Espaço entre seções lógicas: 32px. Largura máxima do conteúdo: 1280px.

LAYOUT BASE (presente em todas as telas)
Sidebar fixa de 256px à esquerda, fundo DEEP NAVY #2E2E3A, texto claro.
  Topo: "AutoTransparência" em branco, 18px semibold; abaixo "Backoffice" em 12px
  na cor #9795a4.
  Navegação: Painel · Estoque · Validação · Interesses · Compras
  Itens inativos em #9795a4; o item ATIVO tem fundo #191925 e texto branco.
  "Validação" exibe à direita um badge circular #ba1a1a com o número 7 em branco.
Header de 64px no topo da área de conteúdo, fundo #ffffff, borda inferior #CDCDDB:
  à esquerda "Área de operação" em 14px #4A4A4A; à direita "Ana Souza" em 14px e um
  botão fantasma "Sair".
Área de conteúdo com fundo #f9f9ff e padding de 40px.

NÃO FAZER
- Não inventar campos, colunas, filtros ou botões além dos listados.
- Não usar imagens, fotos de veículos ou ilustrações — este é o backoffice.
- Não usar gradientes, glassmorphism ou ícones grandes coloridos.
- Não usar azul: não existe azul neste sistema. Ação é laranja, estrutura é navy.
- Não colocar sombra em cards. Não colocar bordas verticais em tabelas.
- Sem modo escuro.
```

---

## 3. T01 — Lista do estoque

**Arquivo** `t01-lista-estoque.html` · **Rota** `/estoque` · Sidebar ativa: **Estoque**

▸ **PROMPT**

```
Tela "Estoque curado" — lista principal do estoque.

CABEÇALHO DA PÁGINA
Título "Estoque curado" (32px bold) e, abaixo, "142 veículos" em 14px #4A4A4A.
À direita, o botão primário LARANJA "Cadastrar veículo" com ícone de +.

FAIXA DE FILTROS (card branco, borda #CDCDDB, raio 8px, padding 16px)
- Campo de busca com ícone de lupa, largura ~320px,
  placeholder "Buscar por placa, marca ou modelo"
- Grupo de chips de situação, selecionáveis, lado a lado:
  "Todas" (ativo — fundo #2E2E3A, texto branco) · "Em preparação" · "Elegível" ·
  "Suspensa" · "Retirada" (inativos — borda #CDCDDB, texto #4A4A4A)
- Select "Disponibilidade": Todas / Disponível / Reservado / Vendido
- Select "UF"
- Botão fantasma "Limpar filtros" à direita

TABELA (card branco, raio 8px, bordas horizontais apenas, zebra #f3f3fb,
linhas alternadas, hover com fundo #ededf5)
Cabeçalho em CAIXA ALTA 12px peso 700 letter-spacing 0.05em, cor #78767c.
Colunas, nesta ordem:
1. VEÍCULO — duas linhas: marca + modelo + versão em 14px peso 500;
   abaixo a placa em 12px #78767c com algarismos tabulares
2. ANO — "2021/2022", tabular
3. KM — "48.300 km", tabular, alinhado à direita
4. LOCALIZAÇÃO — "Campinas/SP"
5. PREÇO OFICIAL — "R$ 87.900,00", tabular peso 500, alinhado à direita
6. SITUAÇÃO — badge tonal:
   Em preparação (#e2e2ea/#47464c) · Elegível (#8ef9ab/#00743b) ·
   Suspensa (#ffdad6/#93000a) · Retirada (#e2e2ea/#47464c)
7. DISPONIBILIDADE — badge tonal:
   Disponível (#8ef9ab/#00743b) · Reservado (#ffdbc7/#733600) ·
   Vendido (#e2e2ea/#47464c)
8. PENDÊNCIAS — quando houver, badge tonal #ffdbc7/#733600 com ícone de relógio e o
   tipo, ex. "Preço"; quando não houver, um traço "—" em #CDCDDB
9. ATUALIZADO EM — "14/08/2026" em 12px #78767c, tabular

Mostre 8 linhas de exemplo cobrindo TODAS as combinações de situação e
disponibilidade, com carros brasileiros reais (Honda Civic EXL, Toyota Corolla XEI,
Jeep Compass Longitude, VW T-Cross Highline, Hyundai HB20 Comfort,
Chevrolet Onix LTZ, Fiat Pulse Drive, Renault Kwid Zen).
Pelo menos uma linha com pendência de "Preço" e uma com "Elegibilidade".

RODAPÉ DA TABELA
"Mostrando 1–8 de 142" à esquerda em 12px #78767c; paginação numérica à direita,
página ativa com fundo #2E2E3A e texto branco.
```

---

## 4. T02 — Cadastro de veículo

**Arquivo** `t02-cadastro-veiculo.html` · **Rota** `/estoque/novo` · Sidebar: **Estoque**

▸ **PROMPT**

```
Tela "Cadastrar veículo" — formulário de cadastro.

CABEÇALHO
Breadcrumb "Estoque / Cadastrar veículo" em 12px #78767c.
Título "Cadastrar veículo" (32px bold).

AVISO INFORMATIVO (faixa #f3f3fb, borda esquerda de 3px em #2E2E3A, raio 4px,
ícone ⓘ, abaixo do título — é INFORMATIVO, não um erro)
"Você pode salvar com dados parciais. Este cadastro ficará em preparação até que os
critérios mínimos sejam atendidos."

FORMULÁRIO em card branco (borda #CDCDDB, raio 8px, sem sombra), dividido em 4
seções com título de seção 16px peso 600 e linha divisória #CDCDDB.
Campos em grade de 2 colunas, espaçamento de 24px.
Rótulos de campo em CAIXA ALTA 12px peso 700 letter-spacing 0.05em, cor #78767c.
Inputs com borda 1px #CDCDDB, raio 4px, altura 40px, fundo branco.
NENHUM asterisco vermelho. Em vez disso, os campos que compõem critério mínimo
levam um selo tonal cinza discreto "critério mínimo" (#e2e2ea/#47464c, 11px) ao
lado do rótulo.

1. IDENTIFICAÇÃO
   - PLACA [critério mínimo] — placeholder "ABC1D23"
   - CHASSI (VIN) — placeholder "opcional"

2. CATEGORIA
   - TIPO DE VEÍCULO [critério mínimo] — select com apenas duas opções:
     "Carro seminovo" e "Carro usado".
     Texto auxiliar abaixo, 12px #78767c:
     "Somente carros seminovos e usados compõem o estoque curado."

3. DADOS BÁSICOS
   - MARCA [critério mínimo] · MODELO [critério mínimo]
   - VERSÃO · ANO DE FABRICAÇÃO [critério mínimo]
   - ANO MODELO · QUILOMETRAGEM [critério mínimo]
   - COR · COMBUSTÍVEL (select)
   - CÂMBIO (select) [critério mínimo]

4. LOCALIZAÇÃO
   - CEP · CIDADE [critério mínimo] · UF (select) [critério mínimo]

BARRA DE AÇÕES no rodapé do card, com borda superior #CDCDDB:
à esquerda, em 12px #78767c: "3 de 6 critérios atendidos".
à direita: botão fantasma "Cancelar" e botão primário LARANJA "Salvar cadastro".

No exemplo, preencha o formulário PARCIALMENTE: placa, tipo, marca e modelo
preenchidos; ano, quilometragem, cidade e UF vazios mostrando placeholder.
É o estado real de uso.
```

---

## 5. T03 — Detalhe da oferta *(a tela mais importante)*

**Arquivo** `t03-detalhe-oferta.html` · **Rota** `/estoque/:id` · Sidebar: **Estoque**

▸ **PROMPT**

```
Tela "Detalhe da oferta" — painel de controle de um veículo.
Duas colunas: principal 65%, lateral 35%, com 24px entre elas.
Todos os cards: fundo branco, borda 1px #CDCDDB, raio 8px, SEM sombra, padding 24px.

CABEÇALHO
Breadcrumb "Estoque / Honda Civic EXL 2.0" em 12px #78767c.
Linha do título: "Honda Civic EXL 2.0" (32px bold #2E2E3A) e, ao lado, dois badges
tonais: "Elegível" (#8ef9ab/#00743b) e "Disponível" (#8ef9ab/#00743b).
Abaixo, 14px #4A4A4A com algarismos tabulares:
"Placa ABC1D23 · 2021/2022 · 48.300 km · Campinas/SP".
À direita do cabeçalho, três ações alinhadas:
  botão primário LARANJA "Solicitar elegibilidade",
  botão secundário (borda navy) "Solicitar retirada",
  botão de ícone "⋯" fantasma.

COLUNA PRINCIPAL (esquerda)

Card "Fatos conhecidos" — título 16px/600 com botão fantasma "Editar fatos" à direita.
Três blocos empilhados, separados por linha #CDCDDB, com 16px de respiro:

  ORIGEM (rótulo em CAIXA ALTA 12px/700 #78767c) — ícone ✓ verde #018444
    "Veículo de frota corporativa, único proprietário pessoa jurídica."
    Linha 12px #78767c: "Fonte: Contrato de cessão Localiza, 02/2026 · Ver evidência"

  CONDIÇÃO — ícone ✓ verde #018444
    "Revisões em concessionária até 40.000 km. Pneus dianteiros trocados em 03/2026."
    Linha 12px #78767c: "Fonte: Histórico de manutenção Honda · Ver evidência"

  HISTÓRICO — badge tonal "Limitação declarada" (#ffdbc7/#733600)
    Em itálico #4A4A4A: "Não foi possível obter o histórico de sinistros deste veículo
    junto às bases consultadas."
    Linha 12px #78767c: "Declarado em 10/08/2026 por Ana Souza"

Card "Dados do veículo" — grade de leitura de 3 colunas.
Rótulo em CAIXA ALTA 12px/700 #78767c acima do valor em 14px/500 #2E2E3A.
Campos: PLACA, CHASSI, TIPO, MARCA, MODELO, VERSÃO, ANO FAB., ANO MODELO,
QUILOMETRAGEM, COR, COMBUSTÍVEL, CÂMBIO, CIDADE, UF.
Botão fantasma "Editar" no canto superior direito do card.

COLUNA LATERAL (direita), cards empilhados com 24px entre eles:

Card "Preço oficial"
  Valor "R$ 87.900,00" em 32px bold #2E2E3A, com algarismos tabulares.
  Abaixo, 12px #78767c: "Atualizado em 12/08/2026 por Ana Souza".
  Botão secundário (borda navy) de largura total: "Solicitar alteração".

Card "Disponibilidade"
  Badge tonal grande "Disponível" (#8ef9ab/#00743b).
  Abaixo, 12px #78767c: "Desde 05/08/2026".
  Botão secundário de largura total: "Registrar reserva".
  Nota no rodapé do card, 11px #78767c:
  "Retirar a oferta não altera a disponibilidade, e vice-versa."

Card "Critérios de elegibilidade"
  Cabeçalho: "5 de 6 atendidos" em 14px/600 e uma barra de progresso fina de 4px,
  preenchida em verde #018444 a 83%, trilho #e2e2ea.
  Lista de 6 itens, ícone à esquerda:
    ✓ #018444  Identificação
    ✓ #018444  Dados básicos
    ✓ #018444  Localização
    ✓ #018444  Preço oficial
    ✓ #018444  Disponibilidade conhecida
    ✗ #ba1a1a  Transparência dos fatos
       sublinha em 12px #ba1a1a: "Condição sem limitação declarada"
       e um link "Resolver" em #2E2E3A sublinhado

Card "Pendências abertas"
  Uma entrada: badge tonal "Preço" (#ffdbc7/#733600),
  texto "R$ 87.900,00 → R$ 84.500,00" em 14px/500 tabular,
  linha 12px #78767c "Aberta por Carlos Lima há 4h".
```

---

## 6. T03-b — Variante: oferta suspensa

**Arquivo** `t03b-oferta-suspensa.html`

▸ **PROMPT**

```
Mesma tela do "Detalhe da oferta", com estas mudanças:

- O badge de situação no cabeçalho vira "Suspensa" no par de ERRO (#ffdad6/#93000a).
- O botão primário laranja "Solicitar elegibilidade" está DESABILITADO: fundo
  #e2e2ea, texto #78767c, sem cor de ação. Um tooltip visível aponta para ele:
  "Resolva o critério pendente para solicitar."
- No TOPO da coluna principal, ACIMA do card de fatos, um banner de alerta:
  fundo #ffdad6, borda esquerda de 3px em #ba1a1a, raio 8px, ícone de triângulo
  de atenção em #ba1a1a.
    Título 16px/600 #93000a: "Elegibilidade suspensa"
    Corpo 14px #93000a: "Em 15/08/2026, a remoção da fonte do bloco Condição fez
    esta oferta deixar de cumprir os critérios mínimos. Ela não está sendo fornecida
    ao catálogo. Corrija o critério e solicite nova validação."
- No card "Critérios de elegibilidade", a barra de progresso fica VERMELHA #ba1a1a.
- No bloco CONDIÇÃO do card de fatos, o ícone vira ✗ #ba1a1a e o texto é substituído
  por, em 12px #ba1a1a: "Sem conteúdo e sem limitação declarada."
```

---

## 7. T04 — Fatos conhecidos

**Arquivo** `t04-fatos-conhecidos.html` · **Rota** `/estoque/:id/fatos` · Sidebar: **Estoque**

▸ **PROMPT**

```
Tela "Fatos conhecidos" — edição dos fatos de um veículo.
Coluna única, largura máxima 880px.

CABEÇALHO
Breadcrumb "Estoque / Honda Civic EXL 2.0 / Fatos conhecidos" em 12px #78767c.
Título "Fatos conhecidos" (32px bold).

AVISO PERMANENTE (faixa #f3f3fb, borda esquerda 3px #2E2E3A, ícone ⓘ)
"Dado ausente não impede a elegibilidade — dado ausente sem limitação declarada, sim.
Nenhuma certificação formal é exigida nesta fase."

TRÊS CARDS empilhados (branco, borda #CDCDDB, raio 8px, sem sombra, 24px entre eles),
com a MESMA estrutura interna. Títulos: "Origem", "Condição", "Histórico".

Estrutura de cada card:
  Linha do título: nome do bloco em 16px/600 à esquerda; à direita, um switch com o
  rótulo "Informação indisponível" em 14px #4A4A4A.
  Campos abaixo, rótulos em CAIXA ALTA 12px/700 #78767c:
    - DESCRIÇÃO — textarea de 4 linhas, borda #CDCDDB, raio 4px,
      placeholder "O que a operação sabe sobre este aspecto"
    - FONTE — input de texto,
      placeholder "Ex.: Laudo cautelar Auto Check, 03/2026"
    - EVIDÊNCIA — NÃO é um input de texto. É uma zona de upload de arquivo:
      retângulo com borda TRACEJADA 1px #CDCDDB, raio 4px, altura 88px, fundo
      #f9f9ff, conteúdo centralizado: ícone de clipe em #78767c, texto 14px
      "Arraste um arquivo ou clique para selecionar" e, abaixo, 12px #78767c
      "PDF, JPG ou PNG · até 10 MB".
      Ao lado do rótulo EVIDÊNCIA, "(opcional)" em 12px #78767c.

ESTADOS DIFERENTES POR CARD (mostre os três; Origem e Condição demonstram dois
estados distintos de upload, e Histórico não tem upload por estar colapsado):

  ORIGEM: switch DESLIGADO (trilho #CDCDDB), campos preenchidos.
    Descrição: "Veículo de frota corporativa, único proprietário pessoa jurídica."
    Fonte: "Contrato de cessão Localiza, 02/2026"
    Evidência: ARQUIVO JÁ ANEXADO — em vez da zona tracejada, um chip retangular
    com fundo #f3f3fb, borda #CDCDDB, raio 4px, altura 56px, contendo:
    ícone de PDF em #ba1a1a à esquerda; ao centro, em duas linhas,
    "contrato-cessao-localiza.pdf" em 14px/500 e "PDF · 1,2 MB · enviado em
    10/08/2026" em 12px #78767c; à direita, dois botões de ícone fantasma:
    baixar e remover (o remover em #ba1a1a).

  CONDIÇÃO: switch DESLIGADO, campos VAZIOS mostrando placeholders.
    O card tem borda #ba1a1a em vez de #CDCDDB.
    Evidência: UPLOAD EM ANDAMENTO — chip com o nome "laudo-cautelar.pdf",
    uma barra de progresso fina de 4px preenchida a 60% em #FC8422 sobre trilho
    #e2e2ea, e "60% · 2,4 MB de 4,0 MB" em 12px #78767c. Um botão × de cancelar
    à direita.
    No rodapé do card, alerta 12px #ba1a1a com ícone:
    "Sem conteúdo e sem limitação declarada, este bloco impede a elegibilidade."

  HISTÓRICO: switch LIGADO (trilho #018444). Os três campos acima estão colapsados e,
    no lugar deles, um único campo em destaque com fundo #ffdbc7 e raio 4px:
    - LIMITAÇÃO DECLARADA (obrigatório) — textarea de 3 linhas, preenchida com:
      "Não foi possível obter o histórico de sinistros deste veículo junto às bases
      consultadas."
    Abaixo, 12px #733600: "Esta limitação será exibida ao comprador no catálogo."

BARRA DE AÇÕES no rodapé, alinhada à direita:
botão fantasma "Cancelar" e botão primário LARANJA "Salvar fatos".
```

---

## 8. M05 — Modal: solicitar alteração de preço

**Arquivo** `m05-modal-preco.html`

▸ **PROMPT**

```
Modal centrado sobre a tela de detalhe da oferta, com o fundo escurecido em navy
#191925 a 50% de opacidade. Largura do modal: 520px. Card branco, raio 8px,
com sombra ambiente ampla e suave (única exceção à regra de "sem sombra").

CABEÇALHO DO MODAL
Título "Solicitar alteração de preço" em 24px/600. Botão × fantasma à direita.
Subtítulo 14px #4A4A4A: "Honda Civic EXL 2.0 · ABC1D23".

CORPO
Bloco de leitura com fundo #f3f3fb, raio 4px, padding 16px:
  Rótulo CAIXA ALTA 12px/700 #78767c: "PREÇO VIGENTE"
  Valor "R$ 87.900,00" em 24px/600 tabular
  Linha 12px #78767c: "Atualizado em 12/08/2026 por Ana Souza"

Campo "NOVO PREÇO OFICIAL" — input com prefixo "R$", preenchido com "84.500,00",
fonte 18px tabular, borda #CDCDDB, raio 4px.

Abaixo do input, linha de variação em 12px #ba1a1a:
"Variação: −R$ 3.400,00 (−3,9%)"

Campo "JUSTIFICATIVA" com selo "obrigatório" — textarea de 3 linhas, preenchida com:
"Ajuste para alinhar ao valor de mercado da região após 30 dias sem manifestações
de interesse."

NOTA (faixa #f3f3fb, borda esquerda 3px #2E2E3A, ícone ⓘ, 12px)
"A alteração entra na fila de validação. O preço vigente continua valendo até a
aprovação."

RODAPÉ, alinhado à direita:
botão fantasma "Cancelar" e botão primário LARANJA "Enviar para validação".
```

---

## 9. M06 — Modal: alterar disponibilidade

**Arquivo** `m06-modal-disponibilidade.html`

▸ **PROMPT**

```
Modal centrado sobre a tela de detalhe da oferta, fundo escurecido em navy #191925
a 50%. Largura 520px, card branco, raio 8px, sombra ambiente suave.

Este modal mostra a transição "Registrar reserva".

CABEÇALHO
Título "Registrar reserva" em 24px/600.
Subtítulo 14px #4A4A4A: "Honda Civic EXL 2.0 · ABC1D23".

CORPO
Visualização da transição, centralizada, com 32px de respiro acima e abaixo:
  badge tonal "Disponível" (#8ef9ab/#00743b)
  →  seta em #78767c
  badge tonal "Reservado" (#ffdbc7/#733600)

Campo "OBSERVAÇÃO" com "(opcional)" em #78767c ao lado — textarea de 3 linhas,
placeholder "Contexto da reserva".

NOTA (faixa #ffdbc7, borda esquerda 3px #FC8422, ícone de atenção, texto #733600)
"A reserva não expira automaticamente. Liberar o veículo exigirá uma ação explícita
da operação."

NOTA SECUNDÁRIA em 11px #78767c, abaixo:
"Retirar a oferta não altera a disponibilidade, e vice-versa."

RODAPÉ, alinhado à direita:
botão fantasma "Cancelar" e botão primário LARANJA "Confirmar reserva".
```

---

## 10. T07 — Fila de validação

**Arquivo** `t07-fila-validacao.html` · **Rota** `/validacao` · Sidebar: **Validação** (ativo)

▸ **PROMPT**

```
Tela "Validação" — fila de trabalho do responsável por aprovar alterações.
Na sidebar, "Validação" é o item ATIVO (fundo #191925, texto branco), com o badge
circular vermelho "7".

CABEÇALHO
Título "Validação" (32px bold) e, abaixo, "7 solicitações pendentes" em 14px #4A4A4A.

ABAS logo abaixo do título: "Pendentes (7)" ATIVA (texto #2E2E3A com sublinha de 2px
em #FC8422) e "Decididas" (texto #78767c, sem sublinha).

FAIXA DE FILTROS (card branco, borda #CDCDDB, raio 8px)
Chips de tipo, selecionáveis: "Todos" (ativo, fundo #2E2E3A texto branco) ·
"Elegibilidade" · "Preço" · "Retirada" · "Reversão de venda"

TABELA (card branco, raio 8px, bordas horizontais apenas, zebra #f3f3fb).
Cabeçalho em CAIXA ALTA 12px/700 #78767c. Colunas:
1. VEÍCULO — marca + modelo em 14px/500; placa em 12px #78767c tabular abaixo
2. TIPO — badge tonal por tipo:
   Elegibilidade (#e3e1f1/#464652) · Preço (#ffdbc7/#733600) ·
   Retirada (#e2e2ea/#47464c) · Reversão de venda (#ffdad6/#93000a)
3. ALTERAÇÃO — a transição em uma linha: valor vigente em #78767c, seta,
   e o proposto em #2E2E3A peso 500, tudo tabular. Exemplos:
   "Em preparação → Elegível" · "R$ 87.900,00 → R$ 84.500,00" ·
   "Elegível → Retirada" · "Vendido → Disponível"
4. SOLICITADO POR — nome em 14px; data e hora em 12px #78767c tabular abaixo
5. ABERTA HÁ — tempo decorrido, tabular. Até 24h em #4A4A4A ("4h", "18h").
   Acima de 24h em #ba1a1a peso 600 com ícone de alerta ("1d 6h", "2d 3h").
6. AÇÕES — dois botões pequenos: "Aprovar" (borda #018444, texto #018444) e
   "Rejeitar" (borda #ba1a1a, texto #ba1a1a)

Mostre 7 linhas cobrindo os quatro tipos, com pelo menos 2 linhas com o tempo em
vermelho estourando o SLA. Use carros brasileiros reais.

Abaixo da tabela, legenda em 12px #78767c com um quadrado #ba1a1a:
"Acima de 1 dia útil — fora da meta de validação."
```

---

## 11. T08 — Detalhe da solicitação

**Arquivo** `t08-detalhe-solicitacao.html` · **Rota** `/validacao/:id` · Sidebar: **Validação**

▸ **PROMPT**

```
Tela "Detalhe da solicitação" — tela de decisão do responsável.
Coluna única centrada, largura máxima 880px. Cards com borda #CDCDDB, raio 8px,
sem sombra, 24px entre eles.

CABEÇALHO
Breadcrumb "Validação / Solicitação #4821" em 12px #78767c.
Linha do título: badge tonal "Elegibilidade" (#e3e1f1/#464652) e, ao lado, o título
"Honda Civic EXL 2.0" (32px bold) com um ícone de link externo.
Abaixo, 14px #4A4A4A tabular: "Placa ABC1D23 · Solicitada por Carlos Lima em
15/08/2026 às 09:12 · aberta há 1d 6h" — com "1d 6h" em #ba1a1a peso 600.

BLOCO DE COMPARAÇÃO (é o centro da tela — card com fundo #f3f3fb em vez de branco,
para se destacar dos demais)
Duas colunas com uma seta grande #78767c entre elas:
  Esquerda, rótulo CAIXA ALTA 12px/700 #78767c "VIGENTE":
    badge tonal "Em preparação" (#e2e2ea/#47464c)
  Direita, rótulo "PROPOSTO":
    badge tonal "Elegível" (#8ef9ab/#00743b)

Abaixo, dentro do mesmo card, separado por linha #CDCDDB:
  "6 de 6 critérios atendidos" em 14px/600 #018444
  e o checklist em duas colunas, todos com ✓ #018444:
  Identificação · Dados básicos · Localização · Preço oficial ·
  Disponibilidade conhecida · Transparência dos fatos

CARD "Justificativa do solicitante"
"Cadastro completo, laudo de origem anexado e limitação de histórico declarada.
Veículo disponível no pátio de Campinas desde 05/08."

CARD "Contexto da oferta" — grade de leitura de 2 colunas, rótulos em CAIXA ALTA:
  PREÇO OFICIAL: R$ 87.900,00 · abaixo em 12px #78767c
    "Atualizado em 12/08/2026 por Ana Souza"
  DISPONIBILIDADE: badge tonal "Disponível" (#8ef9ab/#00743b)
  LOCALIZAÇÃO: Campinas/SP
  FATOS: "Origem e Condição preenchidos" seguido do badge tonal
    "Histórico com limitação declarada" (#ffdbc7/#733600)

CARD "Impacto ao aprovar" (fundo #f3f3fb, borda esquerda 3px #2E2E3A, ícone ⓘ)
"Ao aprovar, esta oferta passa a ser fornecida ao catálogo público em até 1 hora,
incluindo as limitações declaradas."

BARRA DE DECISÃO fixa no rodapé da janela, largura total, fundo branco,
borda superior #CDCDDB, sombra suave para cima. Alinhada à direita:
botão destrutivo (borda #ba1a1a, texto #ba1a1a) "Rejeitar" e
botão sólido VERDE #018444 com texto branco "Aprovar solicitação".
```

---

## 12. T08-b — Variante: rejeição com justificativa

**Arquivo** `t08b-rejeicao.html`

▸ **PROMPT**

```
Mesma tela do "Detalhe da solicitação", com o formulário de rejeição aberto.

Acima da barra de decisão, um card expandido com borda 1px #ba1a1a e raio 8px:
  Título "Rejeitar solicitação" em 16px/600 #93000a
  Campo "MOTIVO DA REJEIÇÃO" com selo "obrigatório" em #ba1a1a — textarea de
  4 linhas, VAZIA, borda #CDCDDB, placeholder:
  "Explique o que precisa ser corrigido. O operador receberá esta mensagem."
  Abaixo, 12px #78767c: "A justificativa é obrigatória e será enviada a Carlos Lima."

Na barra de decisão, o botão verde "Aprovar solicitação" está DESABILITADO
(fundo #e2e2ea, texto #78767c). Os botões viram:
fantasma "Cancelar" e sólido #ba1a1a com texto branco "Confirmar rejeição" —
este último DESABILITADO (fundo #e2e2ea, texto #78767c), porque o campo está vazio.
```

---

## 13. Ordem sugerida de geração

| Ordem | Tela | Por quê |
|---|---|---|
| 1 | **T03** Detalhe da oferta | Define cards, badges tonais e o checklist que reaparece no T08. Acerte essa primeiro. |
| 2 | **T01** Lista do estoque | Define a linguagem de tabela e filtros, reusada no T07. |
| 3 | **T07** Fila de validação | Herda a tabela do T01. |
| 4 | **T08** Detalhe da solicitação | Herda cards do T03 e o checklist. |
| 5 | T04, T02 | Formulários — herdam a linguagem de campos. |
| 6 | M05, M06 | Modais — os menores, herdam tudo. |
| 7 | T03-b, T08-b | Variantes de estado, geradas a partir das aprovadas. |

Total: **10 telas** (6 principais + 2 modais + 2 variantes de estado).
