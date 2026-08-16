# Gestão do Estoque Curado e Disponibilidade

## Visão Geral

Esta funcionalidade consolida a operação da oferta de carros seminovos e usados sob responsabilidade da Operação central. Ela permite cadastrar veículos, formar ofertas curadas, manter fatos conhecidos, definir preço oficial, controlar disponibilidade e indicar quais ofertas estão aptas para apresentação em D01.

O PRD reúne F01–F06 porque essas capacidades compartilham usuários, entidades, regras e um resultado único: disponibilizar ao catálogo público ofertas confiáveis, transparentes e operacionalmente válidas.

## Rastreabilidade

### Vision Doc

- **Objetivo atendido**: validar a jornada de descoberta transparente até o interesse qualificado.
- **Restrições aplicáveis**: catálogo inicialmente curado ou simulado; nenhuma integração comercial obrigatória; operação nacional progressiva e assistida.
- **Non-Goals respeitados**: marketplace aberto, compra ou pagamento integralmente online, dependência de estoque real, motos, certificação formal e substituição de legado.

### Domain Doc

- **Domínio**: D02 — Estoque Curado e Disponibilidade.
- **Features**: F01 Cadastro e manutenção de veículos; F02 Curadoria da oferta; F03 Gestão de fatos conhecidos; F04 Gestão do preço oficial; F05 Gestão da disponibilidade operacional; F06 Elegibilidade para publicação em D01.
- **Entidades**: Veículo, Oferta curada, Estoque curado, Origem conhecida, Condição conhecida, Histórico disponível, Preço oficial e Disponibilidade operacional.
- **Regras**: RN-01 a RN-10.
- **Dependências upstream**: nenhuma obrigatória na Fase 1.
- **Dependências downstream**: D01 Catálogo e Descoberta; D03 Interesse e Atendimento; D04 em fase posterior.
- **Eventos produzidos**: "estoque.oferta-incluida", "estoque.oferta-atualizada", "estoque.oferta-retirada" e "estoque.disponibilidade-alterada".
- **Eventos consumidos**: nenhum obrigatório na Fase 1.

## Termos Canônicos

| Termo | Definição |
|---|---|
| Cadastro em preparação | Registro ainda incompleto, mantido pela Operação central, que não pode ser considerado elegível para D01. |
| Solicitação pendente | Alteração de elegibilidade, preço ou retirada aguardando validação. |
| Responsável de validação | Papel operacional que aprova ou rejeita solicitações de elegibilidade, preço e retirada. |
| Elegibilidade | Condição de uma oferta cumprir os critérios mínimos para ser fornecida a D01. |

## Objetivos

- Permitir que a Operação central mantenha o ciclo completo da oferta sem depender de integração externa.
- Garantir que 100% das ofertas aprovadas como elegíveis cumpram os critérios mínimos e declarem limitações conhecidas.
- Validar ou rejeitar 90% das solicitações completas em até um dia útil.
- Fazer com que alterações aprovadas de preço, disponibilidade ou retirada sejam refletidas em D01 em até uma hora.
- Avaliar as metas nos primeiros 30 dias após o lançamento.

## Histórias de Usuário

- Como **Operador de estoque**, quero cadastrar um veículo mesmo antes de completar todas as informações, para concluir o registro progressivamente.
- Como **Operador de estoque**, quero manter fatos, preço e disponibilidade, para que a oferta represente a situação conhecida pela operação.
- Como **Responsável de validação**, quero revisar solicitações pendentes em uma fila, para controlar alterações com impacto público e comercial.
- Como **comprador final**, quero receber informações transparentes sobre a oferta, inclusive limitações conhecidas, para avaliar opções com mais segurança.
- Como **D01**, quero receber apenas ofertas elegíveis e seus dados operacionais vigentes, para apresentá-las ao comprador.

## Funcionalidades Principais

### RF-01: Cadastro e manutenção de veículos

**Descrição**: Permitir o registro e a manutenção de carros seminovos ou usados sob responsabilidade da operação.

**Critérios de Aceitação**:

- **Given** um carro seminovo ou usado com dados parciais  
  **When** o Operador salvar o registro  
  **Then** o cadastro ficará em preparação e não poderá ser indicado como elegível.

- **Given** um cadastro em preparação com os dados mínimos preenchidos  
  **When** o Operador solicitar a elegibilidade  
  **Then** o cadastro poderá seguir para validação.

- **Given** um veículo que não seja carro seminovo ou usado  
  **When** o Operador tentar incluí-lo  
  **Then** o registro será recusado.

**Prioridade**: Must Have  
**Rastreabilidade**: RN-01, RN-02, RN-10

### RF-02: Curadoria e retirada da oferta

**Descrição**: Permitir incluir ou retirar ofertas sob controle da Operação central.

**Critérios de Aceitação**:

- **Given** um cadastro em preparação ou uma oferta existente  
  **When** o Operador solicitar inclusão ou retirada  
  **Then** a alteração ficará pendente de validação.

- **Given** uma solicitação pendente  
  **When** o Responsável aprovar  
  **Then** a alteração será aplicada e a retirada não alterará automaticamente a disponibilidade operacional.

- **Given** uma solicitação pendente  
  **When** o Responsável rejeitar  
  **Then** o estado vigente permanecerá ativo e o Operador receberá o motivo.

**Prioridade**: Must Have  
**Rastreabilidade**: RN-02, RN-05, RN-07

### RF-03: Gestão de fatos conhecidos

**Descrição**: Permitir registrar origem, condição e histórico disponíveis, sempre declarando fontes, evidências e limitações quando aplicável.

**Critérios de Aceitação**:

- **Given** uma informação com fonte ou evidência disponível  
  **When** o Operador registrá-la  
  **Then** a fonte ou evidência será mantida junto do fato.

- **Given** condição ou histórico indisponível  
  **When** o Operador mantiver a oferta  
  **Then** a limitação será declarada e a ausência não impedirá a elegibilidade por si só.

- **Given** uma oferta elegível  
  **When** uma alteração fizer com que ela deixe de cumprir os critérios mínimos  
  **Then** sua elegibilidade será suspensa até correção e nova validação.

**Prioridade**: Must Have  
**Rastreabilidade**: RN-03, RN-07, RN-09

### RF-04: Gestão do preço oficial

**Descrição**: Permitir definir e atualizar o preço oficial da oferta com validação do Responsável.

**Critérios de Aceitação**:

- **Given** uma oferta sem preço oficial  
  **When** o Operador tentar solicitar elegibilidade  
  **Then** a solicitação será bloqueada.

- **Given** uma alteração de preço pendente  
  **When** ela ainda não tiver sido aprovada  
  **Then** o preço vigente permanecerá válido.

- **Given** uma alteração de preço aprovada  
  **When** a atualização for concluída  
  **Then** o novo preço oficial será fornecido a D01 em até uma hora.

**Prioridade**: Must Have  
**Rastreabilidade**: RN-06, RN-07

### RF-05: Gestão da disponibilidade operacional

**Descrição**: Controlar os estados "disponível", "reservado" e "vendido".

**Critérios de Aceitação**:

- **Given** uma oferta disponível  
  **When** a operação registrar uma reserva  
  **Then** o estado passará para "reservado".

- **Given** uma oferta reservada  
  **When** a reserva for encerrada por ação explícita da operação  
  **Then** o estado poderá retornar para "disponível".

- **Given** uma oferta reservada  
  **When** a venda for concluída  
  **Then** o estado passará para "vendido".

- **Given** uma oferta vendida cuja venda foi cancelada  
  **When** o Operador solicitar a reversão  
  **Then** o retorno para "disponível" exigirá validação do Responsável.

**Prioridade**: Must Have  
**Rastreabilidade**: RN-04, RN-08

### RF-06: Elegibilidade para publicação em D01

**Descrição**: Indicar quais ofertas cumprem os critérios mínimos para serem fornecidas ao catálogo público.

**Critérios de Aceitação**:

- **Given** uma oferta com identificação, dados básicos, localização, preço oficial e disponibilidade conhecidos  
  **When** o Responsável aprovar a elegibilidade  
  **Then** a oferta será indicada como elegível para D01.

- **Given** condição ou histórico ausente  
  **When** a limitação estiver declarada  
  **Then** a oferta poderá ser elegível sem certificação formal.

- **Given** uma oferta retirada ou sem critério mínimo  
  **When** D01 solicitar ofertas elegíveis  
  **Then** essa oferta não será fornecida.

**Prioridade**: Must Have  
**Rastreabilidade**: RN-03, RN-05, RN-07, RN-09

## Experiência do Usuário

O Operador inicia pelo cadastro em preparação, completa os dados conhecidos e mantém a oferta. Alterações de elegibilidade, preço e retirada aparecem como solicitações pendentes.

O Responsável acessa uma fila com veículo, tipo de alteração, estado atual e data. Pode aprovar ou rejeitar cada solicitação; rejeições exigem justificativa.

A Operação central visualiza o valor atual, a data de atualização, o responsável e as fontes ou limitações. O comprador não acessa o histórico interno completo, mas recebe as limitações e a data de atualização por meio de D01.

## Decisões de Produto

| ID | Decisão | Alternativa descartada | Impacto |
|---|---|---|---|
| DP-01 | F01–F06 serão tratados em um único PRD e MVP integrado. | Seis PRDs independentes, por duplicarem contexto e fragmentarem o fluxo. | Define o escopo do documento e do rollout. |
| DP-02 | Elegibilidade, preço e retirada exigem validação separada. | Operação com uma única etapa sem revisão. | Cria fila de pendências e preserva o estado vigente. |
| DP-03 | Dados ausentes podem ser aceitos com limitações declaradas. | Exigir condição, histórico ou certificação formal. | Mantém o MVP compatível com dados curados ou simulados. |
| DP-04 | A reserva não expira automaticamente. | Política de prazo automático sem base operacional definida. | Exige ação explícita para liberar a oferta. |
| DP-05 | Histórico completo será interno e ficará para a Fase 2. | Incluir auditoria ampliada no MVP. | Mantém o primeiro recorte focado. |

Não há Product Decision Records criados ou herdados.

## Restrições Técnicas de Alto Nível

- O MVP não depende de integração comercial externa nem de estoque real.
- Dados curados ou simulados são aceitos na Fase 1.
- D02 mantém os fatos, o preço oficial e a disponibilidade; D01 controla a apresentação.
- Requisitos legais brasileiros de privacidade e relação de consumo devem ser definidos antes da operação comercial real.
- A atualização para D01 deve ocorrer em até uma hora após aprovação.

## Não-Objetivos

- Busca, comparação e apresentação pública, pertencentes a D01.
- Recebimento e qualificação de interesses ou organização de test drive, pertencentes a D03.
- Compra assistida, financiamento, pagamento e documentação online, pertencentes a D04 ou fora do primeiro recorte.
- Marketplace aberto, motos e outras categorias.
- Certificação formal de condição ou histórico.
- Expiração automática de reservas.
- Histórico interno completo no MVP.
- Integração comercial obrigatória na Fase 1.

## Plano de Rollout Faseado

### MVP — Fase 1

- **Inclui**: RF-01 a RF-06.
- **Critério para avançar**: 100% das ofertas elegíveis conformes; 90% das solicitações completas validadas em até um dia útil; atualizações aprovadas refletidas em D01 em até uma hora.

### Fase 2

- **Inclui**: histórico interno completo das alterações e acompanhamento da qualidade e atualidade dos dados.
- **Critério para avançar**: operação usando o histórico para corrigir dados e manter as metas de conformidade.

### Fase 3

- **Inclui**: integração com uma fonte real de estoque ou operação comercial.
- **Critério de sucesso**: dados externos incorporados sem quebrar as regras de curadoria, transparência, preço e disponibilidade.

## Métricas de Sucesso

- **Conformidade de elegibilidade**: percentual de ofertas elegíveis que cumprem os critérios mínimos e declaram limitações. **Meta**: 100% nos primeiros 30 dias.
- **Tempo de validação**: percentual de solicitações completas aprovadas ou rejeitadas em até um dia útil. **Meta**: 90% nos primeiros 30 dias.
- **Atualidade operacional**: tempo entre aprovação de preço, disponibilidade ou retirada e reflexão em D01. **Meta**: até uma hora nos primeiros 30 dias.

## Riscos e Mitigações

- **Fila de validação tornar-se gargalo** — acompanhar o prazo de um dia útil e redistribuir responsabilidades.
- **Dados incompletos reduzirem a confiança** — exigir limitações declaradas e impedir elegibilidade fora dos critérios.
- **Preço ou disponibilidade desatualizados** — medir o prazo de atualização e manter a Operação central como autoridade.
- **Operação crescer além da capacidade de atendimento** — ampliar cobertura progressivamente e manter localização explícita.
- **Requisitos legais atrasarem a operação real** — tratar privacidade e relação de consumo antes da Fase 3.

## Alternativas Consideradas

### Abordagem Escolhida: MVP ponta a ponta com gate de qualidade

Entrega F01–F06 em conjunto, com dois papéis, validações pendentes, transparência e fornecimento de ofertas elegíveis a D01. Foi escolhida por validar o objetivo completo da Fase 1.

### Alternativa Rejeitada: Fundação operacional primeiro

Adia a elegibilidade e a conexão com D01. Reduz o esforço inicial, mas não valida a jornada de descoberta transparente.

### Alternativa Rejeitada: Governança ampliada desde o início

Inclui histórico completo, políticas detalhadas de validade e controles adicionais no MVP. Aumenta a governança, mas prolonga a entrega e viola YAGNI.

## Questões em Aberto

- **Requisitos legais e de privacidade** — responsável: Product Owner e apoio jurídico; prazo: antes da Fase 3; impacto: pode impedir operação comercial real.
- **Capacidade e distribuição dos papéis operacionais** — responsável: Operação central; prazo: antes da expansão; impacto: pode comprometer o SLA de validação.
- **Indicadores detalhados de qualidade da Fase 2** — responsável: Product Owner e Operação central; prazo: antes da Fase 2; impacto: limita a evolução do monitoramento.
- **Fonte real para a integração da Fase 3** — responsável: Product Owner e Operação central; prazo: antes da Fase 3; impacto: condiciona o planejamento da integração.
- **Apresentação final de limitações e data de atualização em D01** — responsável: Product Owner e D01; prazo: antes da integração entre domínios; impacto: pode afetar a experiência de transparência.
