#!/usr/bin/env bash
# Validate a FHIR resource file against the pinned NHI pas IG package.
# Usage: tools/validate.sh <resource.json> [extra validator args]
# Note: -tx n/a skips the terminology server (fast, structural). For full terminology
#       binding checks, drop -tx n/a (needs network to tx.fhir.org, slower).
set -euo pipefail
cd "$(dirname "$0")/.."
FILE="${1:?usage: tools/validate.sh <resource.json> [extra args]   (IG_PKG=.fhir/ci-package.tgz to validate another IG)}"; shift || true
[ -s .fhir/validator_cli.jar ] || { echo "run tools/fetch_validation_assets.sh first"; exit 1; }
# Which IG package to bind to. Default = pas; override for a sibling IG, e.g.
#   IG_PKG=.fhir/ci-package.tgz tools/validate.sh impl/csharp/build/ci-bundle.cs.json
IG_PKG="${IG_PKG:-.fhir/pas-package.tgz}"
# tx-patch: corrected TW Core ICD CodeSystems (upstream 0.3.2 ships them with a wrong canonical url,
# which breaks Condition/Procedure/Substance CLOSED slicing). With the patch loaded, memberOf resolves
# locally so even -tx n/a reaches 0. See tools/fetch_validation_assets.sh + docs/validation/.
PATCH=(); [ -d .fhir/tx-patch ] && PATCH=(-ig .fhir/tx-patch)
exec java -jar .fhir/validator_cli.jar "$FILE" \
  -ig "$IG_PKG" "${PATCH[@]}" -version 4.0.1 -tx n/a "$@"
