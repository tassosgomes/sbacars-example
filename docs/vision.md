# Vision Document — Plataforma de Venda de Carros

> **Nível 0 da hierarquia de documentação.** Este documento é a âncora de contexto para todos os Domain Docs, PRDs, Tech Specs e Tasks do projeto. Sempre que iniciar uma nova sessão com a IA, forneça este arquivo como contexto.

---

## 1. Visão Geral do Sistema (System Overview)

### Problema de Negócio

Pessoas que procuram carros seminovos e usados no Brasil precisam comparar ofertas com informações que podem estar incompletas, inconsistentes ou difíceis de interpretar. A incerteza sobre condição, histórico disponível, preço, disponibilidade e adequação ao uso aumenta a insegurança da compra e torna a jornada dependente de contatos manuais e canais fragmentados.

Para a operação que oferece os veículos, essa falta de clareza dificulta a construção de confiança, a organização do catálogo e a continuidade do atendimento aos interessados. Se o problema não for tratado, a plataforma poderá gerar tráfego sem gerar entendimento suficiente para que o comprador avance.

### Solução Proposta

A Plataforma de Venda de Carros será uma loja digital de catálogo curado, com alcance nacional e operação central. Ela ajudará o comprador final a descobrir veículos, compreender as informações disponíveis sobre cada oferta e demonstrar interesse para que a operação dê continuidade ao atendimento ou organize um test drive.

A proposta prioriza confiança por transparência: a plataforma deve apresentar de forma clara a origem, a condição conhecida, o histórico disponível, o preço e a disponibilidade do veículo, sem pressupor certificação formal no primeiro recorte.

O produto terá caráter didático, mas será orientado por uma visão de operação real e evolutiva. As experiências da [Localiza Seminovos](https://seminovos.localiza.com/) e da [Webmotors](https://www.webmotors.com.br/) serão usadas como referências de mercado para descoberta e jornada automotiva, sem reproduzir identidade, conteúdo ou dados proprietários.

### Público-Alvo (Target Audience)

| Perfil (Role) | Descrição | Necessidade Principal |
|---|---|---|
| Comprador final | Pessoa que pesquisa um carro para uso pessoal, familiar ou profissional. | Encontrar opções adequadas, entender a oferta e avançar com segurança para o contato. |
| Operação central | Equipe responsável por manter o catálogo curado e dar continuidade aos interessados. | Controlar a qualidade das informações, a disponibilidade e o atendimento recebido. |
| Product Owner / decisor | Solicitante responsável por validar prioridades, escopo e evolução do produto. | Manter uma visão coerente entre o objetivo didático e a possibilidade de operação real. |

### Contexto de Entrada

- [ ] Discovery com cliente
- [ ] Modernização de sistema legado
- [x] Ideia nova (greenfield)

Não foi identificado um sistema legado a substituir ou preservar. O produto será iniciado como uma iniciativa nova, com catálogo inicialmente curado ou simulado.

---

## 2. Domínios Identificados (Domain Map)

> Um domínio é um conjunto coeso de responsabilidades de negócio com fronteiras bem definidas (bounded context).

| # | Domínio (Domain) | Responsabilidade Principal | Status | Domain Doc |
|---|---|---|---|---|
| D01 | Catálogo e Descoberta | Organizar e apresentar a oferta curada para que o comprador encontre e compreenda os veículos disponíveis. | `done` | `domains/catalogo-descoberta/domain.md` |
| D02 | Estoque Curado e Disponibilidade | Representar a oferta controlada pela operação central, incluindo informações de condição e disponibilidade conhecidas. | `done` | `domains/estoque-curado/domain.md` |
| D03 | Interesse e Atendimento | Receber manifestações de interesse e apoiar a continuidade do contato, incluindo a possibilidade de test drive. | `done` | `domains/interesse-atendimento/domain.md` |
| D04 | Compra Assistida e Financiamento | Evoluir o interesse qualificado para uma jornada de compra assistida e opções de financiamento. | `planned` | `domains/compra-assistida/domain.md` |

**Status possíveis:** `planned` · `in-progress` · `done` · `out-of-scope`

D01–D03 possuem Domain Documents iniciais em `domains/` e mapa em `context/domain-map.md`. As fronteiras continuam sujeitas a refinamento conforme PRDs e validação de negócio.

---

## 3. Mapa de Interdependências (Dependency Map)

> Quais domínios dependem de quais. Use para identificar a ordem de desenvolvimento e riscos de acoplamento.

```text
D02 Estoque Curado e Disponibilidade ──fornece a oferta para──→ D01 Catálogo e Descoberta
D03 Interesse e Atendimento          ──nasce da jornada de────→ D01 Catálogo e Descoberta
D04 Compra Assistida e Financiamento ──depende do contexto de─→ D02 + D03
```

| Domínio Origem | Depende de | Tipo de Dependência | Risco |
|---|---|---|---|
| D01 Catálogo e Descoberta | D02 Estoque Curado e Disponibilidade | Informação de oferta e disponibilidade | Médio |
| D03 Interesse e Atendimento | D01 Catálogo e Descoberta | Continuidade da jornada do comprador | Médio |
| D04 Compra Assistida e Financiamento | D02 Estoque Curado e Disponibilidade e D03 Interesse e Atendimento | Contexto do veículo e oportunidade qualificada | Alto |

---

## 4. Roadmap Macro (High-Level Roadmap)

> Fases de entrega do sistema inteiro. Cada fase deve ser entregável e testável de forma independente.

### Fase 1 — Descoberta e Interesse Qualificado (MVP / Foundation)

**Objetivo:** Validar a jornada principal do comprador desde a descoberta de um veículo até a demonstração de interesse, com informações transparentes e continuidade possível pela operação central.

**Domínios incluídos:** D01, D02, D03

**Critério de conclusão:** Uma pessoa consegue encontrar e compreender uma oferta do catálogo nacional curado e demonstrar interesse; a operação central consegue receber esse interesse e dar continuidade ao atendimento ou ao agendamento de test drive.

### Fase 2 — Compra Assistida e Financiamento

**Objetivo:** Aprofundar a conversão de interesse qualificado para uma jornada de compra assistida, com opções de financiamento compreensíveis e suporte da operação central.

**Domínios incluídos:** D01, D02, D03, D04

**Critério de conclusão:** Um interesse qualificado pode avançar por um processo assistido de compra, com condições apresentadas de forma clara e responsabilidades da operação explicitamente definidas.

### Fase 3 — Não aplicável neste momento

**Objetivo:** Não definido nesta visão. Expansões como marketplace controlado, pós-venda ou novos canais dependem da validação das fases anteriores.

**Domínios incluídos:** Não aplicável neste momento.

**Critério de conclusão:** Não aplicável neste momento.

---

## 5. Restrições Globais (Global Constraints)

> Restrições que se aplicam a **todo** o sistema, não a um domínio específico.

### Restrições Técnicas (Technical Constraints)

- **Stack obrigatória:** Não aplicável neste momento; a escolha será feita nas etapas técnicas posteriores.
- **Integrações obrigatórias:** Não há integração obrigatória para a primeira fase. O catálogo inicial será curado ou simulado.
- **Infraestrutura:** Não aplicável neste momento; a orientação de prontidão para produção é um objetivo de qualidade, não uma decisão de infraestrutura.
- **Autenticação:** Não aplicável neste momento; perfis, acesso e responsabilidades serão definidos na descoberta dos domínios.

### Restrições de Negócio (Business Constraints)

- **Prazo:** Não há prazo rígido conhecido.
- **Orçamento:** Não há restrição orçamentária informada.
- **Regulatório:** A operação real deverá considerar a legislação brasileira aplicável à privacidade, proteção de dados e relação de consumo. Os requisitos específicos permanecem abertos.
- **Dados legados:** Não aplicável; não foi identificado legado a migrar.
- **Abrangência:** O catálogo deve ser pensado para alcance nacional, mas a operação inicial poderá ser assistida e progressiva.
- **Governança:** O solicitante atua como Product Owner e ponto focal para validar prioridades e escopo.

### Non-Goals do Sistema

O sistema não terá, no primeiro recorte:

- marketplace aberto para anúncios livres de particulares ou lojas;
- conclusão integral da compra, pagamento ou documentação exclusivamente online;
- dependência de estoque real ou integração comercial para validar a primeira jornada;
- motos ou outras categorias de veículos fora do foco em carros;
- certificação formal de histórico ou condição como premissa obrigatória da primeira fase;
- substituição de um sistema legado, pois nenhum legado foi identificado.

---

## 6. Glossário de Negócio (Business Glossary)

> Termos do domínio de negócio com definição acordada. Essencial para manter consistência entre domínios e sessões com a IA.

| Termo | Definição | Domínio(s) |
|---|---|---|
| Carro seminovo | Carro usado que compõe a oferta da loja e é apresentado para uma possível compra. | Catálogo e Descoberta, Estoque Curado |
| Catálogo curado | Conjunto de veículos selecionados e mantidos sob responsabilidade da operação central. | Catálogo e Descoberta, Estoque Curado |
| Operação central | Equipe que controla a oferta, mantém as informações e dá continuidade aos interessados. | Estoque Curado, Interesse e Atendimento |
| Transparência das informações | Apresentação clara da origem, condição conhecida, histórico disponível, preço e disponibilidade, sem ocultar limitações de informação. | Catálogo e Descoberta, Estoque Curado |
| Interesse qualificado | Manifestação de interesse que contém contexto suficiente para a operação prosseguir com o atendimento. | Interesse e Atendimento |
| Compra assistida | Jornada em que a operação apoia o comprador na evolução do interesse até a compra, sem exigir conclusão integralmente autônoma. | Compra Assistida e Financiamento |

---

## 7. Premissas e Riscos Globais (Assumptions & Risks)

### Premissas (Assumptions)

- O solicitante permanecerá disponível como Product Owner para validações de direção e escopo.
- Um catálogo curado ou simulado estará disponível para validar a primeira jornada.
- O alcance nacional será tratado inicialmente como uma capacidade de oferta e comunicação; atendimento e operação poderão ser assistidos.
- A validação inicial será considerada bem-sucedida quando a jornada ponta a ponta for coerente, não quando houver um volume de vendas definido.
- Transparência das informações é um diferencial viável antes da adoção de certificação formal.
- O produto será construído com intenção de evolução para uma operação real, mas decisões técnicas e operacionais ainda não estão fechadas.

### Dependências Globais (Global Dependencies)

- Definição posterior da operação responsável por atendimento, disponibilidade e eventual test drive.
- Validação da origem, qualidade e atualização dos dados do catálogo antes de uma operação comercial real.
- Definição dos requisitos legais e de privacidade aplicáveis ao mercado brasileiro.
- Para a Fase 2, definição de parceiros, condições e responsabilidades relacionadas a financiamento.

### Riscos Globais (Global Risks)

| Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|
| Informações incompletas ou desatualizadas reduzem a confiança do comprador. | Alta | Alto | Estabelecer critérios de qualidade e responsabilidade sobre o catálogo antes de ampliar a operação. |
| O escopo didático crescer para uma plataforma de comércio eletrônico completo antes da validação da primeira jornada. | Média | Alto | Usar este Vision Document como contrato de escopo e manter compra assistida como evolução posterior. |
| A promessa de alcance nacional superar a capacidade da operação assistida. | Média | Alto | Validar progressivamente cobertura, atendimento e responsabilidades antes de assumir operação nacional completa. |
| Transparência sem verificação formal não ser suficiente para sustentar a confiança. | Média | Médio | Validar a percepção dos compradores e decidir posteriormente se mecanismos formais de verificação são necessários. |
| Financiamento depender de decisões ou parceiros ainda não definidos. | Média | Alto | Manter financiamento na Fase 2 e esclarecer dependências antes de comprometer a expansão. |

### Pontos em Aberto (Open Points)

- Nome comercial e identidade da plataforma.
- Modelo operacional detalhado para atendimento, test drive, entrega e fechamento.
- Critérios formais para qualidade, atualização e eventual verificação das informações dos veículos.
- Requisitos legais e de privacidade necessários antes de uma operação real.
- Escopo, parceiros e responsabilidades da compra assistida e do financiamento.
- Eventual priorização de marketplace controlado, pós-venda ou novos canais após a validação da Fase 1.

---

## 8. Histórico de Revisões (Revision History)

| Versão | Data | Autor | Alterações |
|---|---|---|---|
| 0.1 | 2026-08-15 | Solicitante + Codex | Versão inicial da visão estratégica da plataforma. |

---

## Próximos Artefatos Recomendados

Com base nesta visão, os artefatos de domínio da Fase 1 estão disponíveis. A próxima etapa é o PRD da Fase 1.

- ~~Domain Landscape~~ → `context/domain-map.md`;
- ~~Domain Documents (D01–D03)~~ → `domains/*/domain.md`;
- **PRD da Fase 1** — Descoberta e Interesse Qualificado;
- TechSpec somente após a aprovação dos PRDs correspondentes.

Não criar Domain Documents, PRDs, TechSpecs ou Tasks automaticamente como parte deste Vision Document.

---

*Vision Doc gerado com a skill `tsg-flow-vision-creator`.*
