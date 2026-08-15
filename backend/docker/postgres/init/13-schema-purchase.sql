-- Schema do serviço purchase (D04 — Compra Assistida e Financiamento, Fase 2). Ver §4.1 do
-- plano. O serviço nasce com schema e migração vazia; nenhuma entidade de negócio ainda.
CREATE SCHEMA purchase AUTHORIZATION own_purchase;

-- svc_purchase: só DML no próprio schema, nunca DDL.
GRANT USAGE ON SCHEMA purchase TO svc_purchase;
ALTER DEFAULT PRIVILEGES FOR ROLE own_purchase IN SCHEMA purchase
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO svc_purchase;
ALTER DEFAULT PRIVILEGES FOR ROLE own_purchase IN SCHEMA purchase
  GRANT USAGE, SELECT ON SEQUENCES TO svc_purchase;

-- Nenhum GRANT de "purchase" para svc_inventory, svc_catalog ou svc_interest: a fronteira é
-- física, imposta pelo banco. A ausência de um GRANT aqui é o ponto — não um esquecimento.
