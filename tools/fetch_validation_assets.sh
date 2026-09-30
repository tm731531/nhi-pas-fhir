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

# Additional published IGs implemented in this repo (same fetch shape). Add a line per IG as built.
for ig in "ci:1.0.2"; do
  name="${ig%%:*}"
  echo "[1b] IG package (nhi.$name) ..."
  curl -fSL --retry 3 -o ".fhir/${name}-package.tgz" "https://nhicore.nhi.gov.tw/${name}/package.tgz"
done

echo "[2/3] HL7 validator_cli.jar (~200MB) ..."
if [ ! -s .fhir/validator_cli.jar ]; then
  curl -fSL --retry 3 -o .fhir/validator_cli.jar \
    https://github.com/hapifhir/org.hl7.fhir.core/releases/latest/download/validator_cli.jar
fi
java -version 2>/dev/null || { echo "WARNING: Java not found — validator needs Java 17+"; exit 1; }

# [3/3] Terminology patch. TW Core 0.3.2 ships the ICD-10-CM/PCS CodeSystems with a WRONG canonical url
# (it points at a /ValueSet/ path), so the validator cannot resolve the /CodeSystem/ url the ValueSets
# include → Condition/Procedure/Substance CLOSED slicing hard-fails (the official pas example trips on
# this too). The codes ARE present (complete content). We rewrite the url to the correct /CodeSystem/
# canonical and load it via -ig, so memberOf resolves locally and bundles validate at 0.
TWCORE_VER="0.3.2"
echo "[3/3] terminology patch (corrected TW Core ICD CodeSystems) ..."
TWDIR="$HOME/.fhir/packages/tw.gov.mohw.twcore#${TWCORE_VER}/package"
if [ ! -d "$TWDIR" ]; then
  # Not in the validator cache yet — fetch the package directly from the FHIR registry.
  curl -fSL --retry 3 -o .fhir/twcore.tgz "https://packages.simplifier.net/tw.gov.mohw.twcore/${TWCORE_VER}"
  rm -rf .fhir/twcore && mkdir -p .fhir/twcore && tar xzf .fhir/twcore.tgz -C .fhir/twcore
  TWDIR=".fhir/twcore/package"
fi
mkdir -p .fhir/tx-patch
python3 - "$TWDIR" <<'PY'
import json, sys, pathlib
src=pathlib.Path(sys.argv[1]); out=pathlib.Path('.fhir/tx-patch')
for name in ['icd-10-cm-2023-tw', 'icd-10-pcs-2023-tw']:
    f = src / f'CodeSystem-{name}.json'
    if not f.exists():
        print(f"    WARNING: {f} missing — patch incomplete"); continue
    cs = json.load(open(f))
    cs['url'] = f'https://twcore.mohw.gov.tw/ig/twcore/CodeSystem/{name}'  # fix: was a /ValueSet/ url
    cs['id'] = name + '-fixed'
    json.dump(cs, open(out / f'CodeSystem-{name}-fixed.json', 'w'), ensure_ascii=False)
    print(f"    {name}: content={cs.get('content')} concepts={len(cs.get('concept', []))}")
PY
echo "done. Pinned IG version target: $PKG_VER"
