# Domain Document — Compra Assistida e Financiamento

> **Nível 1 da hierarquia de documentação.** Este documento detalha o bounded context de um domínio específico do sistema. Sempre forneça o `docs/vision.md` junto com este arquivo ao iniciar sessões de PRD ou Tech Spec dentro deste domínio.

**Domínio:** Compra Assistida e Financiamento (D04)  
**Responsável:** Operação central — papel vendedor  
**Status:** `planned`  
**Fase do Roadmap:** Fase 2 — Compra Assistida e Financiamento  
**Última revisão:** 2026-08-15

---

## 1. Propósito do Domínio (Domain Purpose)

### Responsabilidade Principal

Evoluir um interesse qualificado para uma jornada de compra assistida conduzida pela Operação central, formalizando o comprador como cliente, registrando a forma de compra, reunindo dados e documentos para análise de crédito, apresentando condições manuais e controlando a reserva do veículo.

### Problema que Resolve

Depois que o comprador demonstra interesse, a operação precisa transformar esse contexto em uma oportunidade de compra tratável, sem perder a referência do veículo nem coletar informações de forma dispersa. Este domínio organiza a formalização feita pelo vendedor, o dossiê de análise, a escolha entre compra à vista e financiada, as condições apresentadas, a reserva e os próximos passos da proposta.

O financiamento será manual no primeiro recorte: a operação registra solicitações, condições e resultados conhecidos, mas o domínio não depende de integração com parceiros nem realiza aprovação automática de crédito.

### Fora do Escopo deste Domínio (Out of Scope)

- Receber e qualificar a primeira manifestação de interesse, manter o contato inicial ou organizar test drive → **Interesse e Atendimento (D03)**.
- Manter os fatos do veículo, o preço oficial e a disponibilidade operacional → **Estoque Curado e Disponibilidade (D02)**.
- Buscar, comparar ou apresentar publicamente os veículos → **Catálogo e Descoberta (D01)**.
- Usar um veículo usado como entrada na compra.
- Fazer análise ou aprovação automática de crédito, ou depender de uma integração com financeira.
- Concluir pagamento, assinatura contratual, transferência, entrega ou toda a compra exclusivamente online.
- Criar um perfil ou portal de autoatendimento para o comprador na primeira versão; o vendedor registra os dados em nome do comprador.

O vendedor é um papel da **Operação central**. D03 continua sendo a origem conceitual do interesse qualificado, enquanto D02 permanece como autoridade sobre o veículo e sua disponibilidade. D04 controla a reserva vinculada à compra, mas não substitui a decisão operacional de D02.

## 2. Usuários do Domínio (Domain Users)

| Perfil (Role) | O que faz neste domínio | Frequência de uso |
|---|---|---|
| Operação central — vendedor | Formaliza o interesse, cadastra o cliente, registra dados e documentos, define a forma de compra, acompanha a análise, apresenta condições e conduz a jornada. | Diária |
| Operação central — gerência | Autoriza extensão de reserva e valida exceções ou condições que exigem decisão gerencial. | Eventual, conforme as jornadas |
| Comprador final | Fornece dados e documentos, escolhe a forma de compra, valida informações e recebe as condições apresentadas. Não precisa de perfil autenticado na primeira versão. | Eventual, durante a compra |
| Product Owner / decisor | Valida regras da jornada, limites da operação e prioridades de evolução. | Eventual |

## 3. Entidades Principais (Core Entities)

> Entidades são os objetos de negócio centrais deste domínio. Não representam schemas de banco de dados.

| Entidade | Descrição | Atributos Principais | Relacionamentos |
|---|---|---|---|
| Jornada de compra assistida | Processo aberto pelo vendedor a partir de um interesse qualificado e conduzido até um próximo passo ou encerramento operacional. | estado, responsável, próximos passos, datas | origina: Cliente em compra assistida |
| Cliente em compra assistida | Comprador formalizado para a jornada; não implica criação de uma conta de acesso. | nome completo, CPF, endereço, contatos, situação | participa de: Jornada; possui: Dossiê |
| Dossiê de análise de crédito | Conjunto de informações pessoais, financeiras e documentos reunidos para análise. | renda, dados declarados, status, pendências | pertence a: Cliente; suporta: Solicitação de crédito |
| Documento ou comprovante | Arquivo fornecido para comprovar uma informação do dossiê. | tipo, formato, data, origem, situação | anexado a: Dossiê |
| Forma de compra | Decisão entre compra à vista e compra financiada. | modalidade, data de definição, observações | pertence a: Jornada |
| Condição de financiamento | Opção manual de financiamento apresentada ao comprador, sem equivaler a crédito aprovado. | valor, entrada, prazo, parcelas, taxas, validade | associada a: Jornada; pode originar: Proposta |
| Solicitação de análise de crédito | Pedido acompanhado e registrado manualmente pela operação. | data, responsável, situação, resultado conhecido | usa: Dossiê; pertence a: Jornada |
| Reserva de compra | Compromisso temporário associado à jornada para preservar a oportunidade sobre o veículo. | início, vencimento, extensão, autorização, situação | referencia: Veículo de D02 |
| Proposta de compra | Condição assistida registrada para orientar a continuidade da negociação. | valores, condições, validade, pendências, situação | pertence a: Jornada |

## 4. Features Previstas (Planned Features)

| # | Feature | Descrição | Prioridade | Status | PRD |
|---|---|---|---|---|---|
| F01 | Formalização do interesse e abertura da jornada | Permitir que o vendedor transforme um interesse qualificado em uma jornada de compra assistida e formalize o cliente. | Must Have | `planned` | — |
| F02 | Cadastro de dados pessoais e financeiros | Registrar nome completo, CPF, endereço, renda e demais dados exigidos pelo processo de análise. | Must Have | `planned` | — |
| F03 | Inclusão de documentos e comprovantes | Associar imagens ou PDFs ao dossiê, acompanhar pendências e manter a referência do documento. | Must Have | `planned` | — |
| F04 | Definição da forma de compra | Registrar se a compra será à vista ou financiada, sem incluir veículo usado como entrada. | Must Have | `planned` | — |
| F05 | Acompanhamento da jornada assistida | Controlar estados, responsáveis, pendências, próximos passos e histórico da atuação do vendedor. | Must Have | `planned` | — |
| F06 | Condições e simulação manual de financiamento | Registrar e apresentar opções de financiamento informadas manualmente pela operação. | Must Have | `planned` | — |
| F07 | Solicitação e acompanhamento manual de crédito | Registrar a solicitação, o andamento e o resultado conhecido da análise, sem aprovação automática. | Must Have | `planned` | — |
| F08 | Reserva do veículo | Criar reserva padrão de cinco dias úteis, controlar vencimento e registrar extensão autorizada pela gerência. | Must Have | `planned` | — |
| F09 | Proposta e encerramento operacional | Registrar proposta, pendências, decisão conhecida e encerramento da jornada sem afirmar conclusão integral da venda. | Must Have | `planned` | — |
| F10 | Perfil ou portal do comprador | Permitir que o comprador acompanhe ou altere diretamente sua jornada. | Won't Have | `out-of-scope` | — |

**Prioridades (MoSCoW):** `Must Have` · `Should Have` · `Could Have` · `Won't Have`  
**Status possíveis:** `planned` · `prd-ready` · `in-progress` · `done` · `out-of-scope`

## 5. Dependências (Domain Dependencies)

### Depende de (Upstream)

| Domínio | O que consome | Tipo | Criticidade |
|---|---|---|---|
| Interesse e Atendimento (D03) | Interesse qualificado, contexto do comprador e veículo de referência. O recebimento desse contexto não abre a jornada automaticamente; o vendedor deve formalizá-la. | Contexto de oportunidade | Alta |
| Estoque Curado e Disponibilidade (D02) | Fatos do veículo, preço oficial e disponibilidade operacional atual. | Dados e eventos | Alta |

### Fornece para (Downstream)

| Domínio | O que fornece | Tipo | Criticidade |
|---|---|---|---|
| Estoque Curado e Disponibilidade (D02) | Solicitação de reserva e fatos de criação, extensão ou expiração da reserva para que D02 mantenha sua disponibilidade coerente. | Evento de negócio | Alta |
| Interesse e Atendimento (D03) | Estado da jornada e próximo passo para que o atendimento possa continuar ou ser encerrado. | Contexto de continuidade | Média |

D02 continua sendo a autoridade sobre a disponibilidade. A interação de reserva é um retorno operacional para sincronizar a decisão, não uma transferência de propriedade do conceito.

### Integrações Externas (External Integrations)

| Sistema Externo | Finalidade | Direção | Status |
|---|---|---|---|
| Parceiros financeiros | Não há integração obrigatória; solicitações, condições e resultados são registrados manualmente pela Operação central. | — | `out-of-scope` |

## 6. Regras de Negócio (Business Rules)

| ID | Regra | Origem |
|---|---|---|
| RN-01 | Somente o vendedor da Operação central pode formalizar o interesse qualificado e abrir uma Jornada de compra assistida. | Decisão de negócio |
| RN-02 | Antes da análise de crédito, o dossiê deve conter, no mínimo, nome completo, CPF, endereço, renda e os demais campos obrigatórios definidos pela operação. | Processo de crédito |
| RN-03 | Documentos e comprovantes podem ser anexados em formato de imagem ou PDF e devem permanecer vinculados ao dossiê correspondente. | Decisão de negócio |
| RN-04 | A forma de compra deve ser exatamente `à vista` ou `financiada`. | Decisão de negócio |
| RN-05 | A entrada de veículo usado não faz parte desta versão do domínio. | Escopo confirmado |
| RN-06 | Financiamento e análise de crédito são registrados manualmente; D04 não promete aprovação nem depende de decisão automática ou integração externa. | Escopo confirmado |
| RN-07 | D04 não altera fatos do veículo, preço oficial ou disponibilidade operacional mantidos por D02. | Fronteira D02/D04 |
| RN-08 | A reserva de compra tem duração padrão de cinco dias úteis a partir de sua confirmação. | Política de reserva |
| RN-09 | A extensão da reserva exige autorização gerencial registrada. | Política de reserva |
| RN-10 | A reserva deve ser sincronizada com D02 e não representa pagamento, contrato, aprovação de crédito ou conclusão da compra. | Fronteira D02/D04 |
| RN-11 | O comprador não precisa de perfil autenticado na primeira versão; o vendedor registra os dados fornecidos pelo comprador. | Escopo confirmado |
| RN-12 | Dados pessoais e documentos só podem ser tratados por pessoas autorizadas e conforme os requisitos de privacidade definidos para a operação. | Restrição global / LGPD |

## 7. Eventos do Domínio (Domain Events)

### Produz (Publishes)

- `compra.jornada-iniciada` — o vendedor abriu uma jornada a partir de um interesse qualificado.
- `compra.cliente-formalizado` — o comprador foi formalizado como cliente da jornada.
- `compra.dados-atualizados` — dados pessoais ou financeiros da jornada foram alterados.
- `compra.documento-anexado` — um documento ou comprovante foi associado ao dossiê.
- `compra.forma-definida` — a jornada foi marcada como à vista ou financiada.
- `financiamento.solicitacao-registrada` — a operação registrou uma solicitação manual de análise.
- `financiamento.analise-atualizada` — o andamento ou resultado conhecido da análise mudou.
- `financiamento.condicao-apresentada` — uma condição manual foi apresentada ao comprador.
- `compra.reserva-solicitada` — D04 solicitou a reserva do veículo a D02.
- `compra.reserva-confirmada` — a reserva foi confirmada após a sincronização operacional.
- `compra.reserva-estendida` — uma gerência autorizou a extensão da reserva.
- `compra.reserva-expirada` — a reserva venceu sem extensão válida.
- `compra.proposta-registrada` — uma proposta ou condição assistida foi registrada.
- `compra.jornada-atualizada` — estado, pendência ou próximo passo da jornada mudou.
- `compra.jornada-encerrada` — a jornada foi encerrada operacionalmente, sem afirmar conclusão integral da venda.

### Consome (Subscribes)

- `interesse.qualificado` (de: D03) — fornece o contexto inicial para o vendedor decidir pela abertura da jornada.
- `estoque.oferta-atualizada` (de: D02) — informa alterações relevantes nos fatos ou no preço oficial do veículo.
- `estoque.disponibilidade-alterada` (de: D02) — informa a situação operacional do veículo e pode confirmar ou impedir a reserva.

## 8. Estratégia de Desenvolvimento (Development Strategy)

### Ordem de Implementação Sugerida

1. **F01** — Definir a entrada a partir de D03, a referência ao veículo de D02 e as permissões do vendedor.
2. **F02** — Formalizar o cliente e registrar os dados mínimos da jornada.
3. **F03** — Incluir documentos, comprovantes, pendências e histórico de atualização.
4. **F04** e **F05** — Definir a forma de compra e o ciclo operacional da jornada.
5. **F06** e **F07** — Registrar condições, solicitações e resultados manuais de financiamento.
6. **F08** — Implementar reserva, vencimento, extensão gerencial e sincronização com D02.
7. **F09** — Registrar proposta, próximos passos e encerramento operacional.

### Riscos do Domínio

| Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|
| Dados pessoais, CPF e documentos são tratados sem regras suficientes de consentimento, acesso ou retenção. | Alta | Alto | Definir requisitos de privacidade, permissões, auditoria e retenção antes do PRD de F02/F03. |
| O processo manual de crédito gera estados ou condições inconsistentes. | Alta | Alto | Padronizar estados, responsáveis, campos obrigatórios e histórico de alterações. |
| A reserva expira ou é estendida sem refletir a disponibilidade de D02. | Média | Alto | Definir confirmação, expiração e eventos de sincronização entre D04 e D02. |
| A jornada cresce para um e-commerce completo. | Média | Alto | Manter pagamento, contrato, transferência e entrega fora do escopo da primeira versão. |
| O limite entre vendedor, D02 e D03 permanece ambíguo. | Média | Alto | Confirmar a nomenclatura da Operação central e preservar D03 como origem do interesse qualificado. |

## 9. Questões em Aberto (Open Questions)

- [ ] Quais campos e documentos são obrigatórios para compra à vista e quais são obrigatórios para compra financiada?
- [ ] Quais estados representam a jornada, a solicitação de crédito e a proposta?
- [ ] Quem realiza a análise manual e qual resultado pode ser registrado: aprovado, recusado, pendente ou outro?
- [ ] Pode existir mais de uma reserva ativa para o mesmo veículo? Como funcionam cancelamento e liberação antecipada?
- [ ] Como a operação obtém o consentimento do comprador para tratar CPF, renda e documentos, e por quanto tempo retém esses dados?
- [ ] A proposta é apenas indicativa ou possui validade e aceite formal?
- [ ] O comprador apenas fornece informações ao vendedor ou precisa revisar e confirmar os dados por algum canal externo?
- [ ] A expressão “vendedor que atua no D2” significa um papel dentro de D02 ou apenas um vendedor da Operação central que consulta D02?

---

*Domain Doc gerado com a skill `tsg-flow-domain-creator`. Para criar PRDs das features deste domínio, use a skill `tsg-flow-prd-creator` fornecendo `docs/vision.md` e este arquivo como contexto.*
