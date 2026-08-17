#!/bin/bash
set -euo pipefail

# C2 — idempotent bucket bootstrap for local compose (§7, §10). Runs once per
# `docker compose up` after MinIO is healthy; safe to re-run (`mc mb --ignore-existing`).
#
# CORS: `spa-cors.xml` is the canonical per-bucket policy for managed S3 (C3/prod). MinIO
# community does not implement PutBucketCors — CORS is global via `MINIO_API_CORS_ALLOW_ORIGIN`
# on the `minio` service (see compose comment). This job verifies that global setting.
#
# Lifecycle rules for the 6-year retention window (§7) are intentionally out of scope here —
# wrong ILM on the dev volume would delete objects; production uses managed S3 policies (§11.3).

ALIAS=local
ENDPOINT=http://minio:9000
MINIO_HOST=minio
MINIO_PORT=9000
ACCESS_KEY="${MINIO_ROOT_USER:-sbacars}"
SECRET_KEY="${MINIO_ROOT_PASSWORD:-sbacars_dev_pw}"

BUCKETS=(
  sbacars-catalog-media
  sbacars-inventory-docs
  sbacars-purchase-dossier
)

http_request() {
  local method="$1"
  local path="$2"
  shift 2

  exec 3<>"/dev/tcp/${MINIO_HOST}/${MINIO_PORT}" || {
    echo "FAIL: cannot connect to ${MINIO_HOST}:${MINIO_PORT}" >&2
    exit 1
  }

  {
    printf '%s %s HTTP/1.1\r\n' "${method}" "${path}"
    printf 'Host: %s:%s\r\n' "${MINIO_HOST}" "${MINIO_PORT}"
    while [ "$#" -gt 0 ]; do
      printf '%s\r\n' "$1"
      shift
    done
    printf 'Connection: close\r\n\r\n'
  } >&3

  local response
  response="$(cat <&3)"
  exec 3<&-
  exec 3>&-
  printf '%s' "${response}"
}

http_status_code() {
  local response
  response="$(http_request "$@")"
  printf '%s' "${response}" | head -n 1 | cut -d' ' -f2
}

http_header_value() {
  local header_name="${1,,}"
  local response="$2"
  local line key value

  while IFS= read -r line; do
    [ -z "${line//$'\r'/}" ] && break
    key="${line%%:*}"
    value="${line#*:}"
    key="${key//$'\r'/}"
    value="${value# }"
    value="${value//$'\r'/}"
    if [ "${key,,}" = "${header_name}" ]; then
      printf '%s' "${value}"
      return 0
    fi
  done <<< "${response#*$'\n'}"
}

echo "Configuring mc alias against ${ENDPOINT}..."
mc alias set "${ALIAS}" "${ENDPOINT}" "${ACCESS_KEY}" "${SECRET_KEY}"

for bucket in "${BUCKETS[@]}"; do
  echo "Ensuring bucket ${bucket}..."
  mc mb --ignore-existing "${ALIAS}/${bucket}"

  echo "Setting anonymous access to none on ${bucket}..."
  mc anonymous set none "${ALIAS}/${bucket}"
done

echo "Verifying global CORS origins (MinIO community)..."
for origin in http://localhost:5173 http://localhost:5174; do
  response="$(http_request OPTIONS "/sbacars-catalog-media/cors-probe" \
    "Origin: ${origin}" \
    "Access-Control-Request-Method: PUT" \
    "Access-Control-Request-Headers: Content-Type")"
  allowed_origin="$(http_header_value "Access-Control-Allow-Origin" "${response}")"
  if [ "${allowed_origin}" != "${origin}" ]; then
    echo "FAIL: CORS preflight did not allow origin ${origin} (got '${allowed_origin}')" >&2
    printf '%s\n' "${response}" >&2
    exit 1
  fi
done

denied_response="$(http_request OPTIONS "/sbacars-catalog-media/cors-probe" \
  "Origin: http://evil.example" \
  "Access-Control-Request-Method: PUT" \
  "Access-Control-Request-Headers: Content-Type")"
if [ -n "$(http_header_value "Access-Control-Allow-Origin" "${denied_response}")" ]; then
  echo "FAIL: unexpected CORS allow for untrusted origin" >&2
  printf '%s\n' "${denied_response}" >&2
  exit 1
fi

echo "Verifying buckets, privacy, and anonymous access..."
for bucket in "${BUCKETS[@]}"; do
  if ! mc ls "${ALIAS}/${bucket}" >/dev/null 2>&1; then
    echo "FAIL: bucket missing: ${bucket}" >&2
    exit 1
  fi

  policy="$(mc anonymous get "${ALIAS}/${bucket}" 2>&1 || true)"
  if [[ "${policy}" == *download* || "${policy}" == *public* || "${policy}" == *read* ]]; then
    echo "FAIL: bucket ${bucket} allows anonymous access: ${policy}" >&2
    exit 1
  fi

  http_code="$(http_status_code GET "/${bucket}/")"
  if [ "${http_code}" = "200" ]; then
    echo "FAIL: anonymous GET on ${bucket}/ returned 200" >&2
    exit 1
  fi
done

echo "MinIO buckets ready (private, CORS restricted to local SPAs)."
