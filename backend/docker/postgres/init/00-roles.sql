-- A4 — Privilégio mínimo por serviço (§4.1 do plano de fundação).
--
-- Cada um dos quatro serviços de domínio recebe duas roles:
--   own_<serviço>  — dona do schema, único privilégio de DDL. Usada apenas pelo Migrator do
--                    serviço, nunca pela API em execução.
--   svc_<serviço>  — role de aplicação, DML apenas no próprio schema. Usada pelo DbContext do
--                    serviço em runtime.
--
-- As senhas abaixo são fixas e não sensíveis: servem só para o ambiente local do
-- docker-compose.yml. Nenhum outro ambiente reaproveita este script nem estas credenciais —
-- lá a connection string vem de variável de ambiente/segredo (§4.4).
CREATE ROLE own_inventory WITH LOGIN PASSWORD 'own_inventory_dev_pw' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
CREATE ROLE svc_inventory WITH LOGIN PASSWORD 'svc_inventory_dev_pw' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;

CREATE ROLE own_catalog WITH LOGIN PASSWORD 'own_catalog_dev_pw' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
CREATE ROLE svc_catalog WITH LOGIN PASSWORD 'svc_catalog_dev_pw' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;

CREATE ROLE own_interest WITH LOGIN PASSWORD 'own_interest_dev_pw' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
CREATE ROLE svc_interest WITH LOGIN PASSWORD 'svc_interest_dev_pw' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;

CREATE ROLE own_purchase WITH LOGIN PASSWORD 'own_purchase_dev_pw' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
CREATE ROLE svc_purchase WITH LOGIN PASSWORD 'svc_purchase_dev_pw' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
