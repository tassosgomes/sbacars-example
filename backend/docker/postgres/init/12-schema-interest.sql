-- Schema do serviço interest (D03 — Interesse e Atendimento). Ver §4.1 do plano.
CREATE SCHEMA interest AUTHORIZATION own_interest;

-- svc_interest: só DML no próprio schema, nunca DDL.
GRANT USAGE ON SCHEMA interest TO svc_interest;
ALTER DEFAULT PRIVILEGES FOR ROLE own_interest IN SCHEMA interest
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO svc_interest;
ALTER DEFAULT PRIVILEGES FOR ROLE own_interest IN SCHEMA interest
  GRANT USAGE, SELECT ON SEQUENCES TO svc_interest;

-- Nenhum GRANT de "interest" para svc_inventory, svc_catalog ou svc_purchase: a fronteira é
-- física, imposta pelo banco. A ausência de um GRANT aqui é o ponto — não um esquecimento.
