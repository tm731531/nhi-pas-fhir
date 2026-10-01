#!/usr/bin/env bash
# Package the 插卡→FHIR demo as self-contained single-file binaries (no .NET SDK needed on the target).
# Usage:  tools/publish-card-demo.sh [RID] [OUTDIR]
#   RID    runtime identifier: linux-x64 (default) | win-x64 | osx-arm64 | osx-x64
#   OUTDIR output root (default: ./dist)
# Produces: <OUTDIR>/card-helper/card-helper[.exe]  and  <OUTDIR>/card-web-ui/card-web-ui[.exe] (+ wwwroot)
set -euo pipefail

RID="${1:-linux-x64}"
OUT="${2:-dist}"
CS="$(cd "$(dirname "$0")/../impl/csharp" && pwd)"
export PATH="$HOME/.dotnet:$PATH"

pub() {
  local proj="$1" name="$2"
  echo "── publishing $name ($RID) ──"
  dotnet publish "$CS/samples/$proj" -c Release -r "$RID" \
    --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "$OUT/$name" -v q
}

pub CardHelper/CardHelper.csproj card-helper
pub CardWebUI/CardWebUI.csproj  card-web-ui

echo
echo "✅ done. Run the two binaries (no dotnet needed):"
echo "   1) $OUT/card-helper/card-helper      # :8531  (插卡那台,card inserted)"
echo "   2) $OUT/card-web-ui/card-web-ui      # :8530  → open http://localhost:8530"
