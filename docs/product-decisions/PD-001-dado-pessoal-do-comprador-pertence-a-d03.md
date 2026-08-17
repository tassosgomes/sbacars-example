# PD-001: Dado pessoal do comprador pertence a D03

- **Status**: Accepted
- **Escopo**: Global
- **Data**: 2026-08-16
- **Responsável pela decisão**: Product Owner
- **Origem**: Sessão de discovery do PRD `catalogo-publico-e-descoberta` (D01, F08)
- **Tags**: dado-pessoal, lgpd, fronteira-d01-d03, interesse
- **Substitui**: Não aplicável
- **Substituído por**: Não aplicável

## Contexto

A manifestação de interesse nasce visualmente dentro do catálogo público (D01), mas o domínio responsável por receber, qualificar e dar continuidade ao interesse é D03 — Interesse e Atendimento, dono da entidade "Contexto do comprador". Como a ação parte da página do veículo, havia a tentação de fazer D01 coletar nome, contato e mensagem e apenas repassá-los.

Isso colocaria dado pessoal, retenção e obrigações de LGPD dentro de um domínio cuja responsabilidade é apresentação. O Vision Doc ainda não define os requisitos legais aplicáveis, o que torna a escolha materialmente arriscada.

## Decisão

D01 oferece a ação de manifestar interesse e entrega o **contexto da descoberta** — item, preço e status exibidos naquele momento, e origem da navegação. A coleta, a exibição e a retenção de qualquer dado pessoal do comprador pertencem a D03.

D01 não coleta, não exibe e não retém nome, contato, mensagem ou qualquer identificador de pessoa. A ação encaminha contexto, não pessoas.

## Alternativas consideradas

- **D01 coletar contato e encaminhar** — jornada mais curta e inteiramente sob controle de D01, mas coloca dado pessoal e política de retenção em um domínio de apresentação, contrariando a fronteira estabelecida no `context/domain-map.md`.
- **Canal direto com a operação (telefone/WhatsApp)** — entrega imediata e barata, mas perde a rastreabilidade da descoberta e esvazia o evento `catalogo.interesse-solicitado` que D03 espera consumir.

## Consequências

- **Positivas**: mantém a fronteira D01/D03 do domain map; concentra a superfície de LGPD em um único domínio; permite que D01 permaneça público e sem autenticação.
- **Negativas ou riscos**: a jornada percebida como única é entregue por dois domínios, exigindo coordenação de experiência entre D01 e D03; D03 precisa estar pronto para captar antes que a conversão possa ser medida ponta a ponta.
- **Impacto em futuros PRDs**: o PRD de D03 é responsável pela captação, pelo consentimento e pela retenção do dado pessoal. O PRD-B de D01 (F04, F09) herda a mesma restrição. D04 recebe dado pessoal por meio de D03, nunca de D01.

## Termos e documentos afetados

- **Termos canônicos**: "Contexto da descoberta" (definido no PRD `catalogo-publico-e-descoberta`); "Contexto do comprador" (D03, inalterado).
- **Vision/Domain Docs**: compatível com `domains/catalogo-descoberta/domain.md` (RN-11) e com `context/domain-map.md`. Nenhuma alteração exigida.
- **PRDs relacionados**: `tasks/prd-catalogo-publico-e-descoberta/prd.md` (RF-05, DP-05).

## Histórico

- 2026-08-16 — Criado como `Proposed` durante o discovery do PRD de catálogo público.
- 2026-08-16 — Marcado como `Accepted` com a aprovação do PRD `catalogo-publico-e-descoberta`.
