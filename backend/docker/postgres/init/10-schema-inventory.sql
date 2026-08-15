-- Schema do serviço inventory (D02 — Estoque Curado e Disponibilidade). Ver §4.1 do plano.
CREATE SCHEMA inventory AUTHORIZATION own_inventory;

-- svc_inventory: só DML no próprio schema, nunca DDL.
GRANT USAGE ON SCHEMA inventory TO svc_inventory;
ALTER DEFAULT PRIVILEGES FOR ROLE own_inventory IN SCHEMA inventory
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO svc_inventory;
ALTER DEFAULT PRIVILEGES FOR ROLE own_inventory IN SCHEMA inventory
  GRANT USAGE, SELECT ON SEQUENCES TO svc_inventory;

-- Nenhum GRANT de "inventory" para svc_catalog, svc_interest ou svc_purchase: a fronteira é
-- física, imposta pelo banco. A ausência de um GRANT aqui é o ponto — não um esquecimento.
