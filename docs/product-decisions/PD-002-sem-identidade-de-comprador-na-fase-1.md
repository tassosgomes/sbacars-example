# PD-002: Sem identidade de comprador na Fase 1

- **Status**: Accepted
- **Escopo**: Global
- **Data**: 2026-08-16
- **Responsável pela decisão**: Product Owner
- **Origem**: Sessão de discovery do PRD `catalogo-publico-e-descoberta` (D01, F06)
- **Tags**: identidade, autenticacao, favoritos, lgpd, fase-1
- **Substitui**: Não aplicável
- **Substituído por**: Não aplicável

## Contexto

O Vision Doc registra "Autenticação: não aplicável neste momento", enquanto a RN-07 do Domain Doc de D01 prevê dois modos de favorito: anônimo por navegador e persistente após cadastro. A ambiguidade precisava ser resolvida antes de especificar favoritos e comparação.

Criar conta de comprador na Fase 1 traria identidade, sessão, recuperação de acesso, política de retenção e obrigações de LGPD — um bloco de escopo que o Vision não autoriza e cuja necessidade ainda não foi demonstrada.

## Decisão

A plataforma não terá cadastro, login nem área logada de comprador na Fase 1. Favoritos e comparação persistem apenas no **navegador identificado**, sem constituir identidade de pessoa.

A RN-07 é cumprida no seu ramo anônimo. O ramo cadastrado — favoritos persistentes e união dos favoritos anônimos no primeiro acesso — fica condicionado à definição dos requisitos de LGPD e ao resultado medido da Fase 2 de engajamento.

## Alternativas consideradas

- **Cadastro de comprador no MVP** — cumpriria RN-07 integralmente, mas adiciona identidade, retenção e LGPD que o Vision deixou explicitamente em aberto, e o valor da persistência ainda não foi demonstrado.
- **Identidade emergindo no momento do interesse** — o contato informado em F08 passaria a persistir os favoritos daquele navegador. Cumpriria RN-07 sem área logada, mas acopla conversão a persistência e contraria [PD-001](PD-001-dado-pessoal-do-comprador-pertence-a-d03.md), que mantém dado pessoal fora de D01.

## Consequências

- **Positivas**: mantém D01 inteiramente público e sem superfície de autenticação; evita decisões de LGPD ainda não fundamentadas; reduz o escopo do MVP à jornada que o Vision quer validar.
- **Negativas ou riscos**: favoritos se perdem com limpeza de dados ou troca de dispositivo — risco já previsto no Domain Doc de D01, mitigado por comunicar a limitação ao comprador; a plataforma não reconhece um comprador recorrente.
- **Impacto em futuros PRDs**: nenhum PRD da Fase 1 pode pressupor comprador autenticado. O PRD de D03 não pode assumir identidade prévia ao interesse. A introdução de conta de comprador exige um novo PD que substitua este.

## Termos e documentos afetados

- **Termos canônicos**: "Navegador identificado" (definido no PRD `catalogo-publico-e-descoberta`).
- **Vision/Domain Docs**: resolve a ambiguidade entre `docs/vision.md` (Restrições Técnicas — Autenticação) e a RN-07 de `domains/catalogo-descoberta/domain.md`. Recomenda-se anotar a resolução na RN-07 quando o Domain Doc for revisado.
- **PRDs relacionados**: `tasks/prd-catalogo-publico-e-descoberta/prd.md` (RF-06, RF-07, DP-02).

## Histórico

- 2026-08-16 — Criado como `Proposed` durante o discovery do PRD de catálogo público.
- 2026-08-16 — Marcado como `Accepted` com a aprovação do PRD `catalogo-publico-e-descoberta`.
