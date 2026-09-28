#!/usr/bin/env bash
# Fetch the authoritative validation assets (NOT committed — see .gitignore).
#   1) official IG package (machine-readable source of truth)
#   2) official HL7 FHIR validator (needs Java)
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p .fhir
IG_CANONICAL="https://nhicore.nhi.gov.tw/pas"
PKG_VER="1.2.6"

echo "[1/2] IG package ($IG_CANONICAL/package.tgz) ..."
curl -fSL --retry 3 -o .fhir/pas-package.tgz "$IG_CANONICAL/package.tgz"
rm -rf .fhir/pas-package && mkdir -p .fhir/pas-package
tar xzf .fhir/pas-package.tgz -C .fhir/pas-package
echo "    package: $(python3 -c "import json;d=json.load(open('.fhir/pas-package/package/package.json'));print(d['name'],d['version'])")"

echo "[2/2] HL7 validator_cli.jar (~200MB) ..."
if [ ! -s .fhir/validator_cli.jar ]; then
  curl -fSL --retry 3 -o .fhir/validator_cli.jar \
    https://github.com/hapifhir/org.hl7.fhir.core/releases/latest/download/validator_cli.jar
fi
java -version 2>/dev/null || { echo "WARNING: Java not found — validator needs Java 17+"; exit 1; }
echo "done. Pinned IG version target: $PKG_VER"
