-- Fecha o schema "public" para todo mundo. Sem isso, qualquer role autenticada poderia criar
-- objetos soltos fora do particionamento por serviço — o que tornaria o "nenhum grant cruzado"
-- das seções anteriores uma ilusão. Roda por último (prefixo "90"): precisa vir depois que os
-- quatro schemas de serviço já existem.
REVOKE ALL ON SCHEMA public FROM PUBLIC;
