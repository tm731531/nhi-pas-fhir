#!/usr/bin/env bash
# Validate a FHIR resource file against the pinned NHI pas IG package.
# Usage: tools/validate.sh <resource.json> [extra validator args]
# Note: -tx n/a skips the terminology server (fast, structural). For full terminology
#       binding checks, drop -tx n/a (needs network to tx.fhir.org, slower).
set -euo pipefail
cd "$(dirname "$0")/.."
FILE="${1:?usage: tools/validate.sh <resource.json>}"; shift || true
[ -s .fhir/validator_cli.jar ] || { echo "run tools/fetch_validation_assets.sh first"; exit 1; }
exec java -jar .fhir/validator_cli.jar "$FILE" \
  -ig .fhir/pas-package.tgz -version 4.0.1 -tx n/a "$@"
