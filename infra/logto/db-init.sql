-- A5 — Banco dedicado do Logto (§5.1 do plano de fundação).
--
-- O Logto ganha um banco próprio ("logto") na mesma instância PostgreSQL provisionada em A4,
-- com uma role própria — mas fora do particionamento por schema do §4.1: o IdP não é um serviço
-- de domínio e não deve compartilhar ciclo de vida com os dados de negócio do banco "sbacars".
--
-- Este script roda fora de `backend/docker/postgres/init/` de propósito: aquele diretório é do
-- Migrator dos serviços .NET (A4) e a task A5 não altera `backend/**`. Aqui rodamos como um job
-- do compose (`logto-db-init`), executado com o client `psql` diretamente contra a instância já
-- no ar — e não como script de entrypoint do container `postgres`.
--
-- Idempotente: seguro reexecutar a cada `docker compose up`. As senhas abaixo são fixas e não
-- sensíveis — servem só para o ambiente local, igual ao padrão já usado em
-- backend/docker/postgres/init/00-roles.sql.
-- CREATEROLE (e não NOCREATEROLE, diferente das roles de negócio em backend/docker/postgres/init/)
-- é necessário porque o próprio `@logto/cli db seed` cria, na primeira vez, uma role interna
-- `logto_tenant_<database>` para isolamento por tenant (ver `_before_all.sql` do pacote
-- `@logto/schemas`) — verificado rodando o seed de verdade contra este script: sem CREATEROLE,
-- ele falha com "permission denied to create role". Continua sem CREATEDB e sem SUPERUSER.
DO
$$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'logto') THEN
    CREATE ROLE logto WITH LOGIN PASSWORD 'logto_dev_pw' NOSUPERUSER NOCREATEDB CREATEROLE NOINHERIT;
  END IF;
END
$$;

-- CREATE DATABASE não pode rodar dentro de bloco DO/transação; o truque \gexec do psql monta o
-- comando condicionalmente e executa fora de transação.
SELECT 'CREATE DATABASE logto OWNER logto ENCODING ''UTF8'''
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'logto')
\gexec
