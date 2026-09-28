# nhi-pas-fhir — Taiwan NHI Prior Authorization (事前審查) FHIR reference implementation

An open-source, spec-driven, multi-language framework that generates **officially-valid** Taiwan
National Health Insurance (健保署) **FHIR** artifacts for **Prior Authorization / 事前審查 (TWPAS)**.
Every output is gated by the **official HL7 FHIR validator at 0 errors** (structural + terminology).

**Keywords:** Taiwan NHI · 健保 · TWPAS · 事前審查 · Prior Authorization · FHIR R4 · Da Vinci PAS ·
TW Core · HL7 · reference implementation · C# / Firely SDK · Python · healthcare interoperability.

> Domain source of truth: <https://nhicore.nhi.gov.tw/pas/> · IG `tw.gov.mohw.nhi.pas#1.2.6` · FHIR R4
> (inherits `tw.gov.mohw.twcore` + `hl7.fhir.us.davinci-pas`).

## What it does

Give it a neutral, non-FHIR description of a case (patient, provider, drug, diagnosis…); it assembles the
full FHIR `Bundle` the NHI expects, and guarantees it passes the official validator. **You describe the
patient's case; the library speaks FHIR.** It is a payload factory + validator gate + response handler —
**not** a FHIR server (that is the NHI platform's role).

### Coverage (every output validated to 0 errors)

| Case | Resources | Official example |
|---|---|---|
| 癌藥 / general submission (cancer drug) | 28 | Bundle-bun-1 |
| 免疫製劑 (immunologic agent) | 36 | Bundle-bun-imm |
| 申復 (appeal) | 28 | Bundle-bun-3 |
| 自主審查 (self-assessment) | 29 | Bundle-bun-self |
| 核定回應 (NHI decision / ClaimResponse) | — | Bundle-bun-response |
| OperationOutcome (error report) | — | error-example |

Filing category (送核/補件/申復/爭議/申復補件) × application category (一般/自主/緊急) and all 50 NHI
departments are parameterized. **43/43 instantiable IG profiles** are exercised. See
[spec/docs/COVERAGE.md](spec/docs/COVERAGE.md).

## The model

```
   spec/            ← single source of truth (language-agnostic)
     specs/         feature specifications
     docs/          IG mirror, architecture, explainer, coverage, validation evidence
     reference-bundles/  the validated "correct" JSON (golden)
        │  spec drives every implementation
   ┌────┴───────────────────────┐
   ▼                            ▼
 impl/csharp/  (primary,     impl/python/  (reference)
   Firely SDK)                 (pydantic)
   └──────────────┬─────────────┘
                  ▼
   tools/ + official HL7 validator  →  0 errors
```

Each `impl/<lang>/` reproduces `spec/reference-bundles/*.json` and passes the official validator at
**0 errors**. Correctness is machine-proven, not asserted — "it runs" ≠ "it is correct".

## Quickstart

```bash
# fetch the authoritative IG package + official validator (large, gitignored)
tools/fetch_validation_assets.sh

# C# (primary implementation)
export PATH="$HOME/.dotnet:$PATH"
cd impl/csharp && dotnet test                       # contract + golden-regression tests
dotnet run --project samples/Emit                   # emit the bundles to build/*.cs.json

# validate ANY fhir json against the pinned IG (from repo root): 0 errors = conformant
tools/validate.sh impl/csharp/build/pa-bundle.cs.json
```

C# usage: [impl/csharp/README.md](impl/csharp/README.md) (English) · [中文](impl/csharp/README.zh.md) ·
per-case manual: [impl/csharp/MANUAL.md](impl/csharp/MANUAL.md).

## Docs

- [EXPLAINER](spec/docs/EXPLAINER.md) — what FHIR is, for a systems owner (not a FHIR expert)
- [ARCHITECTURE](spec/docs/ARCHITECTURE.md) — interface → abstract → implementation + factory
- [CASE-CATALOG](spec/docs/CASE-CATALOG.md) — every case type, lifecycle, class hierarchy
- [COVERAGE](spec/docs/COVERAGE.md) — coverage vs the official IG
- [CONTRACTS-and-TESTS](spec/docs/CONTRACTS-and-TESTS.md) — the contracts and test plan

## Principles

- **Never fabricate FHIR fields** — every value is transcribed from the authoritative IG package.
- **Correctness = the official validator at 0 errors**, not "looks right".
- **No real patient data, ever.** All examples use fabricated identifiers.

## License

[Apache License 2.0](LICENSE).
