# nhi-pas-fhir — Taiwan NHI Prior Authorization (事前審查) FHIR reference implementation

[![Release](https://img.shields.io/github/v/release/tm731531/nhi-pas-fhir?sort=semver)](https://github.com/tm731531/nhi-pas-fhir/releases)
[![CI](https://github.com/tm731531/nhi-pas-fhir/actions/workflows/ci.yml/badge.svg)](https://github.com/tm731531/nhi-pas-fhir/actions/workflows/ci.yml)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](LICENSE)
[![FHIR R4](https://img.shields.io/badge/FHIR-R4%20(4.0.1)-e5462a.svg)](https://hl7.org/fhir/R4/)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](https://dotnet.microsoft.com/)

An open-source, spec-driven, multi-language framework that generates **officially-valid** Taiwan
National Health Insurance (健保署) **FHIR** artifacts for **Prior Authorization / 事前審查 (TWPAS)**.
Every output is gated by the **official HL7 FHIR validator at 0 errors** (structural + terminology).

**Keywords:** Taiwan NHI · 健保 · TWPAS · 事前審查 · Prior Authorization · FHIR R4 · Da Vinci PAS ·
TW Core · HL7 · CQL · Clinical Quality Language · ELM · CQF-Ruler · cqframework · terminology · 核刪 ·
pre-submit rule check · ClaimResponse · reference implementation · C# / Firely SDK · Python ·
healthcare interoperability · clinical decision support.

> Domain source of truth: <https://nhicore.nhi.gov.tw/pas/> · IG `tw.gov.mohw.nhi.pas#1.2.6` · FHIR R4
> (inherits `tw.gov.mohw.twcore` + `hl7.fhir.us.davinci-pas`). Last IG sync: 2026-09-28.

> 🤖 **Reading this with an AI agent?** Start with **[`AGENTS.md`](AGENTS.md)** — repo map, copy-paste
> build/test/run commands, how the pieces fit, the hard rules, and the gotchas — or **[`llms.txt`](llms.txt)**
> for a compact machine summary.

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

Filing category (送核/補件/申復/爭議/申復補件) × application category (一般/自主/緊急) are parameterized,
and the department field accepts any NHI department code. **40/40 profiles + 3/3 extensions** of the PAS
IG are exercised (logical models are non-instantiable). See [spec/docs/COVERAGE.md](spec/docs/COVERAGE.md).

> **Scope today:** this repo implements **one** IG — 事前審查 (`nhi.pas`) — with **two** case assemblers
> (癌藥 · 免疫製劑). The other published Taiwan IGs (重大傷病, 電子處方箋, NGS, EMR, 傳染病…) are on the
> roadmap, built the same way, one at a time. 長照 has **no published FHIR IG yet**. We never claim what
> isn't built.

## CQL pre-submit self-check (送前核刪自查)

The NHI publishes its reimbursement rules as **CQL** (Clinical Quality Language, IG
`tw.gov.mohw.nhi.cql`, ~66 drug rules compiled to **ELM**). Before you POST, this framework runs the NHI's
**own official rules** — faithfully, on a real engine — against your Bundle and predicts the outcome, so
you send only what will pass and hold back what would be **核刪 (post-payment clawback)** or needs **補件
(missing data)**. Three-state output: **Pass / WouldBeRejected / DataMissing** (not a naive pass/fail).

> **Scope today:** proven **end-to-end on one rule** (乳癌 Abemaciclib). "~66" is the size of the NHI
> catalogue, **not** what this repo has vendored — loading more rules is a data task (see
> [cql-integration-notes.md](spec/docs/cql-integration-notes.md)), tracked, in progress.

- Faithful engine: a **CQF-Ruler / HAPI clinical-reasoning** server runs the ELM (it implements
  `InCodeSystem`, which lightweight engines do not) — `cql-engine/` (docker + loader + official rules).
- Toggleable + swappable in C#: `ICqlPreCheck` (off by default) → `CqlPreCheck` → `ICqlEngine`
  (`CqfRulerCqlEngine`). Rules live in the server, not in code: a rule/IG update = reload + re-run the
  same Bundle, no code change.
- Plain-language walk-throughs: [spec/docs/cql-explained.md](spec/docs/cql-explained.md) (what it is) ·
  [spec/docs/cql-wiring.md](spec/docs/cql-wiring.md) (how it is wired) ·
  [spec/docs/cql-integration-notes.md](spec/docs/cql-integration-notes.md) (design + gotchas).

## Demo app (產→驗→查→送→回)

[`demo/`](demo/) — an ASP.NET Core Razor Pages app that consumes the library and shows the whole flow:
pick a case → assemble the Bundle (產) → seed check (驗) → CQL self-check (查) → submit to a receiver
(送) → read the ClaimResponse (回, `queued/審核中` — because **收件 ≠ 核准**). It holds no FHIR/CQL logic
of its own; it only calls the lib.

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

   cql-engine/   ← faithful CQL/ELM engine (CQF-Ruler) + official rules  (送前核刪自查)
   demo/         ← Razor Pages app consuming the lib (產→驗→查→送→回)
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

## Overview diagram
[`docs/flow.html`](docs/flow.html) — interactive, Bundle-centric flow (產→驗→查→送→回); each station
maps to the real class/file/numbers. Open it locally, or serve it from GitHub Pages / your own host.

## Docs

- [NHI↔FHIR MIGRATION](spec/docs/nhi-fhir-migration.md) — how 健保 data maps to/from FHIR (媒體申報 · FHIR IG · 長照 三個世界), and how an existing HIS/EMR migrates in stages
- [EXPLAINER](spec/docs/EXPLAINER.md) — what FHIR is, for a systems owner (not a FHIR expert)
- [ARCHITECTURE](spec/docs/ARCHITECTURE.md) — interface → abstract → implementation + factory
- [CASE-CATALOG](spec/docs/CASE-CATALOG.md) — every case type, lifecycle, class hierarchy
- [COVERAGE](spec/docs/COVERAGE.md) — coverage vs the official IG
- [IG-TRACEABILITY](spec/docs/IG-TRACEABILITY.md) — every official IG artifact ↔ where we cover it (change detector)
- [CONTRACTS-and-TESTS](spec/docs/CONTRACTS-and-TESTS.md) — the contracts and test plan

## Quality & testing

This is AI-assisted code, so correctness is **machine-proven in layers**, not asserted — the full story
is in **[TESTING.md](TESTING.md)**. In short:

- **6 bundle types × `0 errors`** against the **official HL7 validator** (an authority outside the AI).
- **14 byte-for-byte golden baselines** + **48 test cases** (46 CI-enforced + 2 live-integration) (`dotnet test`) — pipeline, CQL three-state,
  transport adapters, fail-loud seams, live engine integration; mirrored by Python tests.
- **3 independent adversarial review passes** (IG conformance, CQL conformance, architecture/security) —
  the layer that catches "plausible but wrong", which unit tests miss.

[![CI](https://github.com/tm731531/nhi-pas-fhir/actions/workflows/ci.yml/badge.svg)](https://github.com/tm731531/nhi-pas-fhir/actions/workflows/ci.yml)

## Principles

- **Never fabricate FHIR fields** — every value is transcribed from the authoritative IG package.
- **Correctness = the official validator at 0 errors**, not "looks right".
- **No real patient data, ever.** All examples use fabricated identifiers.

## FAQ

**What is Taiwan NHI Prior Authorization (事前審查 / TWPAS)?** The 健保署 process where a provider must
get approval before certain drugs/procedures are reimbursed. It is expressed as a FHIR R4 Implementation
Guide, `tw.gov.mohw.nhi.pas`, built on TW Core and HL7 Da Vinci PAS.

**How do I build a valid Taiwan NHI PAS FHIR Bundle?** Describe the case (patient, provider, drug,
diagnosis) and let this library assemble the `Bundle`; every output is checked by the official HL7 FHIR
validator at 0 errors against the pinned IG. See Quickstart.

**What is CQL pre-check / 送前核刪自查?** Running the NHI's own published CQL rules (compiled to ELM)
against your Bundle *before* submitting, to predict Pass / 核刪 (rejection) / 補件 (missing data). See
[cql-explained.md](spec/docs/cql-explained.md).

**Why a CQF-Ruler / HAPI clinical-reasoning server?** Faithful CQL evaluation needs a full engine that
implements operators like `InCodeSystem`; lightweight JS engines do not. The rules live in the server so
updates are a reload, not a code change. See [cql-wiring.md](spec/docs/cql-wiring.md).

**Does submitting mean approval?** No — 收件 ≠ 核准. The receiver accepts (`queued / 審核中`); the actual
adjudication (and 核刪) happens later. That is exactly why the pre-submit self-check exists.

**Is any real patient data used?** No. All identifiers are fabricated; the project never uses real PHI.

## License

[Apache License 2.0](LICENSE).
