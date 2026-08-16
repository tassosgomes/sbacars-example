#!/usr/bin/env bash
#
# Gate deterministico do TSG Flow — sbacars-example (backend .NET + frontend Node/Vite).
#
# Roda formatacao, build e testes de backend e frontend SEM consumir contexto de LLM. E o gate
# que `.github/workflows/ci.yml` executa em push/PR para `main`, e o mesmo que
# `tsg-flow-validator`/`tsg-flow-implementer` executariam quando o pipeline TSG Flow de tasks for
# ativado neste repositorio.
#
# Modo padrao (sem --filter): validacao COMPLETA — e o modo que este repositorio usa hoje, porque
# ainda nao ha tasks TSG Flow em andamento (nada em tasks/) e o gate existe para provar a fundacao
# inteira a cada push/PR, nao um diff de task. format roda sobre TODO o repositorio (nao apenas o
# diff — HEAD sendo a propria base nao teria o que escopar), build e testes rodam a suite inteira
# dos dois stacks.
#
# Modo focused (com um ou mais --filter): pensado para quando o pipeline TSG Flow de tasks entrar
# em uso — format/lint continuam escopados por --base (INVARIANTE 1 do contrato) e os testes de
# backend usam os filtros informados (INVARIANTE 2 detecta filtro vazio); os passos de frontend
# continuam rodando por completo, porque nao ha mecanismo de filtro por arquivo no vitest/eslint
# deste repositorio equivalente ao `dotnet test --filter`.
#
# Uso:
#   scripts/ai-flow/gate.sh [--filter=<expr>]... [--base=<ref>] [--all-tests] [--sln=<path>] [--skip-tests]
#
# Saida: bloco compacto (<= ~60 linhas). Exit 0 = APROVADO, 1 = REPROVADO, 2 = erro de uso/ambiente.

set -uo pipefail

MAX_OUTPUT_LINES=40
SLN=""
SKIP_TESTS=0
ALL_TESTS=0
BASE_REF="HEAD"
FILTERS=()

# Sem interatividade e sem cor ANSI poluindo a saida truncada (INVARIANTE 4).
export CI=true
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export TERM=dumb
export NO_COLOR=1

for arg in "$@"; do
  case "$arg" in
    --filter=*)    FILTERS+=("${arg#*=}") ;;
    --base=*)      BASE_REF="${arg#*=}" ;;
    --all-tests)   ALL_TESTS=1 ;;
    --sln=*)       SLN="${arg#*=}" ;;
    --skip-tests)  SKIP_TESTS=1 ;;
    -h|--help)     sed -n '2,26p' "$0"; exit 0 ;;
    *) echo "GATE: ERRO"; echo "argumento desconhecido: $arg"; exit 2 ;;
  esac
done

REPO_ROOT="$(git rev-parse --show-toplevel 2>/dev/null)" || {
  echo "GATE: ERRO"; echo "nao esta em um repositorio git"; exit 2
}
cd "$REPO_ROOT" || exit 2

if [[ -z "$SLN" ]]; then
  SLN="$(find backend -maxdepth 1 -name '*.sln' | head -1)"
fi
if [[ -z "$SLN" ]]; then
  echo "GATE: ERRO"; echo "nenhum .sln encontrado em backend/ (use --sln=<path>)"; exit 2
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "GATE: ERRO"; echo "comando 'dotnet' nao encontrado no PATH"; exit 2
fi
if ! command -v npm >/dev/null 2>&1; then
  echo "GATE: ERRO"; echo "comando 'npm' nao encontrado no PATH"; exit 2
fi

# FOCUSED = um ou mais --filter informados (uso futuro do TSG Flow por task) — governa o escopo
#           do format/lint (INVARIANTE 1).
# RUN_FULL_TESTS = --all-tests OU nenhum --filter informado — governa a etapa de testes
#           independentemente do escopo do format: "--filter=X --all-tests" roda a suite
#           completa dos testes mesmo com format escopado por --base, exatamente o par de flags
#           que o "full" final de um PRD do TSG Flow usa (contrato: --base=<commit-base> +
#           --all-tests).
if ((${#FILTERS[@]} > 0)); then
  FOCUSED=1
else
  FOCUSED=0
fi
if ((ALL_TESTS == 1)) || ((${#FILTERS[@]} == 0)); then
  RUN_FULL_TESTS=1
else
  RUN_FULL_TESTS=0
fi

# ---------------------------------------------------------------------------
# Escopo do format: arquivos alterados desde BASE_REF em modo focused; TODO o
# repositorio em modo full (arvore inteira contra a arvore vazia do git).
# INVARIANTE 1 — nunca reprova por debito que a task/push atual nao causou.
# ---------------------------------------------------------------------------
EMPTY_TREE_HASH="4b825dc642cb6eb9a060e54bf8d69288fbee4904"
if ((FOCUSED == 1)); then
  FORMAT_BASE_REF="$BASE_REF"
else
  FORMAT_BASE_REF="$EMPTY_TREE_HASH"
fi

mapfile -t CHANGED < <(
  { git diff --name-only "$FORMAT_BASE_REF" -- 2>/dev/null
    git ls-files --others --exclude-standard 2>/dev/null
  } | sort -u | grep -v '^$'
)
mapfile -t CHANGED_CS < <(printf '%s\n' "${CHANGED[@]:-}" | grep -E '^backend/.*\.cs$' || true)

fail() {
  local etapa="$1" cmd="$2" out="$3"
  echo "GATE: REPROVADO"
  echo "etapa: $etapa"
  echo "comando: $cmd"
  echo "--- output (ultimas ${MAX_OUTPUT_LINES} linhas) ---"
  printf '%s\n' "$out" | tail -n "$MAX_OUTPUT_LINES"
  exit 1
}

run() { # run <comando...> -> exporta OUT, retorna exit code
  OUT="$("$@" 2>&1)"
  return $?
}

# ---------------------------------------------------------------------------
# 1. Formatacao / lint — backend (dotnet format, escopado) + frontend (eslint)
# ---------------------------------------------------------------------------
FORMAT_STATUS="pulado (nenhum .cs alterado)"
if ((FOCUSED == 1)); then
  if ((${#CHANGED_CS[@]} > 0)); then
    CMD=(dotnet format "$SLN" --verify-no-changes --no-restore --include "${CHANGED_CS[@]}")
    if ! run "${CMD[@]}"; then
      fail "format" "dotnet format $SLN --verify-no-changes --no-restore --include <${#CHANGED_CS[@]} arquivos>" "$OUT"
    fi
    FORMAT_STATUS="ok (${#CHANGED_CS[@]} arquivos .cs)"
  fi
else
  CMD=(dotnet format "$SLN" --verify-no-changes --no-restore)
  if ! run "${CMD[@]}"; then
    fail "format" "dotnet format $SLN --verify-no-changes --no-restore" "$OUT"
  fi
  FORMAT_STATUS="ok (repositorio inteiro)"
fi

CMD=(npm run lint --workspaces --if-present)
if ! run "${CMD[@]}"; then
  fail "format" "npm run lint --workspaces --if-present" "$OUT"
fi
FORMAT_STATUS="$FORMAT_STATUS; frontend lint ok"

# ---------------------------------------------------------------------------
# 2. Build — backend (dotnet build, warnings-as-errors via Directory.Build.props)
#    + frontend (typecheck e build; `build` do frontend ja roda `tsc --noEmit`
#    antes do vite build, mas `typecheck` roda antes de propósito para reportar
#    erro de tipo como etapa "build" mesmo se `vite build` falhar por outro motivo)
# ---------------------------------------------------------------------------
CMD=(dotnet build "$SLN" --no-restore)
if ! run "${CMD[@]}"; then
  fail "build" "dotnet build $SLN --no-restore" "$OUT"
fi
BUILD_SUMMARY="$(printf '%s\n' "$OUT" | grep -oE '[0-9]+ (Error|Warning)\(s\)' | tr '\n' ' ')"
BUILD_STATUS="backend ok ${BUILD_SUMMARY:-}"

CMD=(npm run typecheck --workspaces --if-present)
if ! run "${CMD[@]}"; then
  fail "build" "npm run typecheck --workspaces --if-present" "$OUT"
fi

CMD=(npm run build --workspaces --if-present)
if ! run "${CMD[@]}"; then
  fail "build" "npm run build --workspaces --if-present" "$OUT"
fi
BUILD_STATUS="$BUILD_STATUS; frontend typecheck+build ok"

# ---------------------------------------------------------------------------
# 3. Testes — backend (dotnet test, com DETECCAO DE FILTRO VAZIO — INVARIANTE 2)
#    + frontend (npm run test, vitest falha por padrao com zero testes coletados)
# ---------------------------------------------------------------------------
TEST_STATUS="pulado (--skip-tests)"

# -m:1 limita o MSBuild a um no, o que faz os projetos de teste rodarem em SEQUENCIA em vez de em
# paralelo. Nao e preferencia de estilo: tres projetos usam Testcontainers e cada processo de teste
# inicia o ResourceReaper (Ryuk) do Testcontainers no arranque. Com dois processos subindo ao mesmo
# tempo, a criacao do container do Ryuk corre entre si e falha de forma intermitente
# ("Could not find resource 'DockerContainer'"), derrubando o gate em codigo que esta correto — um
# gate que reprova por acaso nao serve como gate. Serializar custa pouco tempo de parede aqui,
# porque a suite ja e dominada por um unico projeto (BuildingBlocks.Web.Tests, que sobe o Logto).
DOTNET_TEST_PARALLELISM=(-m:1)

if ((SKIP_TESTS == 0)); then
  if ((RUN_FULL_TESTS == 0)); then
    RESULTS=()
    for f in "${FILTERS[@]}"; do
      CMD=(dotnet test "$SLN" --no-build --no-restore "${DOTNET_TEST_PARALLELISM[@]}" --filter "$f")
      if ! run "${CMD[@]}"; then
        fail "testes" "dotnet test $SLN --no-build --no-restore -m:1 --filter \"$f\"" "$OUT"
      fi
      # "Passed!  - Failed: 0, Passed: 95, ..." — soma o que rodou
      SUM="$(printf '%s\n' "$OUT" | grep -oE 'Passed:[[:space:]]+[0-9]+' | grep -oE '[0-9]+' | awk '{s+=$1} END {print s+0}')"
      if [[ "${SUM:-0}" == "0" ]]; then
        fail "testes" "dotnet test $SLN --no-build --no-restore -m:1 --filter \"$f\"" \
          "Filtro nao selecionou nenhum teste. A suite exigida pela task provavelmente nao existe.
$OUT"
      fi
      RESULTS+=("$f=${SUM}")
    done
    TEST_STATUS="backend ok (${RESULTS[*]})"
  else
    CMD=(dotnet test "$SLN" --no-build --no-restore "${DOTNET_TEST_PARALLELISM[@]}")
    if ! run "${CMD[@]}"; then
      fail "testes" "dotnet test $SLN --no-build --no-restore -m:1" "$OUT"
    fi
    SUM="$(printf '%s\n' "$OUT" | grep -oE 'Passed:[[:space:]]+[0-9]+' | grep -oE '[0-9]+' | awk '{s+=$1} END {print s+0}')"
    if [[ "${SUM:-0}" == "0" ]]; then
      fail "testes" "dotnet test $SLN --no-build --no-restore -m:1" \
        "Nenhum teste foi executado na suite completa do backend — provavelmente um projeto de teste quebrou a descoberta.
$OUT"
    fi
    TEST_STATUS="backend ok (suite completa, ${SUM} testes)"
  fi

  CMD=(npm run test --workspaces --if-present)
  if ! run "${CMD[@]}"; then
    fail "testes" "npm run test --workspaces --if-present" "$OUT"
  fi
  TEST_STATUS="$TEST_STATUS; frontend ok"
fi

# ---------------------------------------------------------------------------
# 4. Higiene do diff (agnostico de stack)
# ---------------------------------------------------------------------------
# Sempre contra BASE_REF (HEAD por padrao), nunca contra a arvore vazia usada para escopar o
# format em modo full: "trailing whitespace"/marcador de conflito faz sentido checar no que MUDOU,
# nao no repositorio inteiro — markdown deste repo usa quebra de linha rigida (dois espacos no
# fim da linha) de proposito, e isso apareceria como falso positivo contra a arvore vazia.
if ! run git diff --check "$BASE_REF" --; then
  fail "diff-check" "git diff --check $BASE_REF" "$OUT"
fi

echo "GATE: APROVADO"
echo "modo: format=$([ "$FOCUSED" = 1 ] && echo focused || echo full) testes=$([ "$RUN_FULL_TESTS" = 1 ] && echo full || echo focused)"
echo "arquivos alterados: ${#CHANGED[@]} (backend .cs: ${#CHANGED_CS[@]})"
echo "format: $FORMAT_STATUS"
echo "build: $BUILD_STATUS"
echo "testes: $TEST_STATUS"
exit 0
