# Domain Map

## Visão Geral da Decomposição

A visão descreve uma loja digital de catálogo curado de carros seminovos e usados, com alcance nacional e operação central. O fluxo de valor principal é levar o comprador da descoberta transparente de um veículo até uma manifestação de interesse que possa ser continuada pela operação. A compra assistida e o financiamento são uma evolução posterior, não parte do primeiro recorte.

Os principais atores são o comprador final, a operação central e o Product Owner/decisor. A operação central aparece em mais de um contexto porque exerce responsabilidades diferentes: controla a oferta e suas informações, recebe e conduz interesses e, futuramente, apoia a compra. Isso não significa que esses contextos devam compartilhar a mesma linguagem ou fronteira.

As capacidades implícitas na visão são:

- manter uma oferta de veículos curada, com origem, condição conhecida, histórico disponível, preço e disponibilidade;
- tornar essa oferta encontrável e compreensível para o comprador;
- receber e qualificar manifestações de interesse;
- continuar o atendimento e organizar, quando aplicável, um test drive;
- evoluir um interesse qualificado para compra assistida e apresentar opções de financiamento.

A decomposição conceitual mantém os quatro domínios inicialmente identificados na visão:

| Domínio | Papel no fluxo de valor | Roadmap |
|---|---|---|
| **Catálogo e Descoberta** | Permitir que o comprador encontre, compare e compreenda os veículos elegíveis para apresentação. | Fase 1 |
| **Estoque Curado e Disponibilidade** | Controlar a oferta sob responsabilidade da operação e os fatos conhecidos sobre cada veículo. | Fase 1 |
| **Interesse e Atendimento** | Transformar uma manifestação de interesse em contexto suficiente para continuidade do contato e eventual test drive. | Fase 1 |
| **Compra Assistida e Financiamento** | Apoiar a evolução de um interesse qualificado para uma compra assistida, com opções de financiamento compreensíveis. | Fase 2 |

### Linguagem transversal inicial

| Termo | Uso conceitual neste mapa |
|---|---|
| **Carro seminovo** | Carro usado que compõe a oferta da loja e pode ser apresentado para uma possível compra. |
| **Catálogo curado** | Conjunto de veículos selecionados e mantidos sob responsabilidade da operação central. |
| **Operação central** | Equipe que controla a oferta, mantém as informações e dá continuidade aos interessados. |
| **Transparência das informações** | Apresentação clara do que é conhecido sobre origem, condição, histórico, preço e disponibilidade, incluindo limitações. |
| **Interesse qualificado** | Manifestação com contexto suficiente para a operação prosseguir com o atendimento. |
| **Compra assistida** | Jornada em que a operação apoia o comprador na evolução do interesse até a compra, sem exigir conclusão integralmente autônoma. |

## Lista de Domínios

## Catálogo e Descoberta

### 1. Responsabilidade Principal

Organizar e apresentar a oferta curada para que o comprador encontre, compare e compreenda os veículos disponíveis, com transparência sobre as informações conhecidas e suas limitações.

### 2. O Que NÃO Faz

- Não decide quais veículos entram ou saem da oferta; essa responsabilidade pertence a **Estoque Curado e Disponibilidade**.
- Não é a fonte de verdade para condição, histórico, preço ou disponibilidade operacional do veículo.
- Não recebe nem qualifica o interesse do comprador; essa responsabilidade pertence a **Interesse e Atendimento**.
- Não conduz a compra assistida, não define financiamento e não conclui pagamento ou documentação.

### 3. Entidades Principais (conceituais)

- **Catálogo público:** conjunto de itens da oferta que podem ser descobertos pelo comprador.
- **Item do catálogo:** representação pública de um veículo da oferta curada, incluindo as informações que podem ser apresentadas.
- **Critério de descoberta:** intenção ou filtro usado pelo comprador para encontrar opções adequadas.
- **Resultado de descoberta:** conjunto de itens que respondem a um critério de busca ou comparação.
- **Comparação de ofertas:** visão que permite ao comprador interpretar diferenças relevantes entre opções apresentadas.

### 4. Linguagem Ubíqua

- **Catálogo público:** visão da oferta disponível para descoberta; não é o estoque operacional em si.
- **Item do catálogo:** forma como um veículo curado é apresentado ao comprador.
- **Descoberta:** ato de encontrar opções potencialmente adequadas ao uso do comprador.
- **Transparência das informações:** exposição do que é conhecido e do que permanece limitado ou não verificado.
- **Disponibilidade exibida:** disponibilidade recebida do domínio operacional e mostrada ao comprador; não é uma decisão local do catálogo.

### 5. Eventos ou Interações

- Recebe de **Estoque Curado e Disponibilidade** os veículos elegíveis para apresentação e as atualizações relevantes sobre seus fatos e disponibilidade.
- Oferece ao comprador meios de descobrir, compreender e comparar as opções curadas.
- Encaminha para **Interesse e Atendimento** o contexto do veículo e da jornada quando o comprador manifesta interesse.
- Não cria uma cópia concorrente da decisão operacional sobre disponibilidade; quando a informação muda, a apresentação deve refletir a decisão de **Estoque Curado e Disponibilidade**.

### 6. Justificativa da Separação

Descobrir e apresentar uma oferta é uma responsabilidade orientada à compreensão do comprador, enquanto controlar a oferta e seus fatos é uma responsabilidade operacional. Separar as duas linguagens evita que filtros, ordenação ou conteúdo de apresentação passem a decidir a verdade do estoque. Também permite que o catálogo evolua como experiência de descoberta sem absorver atendimento ou compra.

## Estoque Curado e Disponibilidade

### 1. Responsabilidade Principal

Representar e controlar a oferta de veículos sob responsabilidade da operação central, incluindo origem, condição conhecida, histórico disponível, preço e disponibilidade operacional, seja o catálogo inicial real, curado ou simulado.

### 2. O Que NÃO Faz

- Não organiza a experiência pública de busca, comparação e apresentação; essa responsabilidade pertence a **Catálogo e Descoberta**.
- Não conduz conversas com compradores nem qualifica manifestações de interesse.
- Não é responsável pela progressão da compra assistida ou pela explicação completa de opções de financiamento.
- Não presume certificação formal da condição ou do histórico como requisito da primeira fase.

### 3. Entidades Principais (conceituais)

- **Veículo:** carro seminovo ou usado que pode compor a oferta da operação.
- **Oferta curada:** decisão de manter um veículo sob responsabilidade da operação para possível apresentação.
- **Origem conhecida:** informação sobre a procedência ou origem disponível para o veículo.
- **Condição conhecida:** conjunto de fatos conhecidos sobre a condição do veículo, sem equivaler a uma certificação formal.
- **Histórico disponível:** informações de histórico que a operação conseguiu reunir, incluindo suas limitações.
- **Disponibilidade operacional:** situação que indica se o veículo pode ser considerado disponível para a jornada do comprador.

### 4. Linguagem Ubíqua

- **Estoque curado:** conjunto operacional de veículos selecionados e mantidos pela operação; pode ser inicialmente simulado e não pressupõe uma integração comercial.
- **Oferta curada:** veículo que a operação decidiu controlar e tornar elegível para apresentação.
- **Disponibilidade:** condição operacional do veículo para ser apresentado ou continuar uma jornada; os estados detalhados ainda precisam ser definidos.
- **Condição conhecida:** aquilo que a operação sabe e consegue declarar sobre o veículo, sem afirmar mais do que as evidências disponíveis.
- **Histórico disponível:** histórico que pode ser informado ao comprador, acompanhado das lacunas conhecidas.
- **Transparência das informações:** obrigação de não ocultar limitações relevantes dos dados da oferta.

### 5. Eventos ou Interações

- Inclui, mantém, atualiza ou retira veículos da oferta curada conforme decisões da operação central.
- Fornece a **Catálogo e Descoberta** os fatos e a disponibilidade que podem ser apresentados ao comprador.
- Fornece a **Interesse e Atendimento** e a **Compra Assistida e Financiamento** o contexto operacional necessário para tratar um veículo durante uma jornada.
- Comunica mudanças relevantes de disponibilidade ou de informações conhecidas para que os demais domínios não continuem tratando uma oferta desatualizada como válida.

### 6. Justificativa da Separação

Este domínio concentra a responsabilidade pela verdade operacional da oferta. Sua preocupação é qualidade, atualização, curadoria e disponibilidade dos veículos, que são políticas diferentes da experiência de descoberta e do tratamento de interessados. Mantê-lo separado evita que a operação perca controle sobre os fatos ao distribuí-los entre telas, atendimentos ou etapas futuras da compra.

## Interesse e Atendimento

### 1. Responsabilidade Principal

Receber manifestações de interesse, reunir contexto suficiente para qualificá-las e apoiar a continuidade do contato pela operação central, incluindo a possibilidade de organizar um test drive.

### 2. O Que NÃO Faz

- Não decide quais veículos compõem o estoque curado nem altera a fonte operacional de suas informações.
- Não é responsável pela busca, comparação ou apresentação pública do catálogo.
- Não transforma automaticamente uma manifestação em compra, contrato, pagamento ou financiamento.
- Não define sozinho a política de disponibilidade do veículo; consulta o contexto de **Estoque Curado e Disponibilidade** quando necessário.

### 3. Entidades Principais (conceituais)

- **Manifestação de interesse:** sinal do comprador de que deseja avançar sobre um veículo ou oportunidade.
- **Interesse qualificado:** manifestação que contém contexto suficiente para a operação prosseguir com o atendimento.
- **Atendimento:** continuidade organizada do contato entre o comprador e a operação central.
- **Contexto do comprador:** informações fornecidas pelo comprador que ajudam a operação a entender a intenção e o próximo passo.
- **Solicitação de test drive:** pedido para vivenciar o veículo, sujeito à disponibilidade e ao modelo operacional definido pela operação.
- **Agendamento de test drive:** combinação de um possível test drive, quando a operação tiver condições de organizá-lo.

### 4. Linguagem Ubíqua

- **Manifestação de interesse:** primeiro sinal de intenção; ainda não significa interesse suficiente para uma ação operacional completa.
- **Interesse qualificado:** interesse com informação bastante para definir uma continuidade de atendimento.
- **Atendimento:** processo de contato e acompanhamento conduzido ou apoiado pela operação central.
- **Test drive:** experiência de avaliação do veículo que pode ser solicitada ou organizada, sem pressupor regras logísticas já definidas.
- **Continuidade:** próximo contato ou ação acordada entre comprador e operação depois da manifestação.

### 5. Eventos ou Interações

- Recebe de **Catálogo e Descoberta** o contexto da opção sobre a qual o comprador demonstrou interesse.
- Consulta **Estoque Curado e Disponibilidade** para tratar a situação conhecida do veículo durante o atendimento.
- Registra e qualifica a manifestação conforme o contexto disponível e a capacidade da operação central.
- Pode organizar ou encaminhar uma solicitação de test drive, conforme o modelo operacional que vier a ser definido.
- Entrega a **Compra Assistida e Financiamento** o contexto de um interesse qualificado quando o comprador estiver pronto para essa evolução.

### 6. Justificativa da Separação

O interesse do comprador e sua continuidade exigem uma linguagem própria, centrada em intenção, contexto, contato e próximos passos. Essa responsabilidade começa depois da descoberta, mas antes da compra. Separá-la evita que o catálogo se torne responsável por conversas e que a compra assistida trate manifestações ainda não qualificadas.

## Compra Assistida e Financiamento

### 1. Responsabilidade Principal

Evoluir um interesse qualificado para uma jornada de compra assistida, apresentando condições e opções de financiamento de forma compreensível e deixando explícitas as responsabilidades da operação.

### 2. O Que NÃO Faz

- Não mantém a fonte de verdade sobre veículo, preço ou disponibilidade; consulta **Estoque Curado e Disponibilidade**.
- Não recebe a primeira manifestação nem define sozinho quando um interesse está qualificado; essa responsabilidade pertence a **Interesse e Atendimento**.
- Não substitui o catálogo nem conduz a descoberta inicial do comprador.
- Não pressupõe conclusão integral da compra, pagamento, documentação ou aprovação de crédito exclusivamente online no primeiro recorte.
- Não assume responsabilidades de parceiros de financiamento que ainda não tenham sido definidas.

### 3. Entidades Principais (conceituais)

- **Jornada de compra assistida:** evolução acompanhada de um interesse qualificado em direção à compra.
- **Opção de financiamento:** alternativa de financiamento que pode ser apresentada ao comprador.
- **Condição de financiamento:** condições conhecidas e comunicáveis de uma opção, sem confundi-las com aprovação definitiva.
- **Solicitação de apoio à compra:** pedido do comprador para que a operação conduza ou apoie a próxima etapa.
- **Responsabilidade da operação:** definição de quais ações e decisões cabem à operação em cada etapa assistida.

### 4. Linguagem Ubíqua

- **Compra assistida:** compra em que a operação apoia o comprador e não exige uma jornada totalmente autônoma.
- **Interesse qualificado:** contexto recebido de **Interesse e Atendimento** que permite iniciar a evolução da compra.
- **Opção de financiamento:** possibilidade apresentada para apoiar a decisão, não promessa de crédito aprovado.
- **Condição apresentada:** informação de compra ou financiamento comunicada ao comprador com suas limitações e dependências.
- **Responsabilidade da operação:** limite explícito entre o que a equipe central faz, o que depende do comprador e o que depende de terceiros.

### 5. Eventos ou Interações

- Recebe de **Interesse e Atendimento** o contexto de um interesse qualificado.
- Consulta **Estoque Curado e Disponibilidade** para manter o veículo e suas informações operacionais coerentes durante a jornada.
- Apoia a operação central na explicação das próximas etapas, das condições conhecidas e das opções de financiamento.
- Poderá interagir com parceiros de financiamento quando escopo, condições e responsabilidades forem definidos; isso é uma possibilidade de negócio, não uma integração assumida neste mapa.
- Devolve à operação o estado conceitual da jornada assistida para continuidade do relacionamento, sem assumir a responsabilidade pelo atendimento inicial.

### 6. Justificativa da Separação

A compra assistida introduz políticas, responsabilidades e possíveis dependências financeiras que não existem na descoberta ou na simples manifestação de interesse. Mantê-la como domínio da Fase 2 protege o escopo da Fase 1 e permite detalhar financiamento somente quando parceiros, condições, responsabilidades e requisitos legais estiverem claros. No recorte atual, compra assistida e financiamento permanecem juntos porque fazem parte da mesma evolução de valor e ainda não há evidência de que precisem de fronteiras independentes.

## Dependências Entre Domínios

```text
D02 Estoque Curado e Disponibilidade ──fatos e disponibilidade──→ D01 Catálogo e Descoberta
D01 Catálogo e Descoberta             ──contexto da descoberta──→ D03 Interesse e Atendimento
D02 Estoque Curado e Disponibilidade ──contexto operacional────→ D03 Interesse e Atendimento
D03 Interesse e Atendimento          ──interesse qualificado───→ D04 Compra Assistida e Financiamento
D02 Estoque Curado e Disponibilidade ──contexto do veículo─────→ D04 Compra Assistida e Financiamento
```

As dependências são informacionais e de continuidade de negócio, não decisões sobre componentes físicos. O domínio downstream consome o contexto necessário, mas não passa a ser dono dos conceitos do upstream.

| Origem | Depende de | O que recebe | Criticidade conceitual |
|---|---|---|---|
| D01 Catálogo e Descoberta | D02 Estoque Curado e Disponibilidade | Oferta elegível, fatos conhecidos e disponibilidade a exibir. | Alta |
| D03 Interesse e Atendimento | D01 Catálogo e Descoberta | Veículo e contexto da descoberta que originaram o interesse. | Alta |
| D03 Interesse e Atendimento | D02 Estoque Curado e Disponibilidade | Situação operacional conhecida do veículo durante o atendimento. | Média |
| D04 Compra Assistida e Financiamento | D03 Interesse e Atendimento | Interesse qualificado e contexto de continuidade. | Alta |
| D04 Compra Assistida e Financiamento | D02 Estoque Curado e Disponibilidade | Fatos e disponibilidade necessários para a jornada assistida. | Alta |

## Pontos de Atenção

### Fronteiras que exigem validação

- **Catálogo x estoque:** a visão usa “oferta”, “catálogo curado” e “disponibilidade” em contextos próximos. Neste mapa, D02 é a autoridade conceitual sobre fatos operacionais; D01 mantém apenas a representação para descoberta. É preciso validar posteriormente quais informações, especialmente preço, podem ser ajustadas na apresentação sem alterar a oferta.
- **Disponibilidade x test drive:** ainda não está definido se uma solicitação ou agendamento de test drive altera a disponibilidade do veículo ou cria algum compromisso operacional. A decisão deve preservar D02 como dono da disponibilidade e D03 como dono do atendimento do test drive.
- **Interesse x compra:** D03 termina na qualificação e continuidade do interesse; D04 começa quando existe intenção e contexto suficientes para compra assistida. Os critérios de passagem precisam ser definidos antes da Fase 2.
- **Compra assistida x financiamento:** permanecem juntos como hipótese de domínio. Parceiros, condições, aprovação de crédito, documentação e responsabilidades legais ainda estão em aberto.

### Candidatos a fusão

- **D01 Catálogo e Descoberta + D03 Interesse e Atendimento:** podem ser tratados como uma frente operacional única em um MVP muito pequeno, caso a mesma equipe e a mesma política conduzam descoberta e contato. A recomendação conceitual é manter as fronteiras separadas para não misturar apresentação pública com atendimento.
- **D03 Interesse e Atendimento + D04 Compra Assistida e Financiamento:** só faria sentido se a compra permanecer inteiramente manual e sem políticas financeiras próprias. A necessidade de condições de financiamento ou parceiros tende a justificar a separação.

### Candidatos a divisão

- **D04 Compra Assistida e Financiamento:** deve ser dividido em “Compra Assistida” e “Financiamento” se o financiamento adquirir regras, parceiros, obrigações regulatórias ou ciclo de decisão próprios.
- **D03 Interesse e Atendimento:** pode ser dividido entre atendimento de interesse e operação de test drive se o agendamento passar a ter logística, capacidade e políticas independentes.
- **D02 Estoque Curado e Disponibilidade:** só deve ser dividido entre curadoria das informações e disponibilidade se essas responsabilidades tiverem políticas ou responsáveis efetivamente distintos; a visão atual não exige essa granularidade.

### Restrições e lacunas da visão

- O modelo operacional de atendimento, test drive, entrega e fechamento ainda não foi definido.
- Os critérios formais de qualidade, atualização e eventual verificação das informações dos veículos ainda estão em aberto.
- A abrangência nacional é uma intenção de oferta e comunicação; a capacidade real de atendimento poderá ser progressiva e assistida.
- Privacidade, relação de consumo e demais requisitos legais brasileiros precisam ser detalhados antes de uma operação comercial real.
- Não há, neste mapa, um domínio separado para autenticação, governança, privacidade ou parceiros externos: a visão ainda não apresenta esses temas como fluxos de valor independentes. Eles devem ser reavaliados quando deixarem de ser restrições transversais e passarem a possuir responsabilidades próprias.

## Decisões Estruturais Tomadas

- A decomposição mantém quatro contextos conceituais: Catálogo e Descoberta; Estoque Curado e Disponibilidade; Interesse e Atendimento; Compra Assistida e Financiamento.
- D02 é a referência conceitual para a oferta curada, os fatos conhecidos do veículo e a disponibilidade operacional.
- D01 é responsável pela descoberta, comparação e apresentação; não é uma segunda fonte de verdade do estoque.
- D03 cobre a jornada desde a manifestação até o interesse qualificado e a possível organização de test drive.
- D04 pertence à Fase 2 e não transforma a plataforma em um e-commerce de compra, pagamento e documentação integralmente online.
- “Oferta curada” será usada para a decisão operacional de D02; “item do catálogo” será usado para a representação pública em D01, reduzindo a sobreposição do termo “oferta”.
- A decomposição é de domínio e linguagem, não uma definição de microservices, módulos, APIs, banco de dados ou tarefas de implementação.
- A separação foi considerada coerente e compreensível para o escopo atual: cada domínio tem uma responsabilidade principal, os conflitos conhecidos estão explicitados e não foi criada granularidade adicional sem evidência na visão.

