#!/usr/bin/env bash
set -euo pipefail

base_url="${BASE_URL:-http://127.0.0.1:18080}"
response_file="$(mktemp)"
archive_file="$(mktemp --suffix=.zip)"
missing_file="$(mktemp)"
trap 'rm -f "$response_file" "$archive_file" "$missing_file"' EXIT

curl --fail --silent --show-error --retry 30 --retry-connrefused --retry-delay 1 \
  "$base_url/healthz" | python3 -c 'import json,sys; assert json.load(sys.stdin) == {"status":"ok"}'

curl --fail --silent --show-error "$base_url/" | python3 -c \
  'import sys; page=sys.stdin.read(); assert "id=\"root\"" in page and ("/src/main.tsx" in page or "/assets/" in page)'

curl --fail --silent --show-error \
  -F "file=@tests/fixtures/synthetic-coinmotion.csv;type=text/csv" \
  "$base_url/report/generate?year=2024" > "$response_file"
report_id="$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["report_id"])' "$response_file")"
test -n "$report_id"

curl --fail --silent --show-error \
  "$base_url/report/download/$report_id" --output "$archive_file"
python3 -c 'import sys,zipfile; z=zipfile.ZipFile(sys.argv[1]); names=z.namelist(); assert names and any(n.endswith(".pdf") for n in names), names; assert z.testzip() is None; assert all(z.read(n).startswith(b"%PDF-") for n in names if n.endswith(".pdf"))' "$archive_file"

status="$(curl --silent --show-error --output "$missing_file" --write-out '%{http_code}' \
  "$base_url/report/download/$report_id")"
test "$status" = "404"

printf 'Staging smoke passed: health, UI shell, synthetic CSV, ZIP/PDF, one-time report cleanup.\n'
