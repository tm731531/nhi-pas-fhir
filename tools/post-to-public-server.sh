#!/usr/bin/env bash
# Really POST a bundle to a live, public FHIR server — no credentials needed.
# Proves the output is well-formed FHIR a real server accepts, stores, and can read back.
# Usage: tools/post-to-public-server.sh <bundle.json> [base_url]
#        FHIR_BASE env overrides the base URL (default: public HAPI R4 test server).
#
# SCOPE (honest): a generic HAPI/Firely test server does NOT validate against the Taiwan IG
# (it stores meta.profile as a label, not a constraint) — "0 errors vs the official IG" is still
# tools/validate.sh's job, a separate thing. The public sandbox is also purged periodically.
# This is NOT submitting to the NHI: real 健保 submission needs an HCA cert + the NHI VPN.
set -euo pipefail
cd "$(dirname "$0")/.."
FILE="${1:?usage: tools/post-to-public-server.sh <bundle.json> [base_url]}"
BASE="${2:-${FHIR_BASE:-https://hapi.fhir.org/baseR4}}"

# A PAS bundle is type "collection"; posting it whole stores one Bundle resource, so the Claim
# never becomes a top-level resource. Rewrite to a transaction of POST + urn:uuid so the server
# mints a fresh id for every resource and resolves the internal references itself. urn:uuid (not
# PUT Type/id) is deliberate: on a shared public sandbox the toy ids (org-hosp, pat-1) collide
# across runs (HAPI-2840 duplicate); fresh uuids never collide and model "submit a new case".
TX="$(python3 - "$FILE" <<'PY'
import json, sys, uuid
d = json.load(open(sys.argv[1]))
entries = d.get("entry", [])
# Public HAPI dedupes by business identifier (fresh identifier -> 201, same -> HAPI-2840). Our
# reference bundles carry FIXED fabricated identifiers (fake 身分證 / 醫事機構代碼), so a re-run
# collides. All identifiers are test data, so suffix them per run -> every run is a new case, always
# 201, infinitely repeatable. (Reference resolution does NOT use these — it uses the urn:uuid below.)
run = uuid.uuid4().hex[:6]
print(f"run-tag: -{run} (appended to identifier values so each run is a fresh case)", file=sys.stderr)
def suffix_identifiers(node):
    if isinstance(node, dict):
        for k, v in node.items():
            if k == "identifier":
                for ident in (v if isinstance(v, list) else [v]):
                    if isinstance(ident, dict) and isinstance(ident.get("value"), str):
                        ident["value"] += f"-{run}"
            else:
                suffix_identifiers(v)
    elif isinstance(node, list):
        for v in node:
            suffix_identifiers(v)
for e in entries:
    suffix_identifiers(e["resource"])
# map every way a resource can be referenced (absolute fullUrl + relative Type/id) -> a fresh urn
ref_map = {}
for e in entries:
    r = e["resource"]; key = f"{r['resourceType']}/{r.get('id')}"
    urn = f"urn:uuid:{uuid.uuid4()}"
    e["_urn"] = urn
    ref_map[key] = urn
    if e.get("fullUrl"):
        ref_map[e["fullUrl"]] = urn
def rewrite(node):
    if isinstance(node, dict):
        ref = node.get("reference")
        if isinstance(ref, str) and ref in ref_map:
            node["reference"] = ref_map[ref]
        for v in node.values():
            rewrite(v)
    elif isinstance(node, list):
        for v in node:
            rewrite(v)
tx = {"resourceType": "Bundle", "type": "transaction", "entry": []}
for e in entries:
    r = e["resource"]; rewrite(r)
    r.pop("id", None)  # POST = create; a body id makes the server key off it and collide (HAPI-2840)
    tx["entry"].append({"fullUrl": e["_urn"], "resource": r,
                        "request": {"method": "POST", "url": r["resourceType"]}})
print(json.dumps(tx))
PY
)"

echo "== POST transaction -> ${BASE} =="
RESP_FILE="$(mktemp)"; trap 'rm -f "$RESP_FILE"' EXIT
CODE="$(curl -sS -X POST "$BASE" \
  -H "Content-Type: application/fhir+json" \
  --data-binary "$TX" -o "$RESP_FILE" -w '%{http_code}')"
echo "HTTP ${CODE}"

python3 - "$BASE" "$RESP_FILE" <<'PY'
import json, sys
base = sys.argv[1]
r = json.load(open(sys.argv[2]))
if r.get("resourceType") == "OperationOutcome":
    for i in r.get("issue", []):
        print(f"  [{i.get('severity')}/{i.get('code')}] {i.get('diagnostics','')}")
    sys.exit(1)
for e in r.get("entry", []):
    resp = e.get("response", {})
    loc = resp.get("location", "")
    ref = loc.split("/_history")[0]
    print(f"  {resp.get('status'):<12} {base}/{ref}" if ref else f"  {resp.get('status')}")
PY
