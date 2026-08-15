# Domain Document — Interesse e Atendimento

> **Nível 1 da hierarquia de documentação.** Este documento detalha o bounded context de um domínio específico do sistema. Sempre forneça o `docs/vision.md` junto com este arquivo ao iniciar sessões de PRD ou Tech Spec dentro deste domínio.

**Domínio:** Interesse e Atendimento (D03)  
**Responsável:** a definir  
**Status:** `done`  
**Fase do Roadmap:** Fase 1 — Descoberta e Interesse Qualificado (MVP / Foundation)  
**Última revisão:** 2026-08-15

---

## 1. Propósito do Domínio (Domain Purpose)

### Responsabilidade Principal

Receber e qualificar manifestações de interesse, registrar o consentimento e o contexto mínimo do comprador e apoiar o contato manual da operação central, incluindo a solicitação e o agendamento de test drive.

### Problema que Resolve

Depois de descobrir um carro seminovo, o comprador precisa de um caminho claro para demonstrar interesse sem perder a referência do veículo ou da jornada que originou o contato. A operação central precisa receber dados suficientes e autorização para contato, acompanhar cada caso e registrar o próximo passo. Este domínio organiza essa continuidade sem transformar uma manifestação em compra ou exigir integrações externas.

No uso cotidiano, a operação pode chamar uma manifestação ou um interesse de **Lead**. O termo de negócio deste documento continua sendo **Interesse**, para manter consistência com o `docs/vision.md` e o `context/domain-map.md`.

### Fora do Escopo deste Domínio (Out of Scope)

- Busca, comparação e apresentação pública dos veículos → **Catálogo e Descoberta (D01)**.
- Inclusão, alteração, retirada, preço, condição conhecida e disponibilidade operacional dos veículos → **Estoque Curado e Disponibilidade (D02)**.
- Reserva do veículo vinculada à intenção de compra e à análise financeira do comprador → **Compra Assistida e Financiamento (D04)**.
- Compra, contrato, pagamento, financiamento, documentação ou aprovação de crédito.
- Envio automático de e-mail, mensagem ou ligação, além de CRM, WhatsApp, calendário ou outro sistema externo.
- Decidir se um veículo está disponível para venda ou para test drive; D03 apenas consulta o contexto operacional de D02.

## 2. Usuários do Domínio (Domain Users)

| Perfil (Role) | O que faz neste domínio | Frequência de uso |
|---|---|---|
| Comprador final | Manifesta interesse, fornece nome e pelo menos um meio de contato, autoriza o contato e pode solicitar test drive. | Eventual, após a descoberta |
| Operação central | Analisa interesses, inicia o contato manual, registra a continuidade, sugere ou confirma consolidações e cadastra agendamentos. | Alta, diariamente |
| Product Owner / decisor | Valida critérios de qualificação, políticas de atendimento, consentimento e evolução do test drive. | Eventual |

## 3. Entidades Principais (Core Entities)

> Entidades são os objetos de negócio centrais deste domínio. Não representam schemas de banco de dados.

| Entidade | Descrição | Atributos Principais | Relacionamentos |
|---|---|---|---|
| Manifestação de interesse | Primeiro sinal de que o comprador deseja avançar sobre um item do catálogo. | item do catálogo, contexto da descoberta, data, origem | pode tornar-se: Interesse qualificado |
| Interesse qualificado | Manifestação que contém contexto e autorização suficientes para a operação prosseguir. | comprador, veículo de referência, consentimento, status | origina: Atendimento; pode ser encaminhado a D04 |
| Contexto do comprador | Informações mínimas fornecidas para permitir contato e entendimento inicial da intenção. | nome obrigatório, e-mail ou telefone, preferência opcional de horário, consentimento | associado a: Manifestação de interesse |
| Atendimento | Continuidade organizada do contato manual entre comprador e operação central. | status, responsável, registro do contato, próximo passo | acompanha: Interesse qualificado |
| Solicitação de test drive | Pedido do comprador para vivenciar o veículo. | interesse, veículo, preferência de data ou horário, observações | pode tornar-se: Agendamento de test drive |
| Agendamento de test drive | Test drive confirmado pela operação com data, horário e local registrados. | data, horário, local, confirmação | origina-se de: Solicitação de test drive |
| Sugestão de consolidação | Indicação de que dois ou mais interesses podem pertencer ao mesmo comprador ou oportunidade. | interesses relacionados, evidências, decisão da operação | pode resultar em: interesses consolidados |

## 4. Features Previstas (Planned Features)

| # | Feature | Descrição | Prioridade | Status | PRD |
|---|---|---|---|---|---|
| F01 | Captura de manifestação de interesse | Receber o interesse associado ao item do catálogo e preservar o contexto da descoberta. | Must Have | `prd-ready` | — |
| F02 | Dados mínimos e consentimento | Coletar nome, e-mail ou telefone, preferência opcional de horário e consentimento explícito para contato. | Must Have | `prd-ready` | — |
| F03 | Qualificação e ciclo do interesse | Validar os dados mínimos e acompanhar os estados operacionais do atendimento. | Must Have | `prd-ready` | — |
| F04 | Painel de interesses | Permitir que a operação central visualize, analise e priorize os interesses recebidos. | Must Have | `prd-ready` | — |
| F05 | Continuidade de atendimento | Registrar contato manual, responsável e próximo passo acordado com o comprador. | Must Have | `prd-ready` | — |
| F06 | Solicitação de test drive | Registrar o pedido de test drive vinculado ao interesse e ao veículo. | Must Have | `prd-ready` | — |
| F07 | Agendamento de test drive | Cadastrar o agendamento confirmado com data, horário e local. | Must Have | `prd-ready` | — |
| F08 | Sugestão de consolidação de interesses | Sugerir possíveis duplicidades por telefone, e-mail ou nome, sem consolidar automaticamente. | Should Have | `planned` | — |

**Prioridades (MoSCoW):** `Must Have` · `Should Have` · `Could Have` · `Won't Have`  
**Status possíveis:** `planned` · `prd-ready` · `in-progress` · `done` · `out-of-scope`

## 5. Dependências (Domain Dependencies)

### Depende de (Upstream)

| Domínio | O que consome | Tipo | Criticidade |
|---|---|---|---|
| Catálogo e Descoberta (D01) | Item do catálogo e contexto da jornada que originou o interesse | Contexto de descoberta | Alta |
| Estoque Curado e Disponibilidade (D02) | Situação operacional conhecida do veículo para atendimento e test drive | Informação de negócio | Média |

### Fornece para (Downstream)

| Domínio | O que fornece | Tipo | Criticidade |
|---|---|---|---|
| Compra Assistida e Financiamento (D04) | Interesse qualificado, contexto do comprador e continuidade registrada | Contexto de oportunidade | Alta na Fase 2 |

### Integrações Externas (External Integrations)

| Sistema Externo | Finalidade | Direção | Status |
|---|---|---|---|
| Nenhum | O contato e a organização do atendimento serão realizados manualmente pela operação central. | — | `out-of-scope` |

## 6. Regras de Negócio (Business Rules)

| ID | Regra | Origem |
|---|---|---|
| RN-01 | Toda manifestação deve preservar a referência ao item do catálogo e o contexto da descoberta recebidos de D01. | Fronteira D01/D03 |
| RN-02 | O nome do comprador é obrigatório e pelo menos um entre e-mail e telefone deve ser informado. | Decisão de negócio |
| RN-03 | O consentimento explícito para contato deve ser registrado antes de o interesse avançar para o atendimento manual. | Decisão de negócio / privacidade |
| RN-04 | A preferência de horário para contato é opcional e não impede a manifestação quando não for informada. | Decisão de negócio |
| RN-05 | O ciclo do atendimento utiliza os estados `recebido`, `em análise`, `contato iniciado`, `aguardando comprador`, `concluído` e `encerrado`. | Política operacional |
| RN-06 | O contato com o comprador é manual; D03 não envia automaticamente mensagens nem cria compromissos em sistemas externos. | Escopo da Fase 1 |
| RN-07 | Uma sugestão de consolidação pode usar coincidências de telefone, e-mail ou nome, mas nunca executa a consolidação sem confirmação da operação central. | Decisão de negócio |
| RN-08 | Uma solicitação de test drive pode existir sem agendamento; um agendamento só é confirmado quando data, horário e local estiverem registrados. | Política de test drive |
| RN-09 | Solicitar ou agendar um test drive não altera a disponibilidade operacional mantida por D02. | Fronteira D02/D03 |
| RN-10 | A reserva do veículo associada à intenção de compra e à análise financeira pertence a D04, não ao atendimento de test drive. | Fronteira D03/D04 |
| RN-11 | Uma manifestação de interesse não representa compra, pagamento, financiamento, contrato ou aprovação de crédito. | Non-goals da visão |

## 7. Eventos do Domínio (Domain Events)

### Produz (Publishes)

- `interesse.manifestado` — uma pessoa registrou interesse em um item do catálogo.
- `interesse.qualificado` — o interesse contém os dados e a autorização necessários para continuidade.
- `atendimento.iniciado` — a operação central iniciou o contato manual.
- `atendimento.atualizado` — o status, o responsável ou o próximo passo do atendimento mudou.
- `testdrive.solicitado` — o comprador solicitou um test drive.
- `testdrive.agendado` — a operação registrou data, horário e local para o test drive.
- `interesse.consolidacao-sugerida` — foi identificada uma possível duplicidade para avaliação da operação.

### Consome (Subscribes)

- `catalogo.interesse-solicitado` (de: D01) — informa o contexto do item e da descoberta que originaram o interesse.
- `estoque.disponibilidade-alterada` (de: D02) — permite que a operação considere a situação conhecida do veículo durante o atendimento, sem transferir a responsabilidade da disponibilidade para D03.

## 8. Estratégia de Desenvolvimento (Development Strategy)

### Ordem de Implementação Sugerida

1. **F01** — Captura da manifestação com referência ao item do catálogo.
2. **F02** — Dados mínimos, preferência opcional de horário e consentimento.
3. **F03** — Qualificação e estados do atendimento.
4. **F04** — Visão operacional dos interesses recebidos.
5. **F05** — Registro do contato manual e do próximo passo.
6. **F08** — Sugestões de consolidação após haver volume suficiente de interesses; depende de F01–F04.
7. **F06** — Registro da solicitação de test drive.
8. **F07** — Cadastro do agendamento com data, horário e local; depende de F06 e das regras operacionais da operação central.

### Riscos do Domínio

| Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|
| A ausência de integração de comunicação pode atrasar o primeiro contato. | Alta | Alto | Definir responsável, prioridade e prazo operacional para tratamento manual. |
| Agendamentos manuais podem conflitar em horários ou locais. | Média | Alto | Definir capacidade, confirmação, cancelamento e remarcação antes do PRD de F07. |
| Sugestões de consolidação podem unir interesses de pessoas diferentes. | Média | Alto | Manter confirmação humana, evidências da sugestão e possibilidade de desfazer a consolidação. |
| O consentimento pode não ter texto, validade ou retenção definidos. | Média | Alto | Validar aviso, registro, revogação e retenção com o Product Owner antes da operação real. |
| A fronteira entre atendimento e reserva de compra pode ficar ambígua. | Média | Alto | Definir no PRD de D04 o evento e os critérios de passagem para análise financeira. |

## 9. Questões em Aberto (Open Questions)

- [ ] Qual texto, versão e evidência devem acompanhar o consentimento para contato?
- [ ] Como o comprador revoga o consentimento e por quanto tempo os dados do interesse são mantidos?
- [ ] Qual interesse será mantido como principal após uma consolidação e como o histórico será preservado?
- [ ] Quais regras de confirmação, cancelamento, remarcação e capacidade governam o agendamento de test drive?
- [ ] Qual prazo e prioridade a operação central deve aplicar ao primeiro contato manual?
- [ ] Quais critérios e eventos formalizam a passagem do interesse qualificado para a reserva e a análise financeira em D04?

---

*Domain Doc gerado com a skill `tsg-flow-domain-creator`. Para criar PRDs das features deste domínio, use a skill `tsg-flow-prd-creator` fornecendo `docs/vision.md` e este arquivo como contexto.*
