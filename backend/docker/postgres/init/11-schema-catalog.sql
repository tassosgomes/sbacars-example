-- Schema do serviço catalog (D01 — Catálogo e Descoberta). Ver §4.1 do plano.
CREATE SCHEMA catalog AUTHORIZATION own_catalog;

-- svc_catalog: só DML no próprio schema, nunca DDL.
GRANT USAGE ON SCHEMA catalog TO svc_catalog;
ALTER DEFAULT PRIVILEGES FOR ROLE own_catalog IN SCHEMA catalog
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO svc_catalog;
ALTER DEFAULT PRIVILEGES FOR ROLE own_catalog IN SCHEMA catalog
  GRANT USAGE, SELECT ON SEQUENCES TO svc_catalog;

-- Nenhum GRANT de "catalog" para svc_inventory, svc_interest ou svc_purchase: a fronteira é
-- física, imposta pelo banco. A ausência de um GRANT aqui é o ponto — não um esquecimento.
