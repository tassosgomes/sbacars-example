# Domain Document — Estoque Curado e Disponibilidade

> **Nível 1 da hierarquia de documentação.** Este documento detalha o bounded context de um domínio específico do sistema. Sempre forneça o `vision.md` junto com este arquivo ao iniciar sessões de PRD ou Tech Spec dentro deste domínio.

**Domínio:** Estoque Curado e Disponibilidade (D02)  
**Responsável:** Operação central  
**Status:** `done`  
**Fase do Roadmap:** Fase 1 — Descoberta e Interesse Qualificado (MVP / Foundation)  
**Última revisão:** 2026-08-15

---

## 1. Propósito do Domínio (Domain Purpose)

### Responsabilidade Principal

Controlar a oferta de carros sob responsabilidade da operação central, incluindo os fatos conhecidos, o preço oficial e a disponibilidade operacional de cada veículo.

### Problema que Resolve

Sem uma referência clara sobre quais veículos a operação controla e o que sabe sobre cada um, o catálogo público e o atendimento podem apresentar informações inconsistentes. Este domínio concentra a verdade operacional da oferta curada, suas limitações declaradas e sua situação para a jornada do comprador.

O domínio pode trabalhar inicialmente com uma oferta real, curada ou simulada. O uso de dados simulados é uma estratégia de validação da Fase 1, não uma feature de negócio independente.

### Fora do Escopo deste Domínio (Out of Scope)

- Buscar, comparar, favoritar ou apresentar publicamente os veículos → **Catálogo e Descoberta (D01)**.
- Definir a experiência de descoberta ou criar uma segunda fonte de verdade para a oferta.
- Receber manifestações, qualificar interesses, manter contato ou organizar a agenda de test drive → **Interesse e Atendimento (D03)**.
- Conduzir o contato com o futuro cliente durante o test drive; D03 é responsável por essa continuidade.
- Evoluir um interesse qualificado para compra assistida ou financiamento → **Compra Assistida e Financiamento (D04)**.
- Concluir pagamento, documentação ou compra integralmente online.
- Marketplace aberto para anúncios de particulares ou lojas.
- Certificação formal de condição ou histórico como requisito da Fase 1.
- Integração comercial externa obrigatória para validar a primeira jornada.

---

## 2. Usuários do Domínio (Domain Users)

| Perfil (Role) | O que faz neste domínio | Frequência de uso |
|---|---|---|
| Operação central | Inclui, atualiza e retira veículos da oferta; mantém fatos conhecidos, preço oficial e disponibilidade. | Alta |
| Comprador final | Não administra o domínio; consome informações por meio de D01 e D03. | Indireta, durante a pesquisa e o atendimento |
| Product Owner / decisor | Valida critérios de qualidade, transparência, curadoria e evolução da oferta. | Eventual |

---

## 3. Entidades Principais (Core Entities)

> Entidades são os objetos de negócio centrais deste domínio. Não são schemas de banco de dados.

| Entidade | Descrição | Atributos Principais | Relacionamentos |
|---|---|---|---|
| Veículo | Carro seminovo ou usado que pode compor a oferta da operação. | identificação, marca, modelo, ano, características básicas | pode compor: Oferta curada |
| Oferta curada | Decisão de manter um veículo sob responsabilidade da operação para possível apresentação. | situação da oferta, data de inclusão, responsável | referencia: Veículo; alimenta: D01 |
| Estoque curado | Conjunto de veículos selecionados e mantidos pela operação central. | alcance, critérios de curadoria, composição | agrupa: Ofertas curadas |
| Origem conhecida | Informação disponível sobre a procedência do veículo. | fonte, evidências, limitações | compõe: Fatos conhecidos |
| Condição conhecida | Fatos disponíveis sobre a condição do veículo, sem equivaler a certificação formal. | observações, evidências, lacunas | compõe: Fatos conhecidos |
| Histórico disponível | Informações de histórico que a operação conseguiu reunir e declarar. | eventos conhecidos, fonte, lacunas | compõe: Fatos conhecidos |
| Preço oficial | Valor declarado pela operação para a oferta curada. | valor, moeda, vigência | pertence a: Oferta curada; é fornecido a D01 |
| Disponibilidade operacional | Situação do veículo para ser apresentado ou continuar uma jornada. | disponível, reservado, vendido | pertence a: Oferta curada; consultada por D03 |

---

## 4. Features Previstas (Planned Features)

| # | Feature | Descrição | Prioridade | Status | PRD |
|---|---|---|---|---|---|
| F01 | Cadastro e manutenção de veículos | Registrar e manter os dados básicos dos carros que podem compor a oferta. | Must Have | `planned` | — |
| F02 | Curadoria da oferta | Incluir, atualizar ou retirar veículos sob responsabilidade da operação central. | Must Have | `planned` | — |
| F03 | Gestão de fatos conhecidos | Manter origem, condição e histórico disponíveis, sempre com limitações explícitas. | Must Have | `planned` | — |
| F04 | Gestão do preço oficial | Definir e atualizar o preço oficial de cada oferta curada. | Must Have | `planned` | — |
| F05 | Gestão da disponibilidade operacional | Controlar os estados `disponível`, `reservado` e `vendido`. | Must Have | `planned` | — |
| F06 | Elegibilidade para publicação em D01 | Indicar quais ofertas atendem aos critérios mínimos para serem apresentadas no catálogo público. | Must Have | `planned` | — |

**Critérios mínimos de elegibilidade:** identificação do veículo, preço oficial, localização, dados básicos, condição ou histórico conhecidos quando disponíveis e limitações declaradas. A publicação e a experiência pública continuam sob responsabilidade de D01.

**Premissa do MVP:** a operação pode fornecer dados curados ou simulados para validar a jornada, sem depender de integração comercial externa.

**Prioridades (MoSCoW):** `Must Have` · `Should Have` · `Could Have` · `Won't Have`  
**Status possíveis:** `planned` · `prd-ready` · `in-progress` · `done` · `out-of-scope`

---

## 5. Dependências (Domain Dependencies)

### Depende de (Upstream)

| Domínio | O que consome | Tipo | Criticidade |
|---|---|---|---|
| — | Não há domínio upstream obrigatório na Fase 1; a operação central pode inserir ou simular os dados. | — | — |

### Fornece para (Downstream)

| Domínio | O que fornece | Tipo | Criticidade |
|---|---|---|---|
| Catálogo e Descoberta (D01) | Ofertas elegíveis, fatos conhecidos, preço oficial, localização e disponibilidade. | Dados e eventos | Alta |
| Interesse e Atendimento (D03) | Contexto operacional do veículo para atendimento e test drive. | Dados de leitura | Média |
| Compra Assistida e Financiamento (D04) | Fatos e disponibilidade necessários para a jornada assistida. | Dados de leitura | Alta na Fase 2 |

### Integrações Externas (External Integrations)

| Sistema Externo | Finalidade | Direção | Status |
|---|---|---|---|
| — | Nenhuma integração comercial é obrigatória na Fase 1; a oferta pode ser curada ou simulada. | — | `out-of-scope` |

---

## 6. Regras de Negócio (Business Rules)

| ID | Regra | Origem |
|---|---|---|
| RN-01 | Apenas carros seminovos ou usados compõem o estoque curado neste recorte. | Non-goals da visão |
| RN-02 | A operação central é responsável por incluir, manter, retirar e atualizar as informações da oferta. | Visão |
| RN-03 | Limitações de origem, condição ou histórico devem ser declaradas e não podem ser ocultadas. | Transparência das informações |
| RN-04 | A disponibilidade operacional usa, no mínimo, os estados `disponível`, `reservado` e `vendido`. | Decisão de domínio |
| RN-05 | A retirada da oferta é independente da disponibilidade: uma oferta retirada deixa de ser elegível para D01, sem criar um novo estado de disponibilidade. | Fronteira oferta/disponibilidade |
| RN-06 | O preço oficial é mantido por D02; D01 pode definir sua forma de apresentação, mas não altera o valor oficial da oferta. | Mapa de domínios |
| RN-07 | Só pode ser indicada como elegível para D01 uma oferta que possua os critérios mínimos definidos neste documento. | F06 |
| RN-08 | Solicitar ou agendar um test drive não altera automaticamente a disponibilidade; D03 é responsável pela agenda e pelo contato, e qualquer mudança de disponibilidade depende de decisão explícita da operação central. | Fronteira D02/D03 |
| RN-09 | Certificação formal de condição ou histórico não é obrigatória para a Fase 1. | Non-goals da visão |
| RN-10 | Dados curados ou simulados podem ser usados para validar a primeira jornada sem estoque real integrado. | Contexto de entrada |

---

## 7. Eventos do Domínio (Domain Events)

### Produz (Publishes)

- `estoque.oferta-incluida` — um veículo passou a compor a oferta curada.
- `estoque.oferta-atualizada` — fatos conhecidos, preço ou elegibilidade da oferta foram alterados.
- `estoque.oferta-retirada` — a oferta deixou de estar sob responsabilidade ou elegibilidade da operação.
- `estoque.disponibilidade-alterada` — a disponibilidade passou a outro estado operacional.

### Consome (Subscribes)

- Nenhum evento externo obrigatório na Fase 1.

---

## 8. Estratégia de Desenvolvimento (Development Strategy)

### Ordem de Implementação Sugerida

1. **F01** — Cadastro e manutenção dos veículos.
2. **F02** — Curadoria da oferta, incluindo inclusão e retirada.
3. **F03** — Registro dos fatos conhecidos e de suas limitações.
4. **F04** — Definição e atualização do preço oficial.
5. **F05** — Estados de disponibilidade operacional.
6. **F06** — Critérios de elegibilidade e fornecimento do contexto a D01.

Dados simulados podem ser usados desde o início para validar essa sequência; não constituem uma etapa funcional separada.

### Riscos do Domínio

| Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|
| Informações incompletas ou desatualizadas reduzem a confiança do comprador. | Alta | Alto | Definir critérios de qualidade, atualização e responsabilidade por campo antes de ampliar a operação. |
| Oferta retirada e disponibilidade operacional serem tratadas como o mesmo conceito. | Média | Alto | Manter ciclos separados: D02 controla ambos, mas retirada não é estado de disponibilidade. |
| Test drive criar compromisso operacional não refletido na oferta. | Média | Alto | D03 controla agenda e contato; D02 só altera disponibilidade mediante decisão explícita da operação. |
| A promessa de alcance nacional superar a capacidade de atendimento. | Média | Alto | Exibir localização e validar progressivamente a cobertura da operação central. |
| Pressão por integração comercial antes da validação do MVP. | Média | Médio | Usar dados curados ou simulados na Fase 1 e postergar integrações não obrigatórias. |

---

## 9. Questões em Aberto (Open Questions)

- [ ] Quais são os prazos de validade e os critérios formais de atualização para preço, condição, histórico e disponibilidade?
- [ ] Quem, dentro da operação central, pode atualizar cada tipo de informação e aprovar uma retirada?
- [ ] Qual é o fluxo operacional para transicionar uma oferta entre `disponível`, `reservado` e `vendido`?
- [ ] Que compromisso operacional um test drive agendado cria para D03, sem transferir a responsabilidade da agenda para D02?
- [ ] Quais requisitos legais e de privacidade devem ser atendidos antes de transformar a oferta simulada em operação comercial real?

---

*Domain Doc alinhado ao `docs/vision.md` e ao `context/domain-map.md`. Para criar PRDs das features deste domínio, use a skill `tsg-flow-prd-creator` fornecendo este arquivo e o `vision.md` como contexto.*
