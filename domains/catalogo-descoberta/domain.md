# Domain Document — Catálogo e Descoberta

> **Nível 1 da hierarquia de documentação.** Este documento detalha o bounded context de um domínio específico do sistema. Sempre forneça o `vision.md` junto com este arquivo ao iniciar sessões de PRD ou Tech Spec dentro deste domínio.

**Domínio:** Catálogo e Descoberta (D01)  
**Responsável:** a definir  
**Status:** `planned`  
**Fase do Roadmap:** Fase 1 — Descoberta e Interesse Qualificado (MVP / Foundation)  
**Última revisão:** 2026-08-15

---

## 1. Propósito do Domínio (Domain Purpose)

### Responsabilidade Principal

Transformar a oferta curada em uma experiência pública de venda, permitindo que o comprador encontre, compreenda, compare, favorite e manifeste interesse por veículos com informações transparentes.

O domínio é responsável pela apresentação e pela conversão inicial da oferta, mas não conclui a venda nem se torna dono dos fatos operacionais do veículo.

### Problema que Resolve

Compradores de carros seminovos e usados enfrentam ofertas fragmentadas, informações incompletas e dificuldade para comparar opções. Este domínio torna o catálogo nacional curado encontrável e compreensível, reduzindo a incerteza antes do contato com a operação central.

### Fora do Escopo deste Domínio (Out of Scope)

- Decidir quais veículos entram ou saem da oferta → **Estoque Curado e Disponibilidade (D02)**.
- Manter a fonte de verdade sobre condição, histórico, documentação operacional, disponibilidade e preço oficial → **Estoque Curado e Disponibilidade (D02)**.
- Qualificar manifestações, conduzir atendimento ou organizar a continuidade do contato → **Interesse e Atendimento (D03)**.
- Concluir compra, pagamento, documentação contratual ou financiamento → **Compra Assistida e Financiamento (D04)**.
- Marketplace aberto para anúncios de particulares ou lojas.
- Motos e categorias fora do foco em carros seminovos e usados.
- Certificação formal de condição ou histórico como premissa obrigatória da Fase 1.

## 2. Usuários do Domínio (Domain Users)

| Perfil (Role) | O que faz neste domínio | Frequência de uso |
|---|---|---|
| Comprador final | Busca, filtra, compara, favorita veículos, consulta a apresentação e inicia uma manifestação de interesse. | Alta durante sessões de pesquisa |
| Operação central | Mantém o conteúdo comercial, fotos, ficha técnica e materiais públicos autorizados para cada item. | Diária |
| Product Owner / decisor | Avalia a coerência da jornada de venda e prioriza evoluções da experiência de descoberta. | Eventual |

## 3. Entidades Principais (Core Entities)

> Entidades são os objetos de negócio centrais deste domínio. Não são schemas de banco de dados.

| Entidade | Descrição | Atributos Principais | Relacionamentos |
|---|---|---|---|
| Catálogo público | Conjunto de veículos elegíveis para descoberta pelo comprador. | alcance, critérios de exibição, ordenação | composto por: Itens do catálogo |
| Item do catálogo | Representação pública de um veículo da oferta curada. | dados exibidos, localização, status, limitações | origina de: Oferta curada (D02) |
| Conteúdo de apresentação | Parte comercial da oferta criada para vender e explicar o veículo. | título, descrição, destaques, textos, fotos de cliente | associado a: Item do catálogo |
| Ficha técnica | Informações técnicas opcionais apresentadas em formato chave/valor. | atributo, valor, unidade, origem conhecida | associada a: Item do catálogo |
| Preço apresentado | Valor oficial recebido de D02 e eventual preço promocional criado por D01. | preço oficial, preço promocional, vigência a definir | associado a: Item do catálogo |
| Favorito | Relação entre um comprador e um item salvo para consulta posterior. | item, navegador ou cadastro, data | referencia: Item do catálogo |
| Comparação | Seleção de veículos analisados conjuntamente pelo comprador. | 2 a 4 itens, atributos contrastados | agrupa: Itens do catálogo |
| Status público do item | Forma como disponibilidade, reserva ou venda é comunicada ao comprador. | disponível, reservado, vendido, aviso exibido | reflete: Disponibilidade de D02 |

## 4. Features Previstas (Planned Features)

| # | Feature | Descrição | Prioridade | Status | PRD |
|---|---|---|---|---|---|
| F01 | Publicação do catálogo e detalhe | Exibir itens elegíveis e permitir consultar a apresentação completa de cada veículo. | Must Have | `planned` | — |
| F02 | Busca, filtros, ordenação e localização | Encontrar veículos por marca, modelo, ano, preço, quilometragem, carroceria, combustível, câmbio e localização, entre outros critérios. | Must Have | `planned` | — |
| F03 | Apresentação transparente | Exibir fatos disponíveis, ficha técnica, materiais documentais e a indicação `Não informado` quando um dado não existir. | Must Have | `planned` | — |
| F04 | Gestão de conteúdo comercial e mídia | Manter textos de venda, destaques, fotos de cliente, fotos técnicas e seleção de materiais autorizados do estoque. | Must Have | `planned` | — |
| F05 | Status e preço do item | Mostrar preço oficial, localização e estados disponível, reservado ou vendido conforme D02. | Must Have | `planned` | — |
| F06 | Favoritos | Permitir favoritar sem cadastro, com persistência no navegador, ou manter favoritos persistentes após cadastro. | Must Have | `planned` | — |
| F07 | Comparação de veículos | Permitir selecionar de 2 a 4 veículos e comparar itens de série, valores e demais atributos disponíveis. | Must Have | `planned` | — |
| F08 | Início de interesse | Encaminhar para D03 o contexto do veículo e da descoberta quando o comprador manifestar interesse. | Must Have | `planned` | — |
| F09 | Preço promocional | Criar e apresentar uma condição promocional distinta do preço oficial de D02. | Should Have | `planned` | — |

**Prioridades (MoSCoW):** `Must Have` · `Should Have` · `Could Have` · `Won't Have`  
**Status possíveis:** `planned` · `prd-ready` · `in-progress` · `done` · `out-of-scope`

## 5. Dependências (Domain Dependencies)

### Depende de (Upstream)

| Domínio | O que consome | Tipo | Criticidade |
|---|---|---|---|
| Estoque Curado e Disponibilidade (D02) | Veículos elegíveis, fatos conhecidos, documentação disponível, dados técnicos, preço oficial, localização e disponibilidade. | Dados e eventos | Alta |

### Fornece para (Downstream)

| Domínio | O que fornece | Tipo | Criticidade |
|---|---|---|---|
| Interesse e Atendimento (D03) | Contexto do item, preço e status exibidos, origem da descoberta e veículo sobre o qual o comprador manifestou interesse. | Contexto de jornada | Alta |

### Integrações Externas (External Integrations)

| Sistema Externo | Finalidade | Direção | Status |
|---|---|---|---|
| — | Não há integração externa obrigatória na Fase 1; a oferta será curada ou simulada. Cookies, navegador e cadastro são mecanismos de persistência da experiência. | — | `out-of-scope` |

## 6. Regras de Negócio (Business Rules)

| ID | Regra | Origem |
|---|---|---|
| RN-01 | Somente veículos elegíveis fornecidos por D02 podem aparecer no catálogo público. | Fronteira D01/D02 |
| RN-02 | Quando uma informação não estiver disponível, a apresentação deve exibir `Não informado`; o domínio não deve inventar ou ocultar fatos relevantes. | Transparência das informações |
| RN-03 | D01 pode alterar conteúdo comercial, fotos de divulgação e materiais de apresentação, mas não altera condição, histórico, disponibilidade ou preço oficial mantidos por D02. | Fronteira D01/D02 |
| RN-04 | O preço promocional criado por D01 deve ser distinguível do preço oficial recebido de D02. | Política comercial do D01 |
| RN-05 | Uma comparação deve conter no mínimo 2 e no máximo 4 veículos. | Decisão de produto |
| RN-06 | Favoritar não cria lead nem `Interesse qualificado`; são ações distintas. | Decisão de produto |
| RN-07 | Sem cadastro, os favoritos ficam restritos ao navegador identificado; com cadastro, tornam-se persistentes conforme a identidade do comprador. | Decisão de produto |
| RN-08 | Veículos reservados continuam visíveis, podem ser favoritados, comparados e gerar interesse, mas devem informar que existe alguém à frente. | Política de disponibilidade exibida |
| RN-09 | Um veículo vendido pode permanecer identificado nos favoritos de quem o favoritou, exibindo somente o aviso de compra, sem fotos nem acesso ao detalhe. | Política de disponibilidade exibida |
| RN-10 | O comprador deve poder descobrir veículos por localização no alcance nacional e visualizar a localização apresentada. | Visão / abrangência nacional |
| RN-11 | Iniciar uma manifestação encaminha o contexto para D03, sem qualificar automaticamente o comprador. | Fronteira D01/D03 |
| RN-12 | D01 não pressupõe certificação formal de condição ou histórico na Fase 1. | Non-goals da visão |

## 7. Eventos do Domínio (Domain Events)

### Produz (Publishes)

- `catalogo.item-publicado` — um item elegível tornou-se descobrível no catálogo.
- `catalogo.item-atualizado` — conteúdo, apresentação ou informação exibida do item foi alterada.
- `catalogo.item-favoritado` — um comprador salvou um item para consulta posterior.
- `catalogo.comparacao-realizada` — o comprador comparou de 2 a 4 veículos.
- `catalogo.interesse-solicitado` — o comprador iniciou uma manifestação com contexto da descoberta.
- `catalogo.preco-promocional-publicado` — uma condição promocional passou a ser exibida.

### Consome (Subscribes)

- `estoque.oferta-incluida` (de: D02) — uma oferta passou a ser elegível para apresentação.
- `estoque.oferta-atualizada` (de: D02) — fatos conhecidos, preço oficial ou elegibilidade foram alterados.
- `estoque.disponibilidade-alterada` (de: D02) — o item tornou-se disponível, reservado ou vendido.
- `estoque.oferta-retirada` (de: D02) — a oferta deixou de ser apresentada no catálogo público.

## 8. Estratégia de Desenvolvimento (Development Strategy)

### Ordem de Implementação Sugerida

1. Definir o contrato de informação com D02 e o contexto entregue a D03.
2. Implementar item do catálogo, publicação, localização e detalhe básico.
3. Implementar conteúdo comercial, fotos, ficha técnica, documentação apresentada e transparência.
4. Implementar busca, filtros e ordenação nacional.
5. Implementar preço oficial, preço promocional e estados disponível, reservado e vendido.
6. Implementar favoritos anônimos por navegador e persistência com cadastro.
7. Implementar comparação de 2 a 4 veículos.
8. Implementar o início de interesse com encaminhamento para D03.

### Riscos do Domínio

| Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|
| Status ou preço desatualizado reduz a confiança do comprador. | Alta | Alto | Tratar D02 como fonte dos fatos e refletir suas alterações no catálogo. |
| Favorito anônimo é perdido com limpeza de cookies ou troca de navegador. | Alta | Médio | Explicar a limitação e incentivar cadastro para persistência. |
| Preço promocional entra em conflito com o preço oficial. | Média | Alto | Exibir os valores separadamente e definir aprovação e vigência antes do PRD de F09. |
| Fotos de cliente não possuem consentimento ou padrão de qualidade suficiente. | Média | Alto | Definir processo de autorização, seleção e retirada de mídia. |
| Ficha técnica ou documentação apresentada diverge dos fatos operacionais. | Média | Alto | Separar conteúdo comercial de fatos de D02 e validar a origem dos materiais. |
| Catálogo nacional promete cobertura maior que a capacidade de atendimento. | Média | Alto | Exibir localização e manter a operação assistida e progressiva conforme a visão. |

## 9. Questões em Aberto (Open Questions)

- [ ] Como unir favoritos anônimos ao cadastro e qual será o prazo de retenção no navegador?
- [ ] Quais são as regras de aprovação, vigência e encerramento de um preço promocional?
- [ ] Quem autoriza e mantém a ficha técnica e a documentação apresentada ao comprador?
- [ ] Quais estados adicionais de disponibilidade existem além de disponível, reservado e vendido?
- [ ] Qual processo garante consentimento e moderação das fotos de cliente?
- [ ] Qual granularidade de localização será exibida e filtrável: cidade, estado, região ou distância?

---

*Domain Doc alinhado ao `docs/vision.md` e ao `context/domain-map.md`. Para criar PRDs das features deste domínio, use a skill `tsg-flow-prd-creator` fornecendo este arquivo e o `vision.md` como contexto.*
