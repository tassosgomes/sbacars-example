# Briefs para o Stitch — Catálogo Público D01

> **Como usar:** cada bloco `▸ PROMPT` é para colar direto no Stitch. Cole sempre o
> **Preâmbulo (§2)** antes do prompt da tela — ele carrega o sistema visual e as regras
> que valem para todas.
>
> **Fonte:** `ux-spec.md` (estrutura, campos, estados) + `api-contract.yaml` (dados) +
> `DESIGN.md` (sistema visual). Os briefs não inventam conteúdo novo; personalize o visual
> à vontade, mas o que está em **negrito** nos prompts é contrato com o backend.
>
> **Destino do HTML:** salve em `.stitch/designs/<codigo>.html`.

---

## 1. Como o DESIGN.md se aplica aqui

**D01 é o destinatário original do `DESIGN.md`.** Ele foi escrito para isto: fotografia de
veículos, *vehicle listing cards*, hierarquia de preço, CTA laranja de conversão, sidebar
de captação. Onde o backoffice de D02 precisou descartar metade do sistema, o catálogo o
usa quase inteiro.

| Do DESIGN.md | No catálogo público |
|---|---|
| Paleta completa (Deep Navy / Trust Green / Action Orange / neutros) | ✅ Herdada integralmente |
| Fotografia de veículo como pilar da interface | ✅ **É o pilar** — mas metade das telas precisa funcionar sem ela (§5) |
| *Vehicle cards* com imagem, hierarquia de preço, specs limpas | ✅ Herdados |
| CTA laranja para conversão | ✅ **Aqui existe conversão de verdade**: `Tenho interesse` |
| Coluna lateral de 4 colunas para captação | ✅ Vira a coluna de decisão da página do veículo |
| Grid de 12 colunas, 1280px, gutter 24px, `stack-lg` entre seções | ✅ Herdado |
| Elevação por outline de 1px `#CDCDDB`, sem sombra | ✅ Herdada |
| Zebra + bordas horizontais em tabelas de spec | ✅ Herdado na comparação (T06) |
| Glassmorphism para overlay de navegação mobile | ✅ Herdado no sheet de filtros e no M02 mobile |
| Badge "Verified", selos de certificação, "Good Deal" | ❌ **Proibidos** — ver DA-02 |
| Corpo em 14px (adaptação do backoffice) | ⚠️ Aqui o corpo é 16px — leitura de consumidor, não varredura de operador |

### Três decisões de adaptação que precisam do seu aval

**DA-01 — Escassez do Action Orange.** O `DESIGN.md` reserva `#FC8422` "exclusivamente
para pontos primários de conversão". No catálogo isso é literal e tem um único dono:
**`Tenho interesse`**. Uma ocorrência por tela, e apenas nas telas onde a ação existe.
`Favoritar` e `Comparar` são secundárias — elas atrasam a conversão, não a produzem. Se o
laranja aparecer em dois botões da mesma tela, o brief está errado.

**DA-02 — Trust Green não pode falar sobre o veículo.** O `DESIGN.md` diz que o verde
"signifies Verified status, certifications and Good Deal indicators". **Isso está proibido
neste PRD**: a RN-12 veda pressupor certificação formal e o RF-03 veda qualquer linguagem
de vistoria aprovada. Um ✓ verde ao lado de "Origem" seria exatamente a afirmação que o
produto se recusa a fazer.

Reinterpretei o verde como **confirmação de ação do comprador** — interesse encaminhado,
favorito salvo — e nunca como atributo do carro. Na prática ele quase some da interface, e
isso é intencional: a transparência deste produto se comunica por *texto verificável com
data e fonte*, não por selo. **Me avise se preferir manter o verde como sinal de
qualidade** — mas ele contradiz dois requisitos do PRD.

**DA-03 — Badges tonais vs. botão sólido.** Mesma regra já adotada em D02, mantida para
que os dois apps pareçam o mesmo sistema: badges usam os pares *container* (fundo pálido +
texto escuro), botões usam a cor cheia. Um badge nunca é sólido saturado; um botão
primário nunca é pálido.

### Duas regras que o Stitch tende a violar sozinho

**`Não informado` não pode ser apagado.** É a tentação natural: cinza fraquinho, 11px,
quase invisível. Aqui ele é **informação**, não ausência dela — o RF-03 existe para isso.
Cor `#78767c` em itálico, mesmo tamanho do valor ao lado, contraste AA. Nunca `—`, nunca
célula vazia, nunca linha suprimida.

**Metade das telas mostra veículo sem foto.** O `DESIGN.md` monta a interface em torno da
fotografia, mas o conteúdo comercial pertence ao PRD-B e pode não existir no lançamento
(RF-01). Onde o brief pede placeholder, ele precisa ficar **neutro e resolvido** — não
"imagem indisponível", não ícone de erro, não caixa quebrada.

---

## 2. Preâmbulo — colar antes de todo prompt

```
CONTEXTO
Catálogo público de carros seminovos e usados da "AutoTransparência", plataforma
brasileira. Usuário: comprador final, pessoa comum pesquisando um carro, sem cadastro
e sem login. Desktop 1440px. O tom da marca é "transparência, expertise profissional e
qualidade curada": educativo, calmo e preciso, nunca promocional ou agressivo. É um
produto de consumidor premium — generoso em respiro e em fotografia — mas jamais usa
linguagem de certificação, garantia formal, vistoria aprovada, "verificado" ou
"selo de qualidade". A plataforma publica o que apurou e declara o que não conseguiu
apurar.

IDIOMA
Todos os rótulos, textos e dados de exemplo em português do Brasil.
Moeda em BRL (R$ 87.900,00). Datas em DD/MM/AAAA. Distâncias em km.

PALETA (usar exatamente estes valores)
- Deep Navy (estrutura, cabeçalho, texto principal): #2E2E3A
  variantes: #191925 (mais escuro), #464652 (navy secundário)
- Action Orange (ação primária de conversão, UMA por tela): #FC8422, texto branco
- Trust Green (APENAS confirmação de ação do comprador): #018444
- Erro / indisponível: #ba1a1a
- Fundo da página: #f9f9ff (off-white, nunca branco puro)
- Cards e superfícies elevadas: #ffffff
- Superfície sutil / zebra: #f3f3fb
- Borda: #CDCDDB
- Texto principal: #2E2E3A · Texto secundário: #4A4A4A · Texto sutil: #78767c

PARES TONAIS PARA BADGES (fundo pálido + texto escuro, NUNCA sólido saturado)
- Atenção (Reservado, Limitação declarada):   fundo #ffdbc7, texto #733600
- Neutro (Vendido, Indisponível):             fundo #e2e2ea, texto #47464c
- Positivo (confirmação de ação do usuário):  fundo #8ef9ab, texto #00743b

TIPOGRAFIA — Inter em tudo
- Nome do veículo na página de detalhe: 48px / peso 700 / entrelinha 56px /
                    letter-spacing -0.02em
- Título de página: 32px / peso 700 / entrelinha 40px / letter-spacing -0.01em
- Título de card:   18px / peso 600
- Corpo:            16px / peso 400 / entrelinha 24px
- Corpo secundário: 14px / peso 400 / cor #4A4A4A
- Dados técnicos e preços: algarismos tabulares (tabular figures) — OBRIGATÓRIO em
                    preços, quilometragem, anos, distâncias e datas
- Rótulos de campo e cabeçalho de tabela: 12px / peso 700 / letter-spacing 0.05em /
                    CAIXA ALTA. Ex.: "QUILOMETRAGEM", "CÂMBIO", "PREÇO"
- Texto auxiliar:   14px / peso 400 / cor #78767c

FORMA E ELEVAÇÃO
- Botões e inputs: raio 4px. Cards e modais: raio 8px.
- Cards NÃO têm sombra. Usam borda de 1px #CDCDDB sobre o fundo #f9f9ff.
  A profundidade vem do contraste entre o card branco e o fundo off-white.
- Apenas modais e sheets têm sombra: ambiente, larga, suave, baixa opacidade.
- Fotos de veículo preenchem a largura do card, proporção 4:3, raio 8px no topo.

BOTÕES
- Primário (UMA ocorrência por tela, só "Tenho interesse"): fundo #FC8422,
  texto branco, raio 4px, altura 48px
- Secundário: fundo transparente, borda 1px #2E2E3A, texto #2E2E3A
- Fantasma: sem borda nem fundo, texto #4A4A4A
- Botão de ícone (favoritar, comparar): circular 40px, fundo branco,
  borda 1px #CDCDDB

ESPAÇAMENTO
Base de 8px em tudo. Padding do conteúdo: 40px. Espaço entre cards: 24px.
Espaço entre seções lógicas: 32px. Largura máxima do conteúdo: 1280px.

LAYOUT BASE (presente em todas as telas)
SEM SIDEBAR — este é o site público, não o backoffice.
Cabeçalho de 72px, fundo #ffffff, borda inferior #CDCDDB, conteúdo centralizado em
1280px:
  à esquerda "AutoTransparência" em #2E2E3A, 20px semibold;
  à direita, dois links em 16px #4A4A4A com ícone: "Favoritos" com um badge circular
  #2E2E3A de texto branco mostrando "3", e "Comparar" com badge mostrando "2".
Área de conteúdo com fundo #f9f9ff e padding de 40px.
Rodapé simples, fundo #f3f3fb, borda superior #CDCDDB, 32px de padding:
  "AutoTransparência — catálogo curado de seminovos e usados" em 14px #78767c.

"NÃO INFORMADO" — REGRA ABSOLUTA
Quando um dado não existe, escreva o texto "Não informado" em itálico, cor #78767c,
NO MESMO TAMANHO do valor que ocuparia aquele lugar. Nunca use travessão, nunca
deixe a célula vazia, nunca omita a linha. É informação, não ausência dela.

NÃO FAZER
- Não usar as palavras "verificado", "certificado", "garantido", "vistoriado",
  "aprovado", "selo", "laudo aprovado" nem qualquer variação.
- Não colocar ícone de check verde, escudo, medalha ou selo ao lado de nenhum dado
  do veículo.
- Não inventar campos, filtros, badges ou botões além dos listados.
- Não usar azul: não existe azul neste sistema. Ação é laranja, estrutura é navy.
- Não colocar sombra em cards. Não colocar bordas verticais em tabelas.
- Não usar linguagem de urgência ("últimas unidades", "aproveite", "imperdível").
- Sem modo escuro.
```

---

## 3. T01 — Resultado do catálogo

**Arquivo** `t01-resultado-catalogo.html` · **Rota** `/` · A tela de maior tráfego

▸ **PROMPT**

```
Tela principal do catálogo — resultado de busca de veículos.

FAIXA DE LOCALIZAÇÃO (logo abaixo do cabeçalho, largura total do conteúdo, fundo
#f3f3fb, borda #CDCDDB, raio 8px, padding 12px 16px, ícone de alfinete em #4A4A4A)
À esquerda, 14px #4A4A4A: "Mostrando distâncias a partir de Campinas/SP".
À direita, link fantasma sublinhado "Alterar".

CABEÇALHO DA PÁGINA
Título "Seminovos e usados" (32px bold #2E2E3A).
Abaixo, 16px #4A4A4A: "142 veículos disponíveis".

LINHA DE CONTROLE (abaixo do título, alinhada em uma linha)
À esquerda: campo de busca com ícone de lupa, largura 360px, altura 48px,
  placeholder "Buscar por marca, modelo ou versão".
À direita: select "Ordenar por" com o valor "Mais próximos" selecionado
  (opções: Mais próximos · Menor preço · Maior preço · Menor quilometragem ·
   Mais novos · Publicados recentemente).

CHIPS DE FILTRO ATIVO (linha abaixo, quando houver)
Três chips com fundo #ffffff, borda #CDCDDB, raio 999px, altura 32px, 14px #2E2E3A,
cada um com um "×" à direita: "Honda", "Até R$ 95.000", "Automático".
Ao lado, botão fantasma "Limpar filtros" em 14px #78767c.

LAYOUT DE DUAS COLUNAS, 24px entre elas:

COLUNA DE FILTROS (280px, à esquerda, card branco, borda #CDCDDB, raio 8px,
padding 24px, seções separadas por linha #CDCDDB com 24px de respiro)
Título "Filtros" em 18px/600. Rótulos de seção em CAIXA ALTA 12px/700 #78767c.
  - MARCA — lista de checkboxes com contagem à direita em #78767c:
    Honda (12) · Toyota (9) · Volkswagen (14) · Jeep (7) · Hyundai (11) ·
    Chevrolet (18) · Fiat (15) · Renault (6)
  - MODELO — select desabilitado com placeholder "Selecione uma marca primeiro"
  - ANO — dois inputs pequenos lado a lado: "De 2016" e "Até 2024"
  - PREÇO — dois inputs com prefixo "R$": "39.900" e "215.000"
  - QUILOMETRAGEM ATÉ — input com sufixo "km", valor "80.000"
  - COMBUSTÍVEL — checkboxes: Flex (98) · Gasolina (24) · Diesel (12) ·
    Híbrido (8)
  - CÂMBIO — checkboxes: Automático (76) · Manual (66)
  - LOCALIZAÇÃO — select "UF" e select "Cidade"
  - DISTÂNCIA ATÉ — slider com o valor "150 km"
NÃO INCLUIR filtro de carroceria, tipo de carroceria, sedã/SUV/hatch ou
categoria — este filtro não existe nesta versão.

COLUNA DE RESULTADOS (restante da largura)
Grade de 3 colunas de cards de veículo, 24px de gutter. Mostre 6 cards.

CARD DE VEÍCULO (branco, borda 1px #CDCDDB, raio 8px, SEM sombra)
  - Foto do carro no topo, largura total do card, proporção 4:3, cantos superiores
    arredondados. Fotografia realista de carro brasileiro, ambiente neutro.
  - Sobre a foto, canto superior direito: dois botões circulares de 40px, fundo
    branco com 90% de opacidade: um coração de contorno e um ícone de comparação
    (duas setas horizontais opostas). No SEGUNDO card, o coração está PREENCHIDO
    em #2E2E3A.
  - Sobre a foto, canto superior esquerdo, apenas no TERCEIRO card: badge tonal
    "Reservado" (#ffdbc7 / #733600).
  - Corpo do card, padding 20px:
    · Título em 18px/600 #2E2E3A, uma linha, ex.: "Honda Civic EXL 2.0"
    · Linha técnica em 14px #78767c tabular:
      "2021/2022 · 48.300 km · Automático · Flex"
    · Preço em 24px/700 #2E2E3A tabular, ex.: "R$ 87.900,00"
    · Abaixo do preço, 12px #78767c: "Preço atualizado em 12/08/2026"
    · Linha final com ícone de alfinete, 14px #4A4A4A:
      "Campinas/SP · ~ 42 km" — a distância em peso 500

Use estes seis veículos brasileiros reais, com preços plausíveis e distâncias
variadas: Honda Civic EXL 2.0 · Toyota Corolla XEI 2.0 · Jeep Compass Longitude ·
Volkswagen T-Cross Highline · Hyundai HB20 Comfort Plus · Chevrolet Onix LTZ.
Um deles (o quinto) NÃO TEM FOTO: no lugar da fotografia, um bloco de proporção 4:3
com fundo #f3f3fb, uma silhueta de carro em contorno fino #CDCDDB centralizada,
e nada mais — sem texto, sem ícone de erro, sem "imagem indisponível".

RODAPÉ DA LISTA
"Mostrando 1–6 de 142" à esquerda em 14px #78767c; paginação numérica à direita,
página ativa com fundo #2E2E3A e texto branco.
```

---

## 4. T01-b — Variante: sem resultados

**Arquivo** `t01b-sem-resultados.html`

▸ **PROMPT**

```
Mesma tela do resultado do catálogo, com a coluna de filtros idêntica, mas a coluna
de resultados mostra o estado SEM NENHUM VEÍCULO.

O contador do cabeçalho vira "Nenhum veículo com esses critérios" em 16px #4A4A4A.
Os chips de filtro ativo continuam visíveis, agora com quatro:
"Honda", "Até R$ 85.000", "Automático", "Até 100 km".

No lugar da grade, um card branco centralizado (borda #CDCDDB, raio 8px, padding
40px, largura máxima 640px):
  Ícone de lupa em contorno fino #CDCDDB, 48px, centralizado.
  Título 24px/600 #2E2E3A: "Nenhum veículo com todos esses critérios"
  Texto 16px #4A4A4A: "Estas mudanças trariam resultados:"

  Três SUGESTÕES empilhadas, cada uma como uma linha clicável com fundo #f3f3fb,
  borda #CDCDDB, raio 4px, padding 16px, 12px entre elas. Cada linha tem:
    à esquerda, 16px #2E2E3A: o texto da sugestão
    à direita, 16px/600 #2E2E3A tabular: a contagem, seguida de "veículos" em
    14px #78767c
  As três sugestões, nesta ordem:
    "Ampliar o preço até R$ 95.000"              → 12 veículos
    "Remover o filtro de câmbio automático"       → 8 veículos
    "Ampliar a distância para 200 km"             → 31 veículos

  Abaixo, centralizado, botão secundário (borda navy) "Limpar todos os filtros".

NÃO use ilustração grande, emoji, rosto triste ou linguagem de desculpa
("Ops!", "Que pena"). O tom é o de quem oferece o próximo passo, não o de quem
lamenta.
```

---

## 5. T01-c — Variante: sem localização de referência

**Arquivo** `t01c-sem-localizacao.html`

▸ **PROMPT**

```
Mesma tela do resultado do catálogo, no estado em que o comprador AINDA NÃO definiu
uma localização de referência. Três diferenças, e nenhuma delas pode parecer erro
ou estado degradado:

1. A FAIXA DE LOCALIZAÇÃO muda de conteúdo e ganha peso. Continua em #f3f3fb com
   borda #CDCDDB e raio 8px, mas agora tem padding 20px 24px e contém:
     à esquerda, ícone de alfinete e o texto em 16px #2E2E3A:
     "Veja primeiro os veículos mais próximos de você"
     à direita, DOIS botões secundários lado a lado, de mesmo tamanho e mesmo peso
     visual (borda 1px #2E2E3A, texto #2E2E3A, altura 40px):
       "Usar minha localização"  e  "Escolher cidade"
   Os dois botões têm exatamente a mesma aparência. Nenhum é primário, nenhum é
   fantasma.

2. Nos CARDS DE VEÍCULO, a linha final perde a distância e mostra só a cidade:
   "Campinas/SP". A distância simplesmente NÃO APARECE — não vire travessão, não
   escreva "Não informado", não deixe espaço reservado.

3. O select de ordenação mostra "Publicados recentemente" como valor selecionado,
   apresentado como uma escolha normal. A opção "Mais próximos" continua na lista.

Nenhum banner de aviso, nenhum ícone de alerta, nenhuma cor de erro. Este é um modo
pleno de uso do catálogo.

O filtro "DISTÂNCIA ATÉ" da coluna lateral aparece desabilitado, em cinza, com um
texto auxiliar de 12px #78767c abaixo dele: "Defina sua localização para filtrar
por distância."
```

---

## 6. T01-m — Variante mobile: resultado do catálogo

**Arquivo** `t01m-resultado-mobile.html` · **Viewport 390px**

▸ **PROMPT**

```
Versão MOBILE (390px de largura) da tela de resultado do catálogo.

CABEÇALHO fixo de 56px, fundo branco, borda inferior #CDCDDB:
"AutoTransparência" em 18px semibold #2E2E3A à esquerda; à direita, ícone de
coração com badge "3" e ícone de comparação com badge "2".

FAIXA DE LOCALIZAÇÃO em uma linha, fundo #f3f3fb, padding 12px 16px, 14px:
ícone de alfinete + "Campinas/SP" + link "Alterar" à direita.

BARRA DE BUSCA E FILTRO, fixa logo abaixo, fundo branco, padding 12px 16px:
campo de busca ocupando o espaço disponível, altura 44px, e ao lado um botão
quadrado de 44px com ícone de controles deslizantes e um badge circular #FC8422
com "3" no canto superior direito.

CONTAGEM E ORDENAÇÃO: linha com "142 veículos" em 14px #4A4A4A à esquerda e um
botão fantasma "Mais próximos ▾" à direita.

CARDS EM COLUNA ÚNICA, largura total menos 16px de margem lateral, 16px entre eles.
Estrutura idêntica à do desktop: foto 4:3 no topo, botões de coração e comparação
sobrepostos no canto superior direito, corpo com título, linha técnica, preço,
data de atualização e "Campinas/SP · ~ 42 km".
Mostre 3 cards: Honda Civic EXL 2.0 · Toyota Corolla XEI 2.0 (com badge tonal
"Reservado" sobre a foto) · Volkswagen T-Cross Highline.

BARRA DE COMPARAÇÃO FIXA no rodapé da tela, altura 64px, fundo #2E2E3A,
texto branco, com sombra ambiente para cima:
  à esquerda, 14px: "2 de 4 selecionados"
  à direita, botão de fundo branco, texto #2E2E3A, raio 4px, altura 40px:
  "Comparar"

Os alvos de toque têm no mínimo 44×44px. O coração e o botão de comparação não
podem cobrir o preço nem o título.
```

---

## 7. M02 — Localização de referência *(sheet sobre T01)*

**Arquivo** `m02-localizacao.html`

▸ **PROMPT**

```
Modal centrado sobre a tela de resultado do catálogo, com o fundo escurecido em navy
#191925 a 50% de opacidade. Largura do modal 480px, card branco, raio 8px, sombra
ambiente ampla e suave.

CABEÇALHO DO MODAL
Ícone de alfinete em #2E2E3A, 32px, centralizado.
Título "Ver os veículos mais próximos de você" em 24px/600 #2E2E3A, centralizado.
Botão × fantasma no canto superior direito.

CORPO, centralizado, com 24px de respiro
Texto 16px #4A4A4A, no máximo duas linhas:
"Usamos sua localização apenas para calcular a distância até a cidade de cada
veículo. Ela não é armazenada."

DUAS AÇÕES EMPILHADAS, ambas de largura total e altura 48px, com 12px entre elas.
As duas têm o MESMO PESO VISUAL — nenhuma é primária, nenhuma é laranja:
  1. Botão secundário (borda 1px #2E2E3A, texto #2E2E3A) com ícone de mira:
     "Usar minha localização"
  2. Botão secundário idêntico, com ícone de lupa:
     "Escolher uma cidade"

Abaixo delas, separado por 16px, um link fantasma centralizado em 14px #78767c:
"Agora não"

BLOCO SECUNDÁRIO, ao lado ou abaixo, mostrando o ESTADO DE ESCOLHA MANUAL já aberto
(gere-o como um segundo modal na mesma página, para comparação):
  Mesmo cabeçalho, mas o corpo tem:
    Rótulo CAIXA ALTA 12px/700 #78767c: "SUA CIDADE"
    Campo de busca com ícone de lupa, altura 48px, preenchido com "camp"
    Lista de resultados logo abaixo, cada linha com 48px de altura, separadas por
    linha #CDCDDB, hover #f3f3fb:
      "Campinas" com "SP" em 14px #78767c à direita
      "Campo Grande" com "MS"
      "Campo Bom" com "RS"
      "Campos dos Goytacazes" com "RJ"
    Rodapé com botão secundário "Cancelar" e botão primário LARANJA "Confirmar
    cidade" — este é o único laranja do modal.

NÃO use ícone de cadeado, escudo ou qualquer sinal de "segurança". A frase sobre
não armazenar já diz o que precisa ser dito; um cadeado transformaria uma
informação em alarme.
```

---

## 8. T03 — Detalhe do veículo

**Arquivo** `t03-detalhe-veiculo.html` · **Rota** `/veiculos/:itemId` · **A tela mais importante**

▸ **PROMPT**

```
Página de detalhe de um veículo do catálogo.
Duas colunas: conteúdo 65% à esquerda, coluna de decisão 35% à direita, 32px entre
elas. A coluna de decisão fica FIXA ao rolar (sticky).
Todos os cards: fundo branco, borda 1px #CDCDDB, raio 8px, SEM sombra, padding 24px.

CABEÇALHO
Breadcrumb "Catálogo / Honda Civic EXL 2.0" em 14px #78767c.
Nome do veículo "Honda Civic EXL 2.0" em 48px/700 #2E2E3A, letter-spacing -0.02em.
Abaixo, 16px #4A4A4A com algarismos tabulares:
"2021/2022 · 48.300 km · Automático · Flex · Campinas/SP · ~ 42 km"

COLUNA DE CONTEÚDO (esquerda)

GALERIA — foto principal grande, proporção 16:9, raio 8px, fotografia realista de
um Honda Civic prata. Abaixo dela, uma fila de 4 miniaturas de 96×72px com raio 4px;
a primeira com borda de 2px #2E2E3A indicando seleção.

Card "O que a operação apurou" — título 18px/600. Este card vem ANTES de qualquer
texto de venda.
Três blocos empilhados, separados por linha #CDCDDB com 20px de respiro. Cada bloco:
rótulo em CAIXA ALTA 12px/700 #78767c, conteúdo em 16px #2E2E3A, procedência em
14px #78767c.
SEM ícones de check, sem selos, sem cor verde.

  ORIGEM
    "Veículo de frota corporativa, único proprietário pessoa jurídica."
    "Fonte: Contrato de cessão Localiza, 02/2026 · Atualizado em 10/08/2026"

  CONDIÇÃO
    "Revisões em concessionária até 40.000 km. Pneus dianteiros trocados em
    03/2026."
    "Fonte: Histórico de manutenção Honda · Atualizado em 10/08/2026"

  HISTÓRICO
    Badge tonal "Limitação declarada" (#ffdbc7 / #733600), 12px, no lugar onde
    estaria o rótulo de fonte.
    Texto em 16px #4A4A4A, em itálico:
    "Não foi possível obter o histórico de sinistros deste veículo junto às bases
    consultadas."
    "Declarado em 10/08/2026"

Card "Ficha técnica" — grade de leitura de 3 colunas, 24px de gutter, 20px entre
linhas. Cada célula: rótulo em CAIXA ALTA 12px/700 #78767c acima do valor em
16px/500 #2E2E3A tabular.
  ANO: 2021/2022 · QUILOMETRAGEM: 48.300 km · CÂMBIO: Automático
  COMBUSTÍVEL: Flex · COR: Não informado · PORTAS: Não informado
  MOTOR: Não informado · FINAL DE PLACA: Não informado
Os quatro "Não informado" em itálico #78767c, MESMO TAMANHO dos outros valores
(16px). Nenhuma linha é omitida por não ter valor.

Card "Sobre este veículo"
  Parágrafo de venda em 16px #4A4A4A, três a quatro linhas, tom informativo e sóbrio.
  Abaixo, três destaques em linha, cada um como chip de fundo #f3f3fb, borda
  #CDCDDB, raio 4px, 14px #2E2E3A: "Único proprietário" · "Revisões em
  concessionária" · "IPVA 2026 pago".

COLUNA DE DECISÃO (direita, fixa)

Card principal, o mais destacado da tela:
  Preço "R$ 87.900,00" em 40px/700 #2E2E3A, tabular.
  Abaixo, 14px #78767c: "Preço atualizado em 12/08/2026".
  Linha com ícone de alfinete, 16px #4A4A4A: "Campinas/SP · ~ 42 km de você".
  Espaço de 24px.
  Botão primário LARANJA de largura total, altura 48px, 18px/600:
  "Tenho interesse"
  Abaixo dele, 14px #78767c centralizado:
  "A operação entra em contato sobre este veículo."
  Espaço de 16px.
  Dois botões secundários lado a lado, largura igual, altura 44px, com ícone:
  "Favoritar" (coração de contorno) e "Comparar" (duas setas opostas).

Card de nota de transparência (fundo #f3f3fb, borda esquerda de 3px em #2E2E3A,
raio 4px, ícone ⓘ, texto 14px #4A4A4A):
"Não realizamos certificação nem vistoria formal. Publicamos o que a operação
apurou e declaramos o que não foi possível apurar."

Esta é a ÚNICA ocorrência de laranja na tela.
```

---

## 9. T03-b — Variante: reservado e sem conteúdo comercial

**Arquivo** `t03b-reservado-sem-conteudo.html`

▸ **PROMPT**

```
Mesma página de detalhe do veículo, com quatro mudanças. Esta variante prova que a
página funciona quando o conteúdo comercial ainda não existe — ela NÃO pode parecer
quebrada nem incompleta.

1. GALERIA: no lugar da foto grande e das miniaturas, um único bloco de proporção
   16:9, fundo #f3f3fb, borda 1px #CDCDDB, raio 8px, com uma silhueta de carro em
   contorno fino #CDCDDB centralizada, ocupando cerca de 30% da largura do bloco.
   Nada mais dentro: sem texto, sem "foto em breve", sem ícone de câmera quebrada,
   sem botão.

2. O card "Sobre este veículo" NÃO EXISTE. Não é um card vazio, não é um card com
   "conteúdo em breve" — ele simplesmente não está na página. A coluna de conteúdo
   termina no card "Ficha técnica".

3. O título do veículo passa a ser composto pelos dados técnicos:
   "Volkswagen T-Cross Highline 1.4 TSI" — sem frase de marketing.

4. STATUS RESERVADO, em dois lugares:
   - Ao lado do nome do veículo no cabeçalho, um badge tonal "Reservado"
     (#ffdbc7 / #733600), 14px, alinhado ao centro da linha do título.
   - No card de decisão, LOGO ACIMA do preço, uma faixa de fundo #ffdbc7, borda
     esquerda de 3px em #FC8422, raio 4px, padding 12px, com ícone de relógio e o
     texto em 14px #733600:
     "Há alguém à frente neste veículo. Você ainda pode demonstrar interesse."

O botão laranja "Tenho interesse" CONTINUA presente, ativo e com o mesmo destaque.
Os botões "Favoritar" e "Comparar" continuam ativos. Nada é removido nem
desabilitado por causa da reserva.

Os cards "O que a operação apurou" e "Ficha técnica" ficam idênticos aos da tela
anterior em estrutura — eles vêm da operação e não dependem do conteúdo comercial.
```

---

## 10. T03-c — Variante: veículo indisponível

**Arquivo** `t03c-indisponivel.html`

▸ **PROMPT**

```
Página de um veículo que não está mais no catálogo. É uma página CURTA — coluna
única centrada, largura máxima 720px, sem coluna lateral.

CABEÇALHO
Breadcrumb "Catálogo / Toyota Corolla XEI 2.0" em 14px #78767c.

CARD ÚNICO (branco, borda #CDCDDB, raio 8px, padding 40px, centralizado)
  Badge tonal "Vendido" (#e2e2ea / #47464c), 14px, no topo.
  Título "Este veículo foi vendido." em 32px/700 #2E2E3A.
  Abaixo, 16px #4A4A4A: "Toyota Corolla XEI 2.0 · 2020/2021".
  Nada além disso sobre o carro: SEM foto, SEM preço, SEM quilometragem, SEM ficha
  técnica, SEM os blocos de fatos, SEM botão de interesse, SEM favoritar,
  SEM comparar.

  Separado por 32px e uma linha #CDCDDB:
  Texto 16px #4A4A4A: "Temos outros veículos parecidos no catálogo."
  Botão secundário (borda navy), altura 48px: "Ver Toyota Corolla disponíveis"

GERE TAMBÉM, na mesma página e abaixo desta, a SEGUNDA VARIANTE do mesmo card, para
o caso de veículo retirado da oferta:
  Badge tonal "Indisponível" (#e2e2ea / #47464c).
  Título "Este veículo não está mais disponível."
  Subtítulo "Jeep Compass Longitude · 2022/2023".
  Mesmo botão de saída, adaptado: "Ver Jeep Compass disponíveis".

Sem cor de erro, sem ícone de alerta, sem ilustração. A página informa um fato e
oferece um caminho.
```

---

## 11. T04 — Início de interesse

**Arquivo** `t04-interesse.html` · **Rota** `/veiculos/:itemId/interesse`

> **Atenção na implementação:** o card de contexto é de D01; o **formulário de contato é
> de D03**. Os campos, a validação, o texto de consentimento e a retenção pertencem ao
> contrato de D03 e não a este PRD (PD-001). O brief os desenha porque o comprador vê uma
> página só — mas nenhum desses campos pode transitar pelo catalog-service. Os valores
> abaixo são ilustrativos e serão substituídos pelo contrato de D03 quando ele existir.

▸ **PROMPT**

```
Página de manifestação de interesse. Coluna única centrada, largura máxima 720px.

CABEÇALHO
Breadcrumb "Catálogo / Honda Civic EXL 2.0 / Interesse" em 14px #78767c.
Título "Demonstrar interesse" em 32px/700 #2E2E3A.
Abaixo, 16px #4A4A4A: "A operação vai retomar o contato sobre este veículo."

CARD DE CONTEXTO (branco, borda #CDCDDB, raio 8px, padding 20px) — é o veículo
sobre o qual o interesse foi manifestado, em formato horizontal compacto:
  À esquerda, foto do carro de 160×120px, raio 4px.
  Ao centro:
    Título "Honda Civic EXL 2.0" em 18px/600.
    Linha técnica 14px #78767c tabular: "2021/2022 · 48.300 km · Campinas/SP".
  À direita, alinhado ao topo:
    Preço "R$ 87.900,00" em 24px/700 tabular.
    Abaixo, 12px #78767c: "Preço atualizado em 12/08/2026".

CARD DE FORMULÁRIO (branco, borda #CDCDDB, raio 8px, padding 32px)
  Título "Como podemos falar com você" em 18px/600.
  Rótulos em CAIXA ALTA 12px/700 #78767c. Inputs com borda 1px #CDCDDB, raio 4px,
  altura 48px, fundo branco.
    - NOME
    - TELEFONE (com máscara "(19) 99999-9999")
    - E-MAIL
    - MENSAGEM (opcional) — textarea de 3 linhas, placeholder
      "Algo que a operação deva saber antes do contato"
  Abaixo dos campos, uma linha com checkbox e texto 14px #4A4A4A:
    "Autorizo o contato da AutoTransparência sobre este veículo."
  Botão primário LARANJA de largura total, altura 48px:
  "Enviar interesse"

GERE TAMBÉM, abaixo, o ESTADO DE CONFIRMAÇÃO da mesma página: o card de formulário
é substituído por um card branco de padding 40px, centralizado, contendo:
  Ícone de check em círculo, contorno de 2px em #018444, 48px.
  Título "Interesse enviado" em 24px/600 #2E2E3A.
  Texto 16px #4A4A4A: "A operação vai retomar o contato sobre o Honda Civic EXL 2.0.
  O veículo continua no seu histórico de interesse."
  Botão secundário "Voltar ao catálogo".
Este check verde é uma das pouquíssimas ocorrências de #018444 no produto: ele
confirma uma ação do comprador, nunca qualifica o veículo.

O card de contexto permanece visível nos dois estados.
```

---

## 12. T05 — Favoritos

**Arquivo** `t05-favoritos.html` · **Rota** `/favoritos` · **Fase 2**

▸ **PROMPT**

```
Tela de veículos favoritados. No cabeçalho, o link "Favoritos" está em destaque
(#2E2E3A, peso 600, com sublinha de 2px em #FC8422).

CABEÇALHO DA PÁGINA
Título "Seus favoritos" em 32px/700.
Abaixo, 16px #4A4A4A: "4 veículos".

AVISO DE ESCOPO — faixa permanente logo abaixo do título, fundo #f3f3fb, borda
esquerda de 3px em #2E2E3A, raio 4px, padding 16px, ícone ⓘ em #4A4A4A,
texto 14px #4A4A4A:
"Seus favoritos ficam guardados neste navegador. Limpar os dados do navegador ou
trocar de aparelho faz com que se percam."

GRADE de 3 colunas de cards, 24px de gutter, com QUATRO cards em estados diferentes:

  CARD 1 e CARD 2 — normais, idênticos aos da tela de catálogo: foto, coração
  PREENCHIDO em #2E2E3A no canto superior direito da foto, título, linha técnica,
  preço, data de atualização, cidade e distância.
  Veículos: Honda Civic EXL 2.0 e Volkswagen T-Cross Highline.

  CARD 3 — VEÍCULO VENDIDO. Card apagado:
    Sem foto: no lugar dela, um bloco 4:3 de fundo #e2e2ea, sem silhueta e sem
    conteúdo.
    Badge tonal "Vendido" (#e2e2ea / #47464c) no corpo do card, acima do título.
    Título "Toyota Corolla XEI 2.0" em 18px/600, cor #78767c em vez de #2E2E3A.
    SEM preço, SEM linha técnica, SEM distância.
    Texto 14px #78767c: "Este veículo foi vendido."
    Única ação: botão fantasma "Remover" em 14px #78767c.
    O card inteiro NÃO é clicável e não leva a lugar nenhum.

  CARD 4 — VEÍCULO INDISPONÍVEL. Igual ao card 3, com:
    Badge tonal "Indisponível" (#e2e2ea / #47464c).
    Título "Jeep Compass Longitude".
    Texto: "Este veículo não está mais disponível."

ABAIXO DA GRADE, alinhado à esquerda, botão fantasma 14px #78767c:
"Remover os 2 veículos indisponíveis"

Não use laranja nesta tela: nenhuma ação aqui é conversão.
```

---

## 13. T06 — Comparação

**Arquivo** `t06-comparacao.html` · **Rota** `/comparar` · **Fase 2**

▸ **PROMPT**

```
Tela de comparação lado a lado de veículos. No cabeçalho, o link "Comparar" está em
destaque (#2E2E3A, peso 600, com sublinha de 2px em #FC8422).

CABEÇALHO DA PÁGINA
Título "Comparar veículos" em 32px/700.
Abaixo, 16px #4A4A4A: "3 veículos selecionados".

AVISO DE ALTERAÇÃO — faixa logo abaixo, fundo #ffdbc7, borda esquerda de 3px em
#FC8422, raio 4px, padding 16px, ícone de atenção, texto 14px #733600:
"O Chevrolet Onix LTZ foi vendido e saiu da comparação."

TABELA DE COMPARAÇÃO em um card branco, borda #CDCDDB, raio 8px, sem sombra.
Bordas HORIZONTAIS apenas, zebra #f3f3fb nas linhas alternadas, SEM bordas
verticais.

CABEÇALHO DA TABELA — primeira coluna vazia (200px de largura), depois três colunas
iguais, uma por veículo. Cada uma, centralizada, com:
  Foto de 160×120px, raio 4px. No TERCEIRO veículo, no lugar da foto, um bloco de
  160×120px com fundo #f3f3fb, borda #CDCDDB e silhueta de carro em contorno
  #CDCDDB.
  Título em 16px/600 #2E2E3A, duas linhas no máximo.
  Preço em 24px/700 tabular #2E2E3A.
  Botão fantasma "Remover" em 14px #78767c.
Este cabeçalho fica FIXO ao rolar a página.

LINHAS, agrupadas. O rótulo do grupo aparece como uma linha de fundo #f3f3fb com o
texto em CAIXA ALTA 12px/700 #78767c ocupando a largura toda. Valores em 16px/500
tabular, centralizados nas colunas de veículo; rótulo da linha em 14px #4A4A4A na
primeira coluna.

  IDENTIFICAÇÃO
    Ano              | 2021/2022        | 2020/2021        | 2022/2023
    Versão           | EXL 2.0          | XEI 2.0          | Não informado
  USO
    Quilometragem    | 48.300 km        | 62.100 km        | 31.400 km
  MECÂNICA
    Câmbio           | Automático       | Automático       | Manual
    Combustível      | Flex             | Flex             | Não informado
    Cor              | Prata            | Não informado    | Branco
  LOCALIZAÇÃO
    Cidade           | Campinas/SP      | São Paulo/SP     | Sorocaba/SP
    Distância        | ~ 42 km          | ~ 98 km          | ~ 76 km
  PREÇO
    Preço oficial    | R$ 87.900,00     | R$ 79.500,00     | R$ 112.900,00
    Atualizado em    | 12/08/2026       | 09/08/2026       | 14/08/2026
  TRANSPARÊNCIA
    Origem           | Frota corporativa, único proprietário PJ
                     | Não informado
                     | Particular, dois proprietários
    Condição         | Revisões em concessionária até 40.000 km
                     | Revisões em dia
                     | Não informado
    Histórico        | badge tonal "Limitação declarada" (#ffdbc7/#733600)
                     | Sem registro de sinistro nas bases consultadas
                     | badge tonal "Limitação declarada"

Todos os "Não informado" em itálico #78767c, no MESMO tamanho dos demais valores
(16px). Nenhuma linha é suprimida por estar vazia em uma das colunas — o
alinhamento horizontal é o ponto da tela.
Nas linhas de TRANSPARÊNCIA, o texto pode quebrar em até três linhas e as células
alinham pelo topo.

RODAPÉ DA TABELA — uma linha final com uma célula por veículo, cada uma contendo um
botão primário LARANJA de largura total, altura 44px: "Tenho interesse".
Esta é a única exceção à regra de um laranja por tela: são três instâncias da MESMA
ação, uma por coluna, e não três ações diferentes competindo.
```

---

## 14. Ordem sugerida de geração

| Ordem | Tela | Por quê |
|---|---|---|
| 1 | **T03** Detalhe do veículo | Define os cards, a tipografia de preço, o tratamento dos fatos e o `Não informado`. Acerte essa primeiro — tudo herda dela. |
| 2 | **T01** Resultado do catálogo | Define o card de veículo, que reaparece em T05 e no cabeçalho de T06. |
| 3 | **T03-b** Reservado sem conteúdo | Prova o estado sem PRD-B. Se essa parecer quebrada, o RF-01 não está atendido. |
| 4 | T01-b, T01-c | Variantes de estado da listagem, geradas a partir da T01 aprovada. |
| 5 | **T04** Interesse | Herda o card de veículo em formato horizontal. |
| 6 | M02, T03-c | Modal e página curta — os menores, herdam tudo. |
| 7 | T05, T06 | Fase 2. Herdam o card e a linguagem de tabela. |
| 8 | T01-m | Mobile, por último: já com o card desktop estabilizado. |

Total: **11 telas** (5 principais + 1 modal + 4 variantes de estado + 1 mobile).

---

## 15. Checklist de revisão do HTML gerado

Antes de aprovar qualquer tela, confira:

- [ ] Nenhuma ocorrência de "verificado", "certificado", "garantido", "vistoriado", "selo" ou "aprovado"
- [ ] Nenhum ✓ verde, escudo ou medalha ao lado de dado do veículo (DA-02)
- [ ] No máximo **um** botão laranja por tela — e ele é `Tenho interesse` (exceção: T06, §13)
- [ ] Todo `Não informado` está em itálico `#78767c`, no mesmo tamanho do valor vizinho, e nenhuma linha foi omitida
- [ ] Nenhum card tem sombra; a profundidade vem do branco sobre `#f9f9ff`
- [ ] Nenhuma tabela tem borda vertical
- [ ] Preços, quilometragens, anos, distâncias e datas usam algarismos tabulares
- [ ] Nenhum azul em lugar nenhum
- [ ] A placa do veículo não aparece em tela alguma (DUX-11)
- [ ] Nome de operador da Operação central não aparece em tela alguma (DUX-07)
- [ ] Onde falta foto, o placeholder é neutro e resolvido — sem texto de erro
