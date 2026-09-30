# AGENTS.md — working in this repo (for AI coding agents & new contributors)

A fast, actionable map so an agent (or a human) can understand and use this project in minutes.
For an even shorter machine summary see [`llms.txt`](llms.txt); for humans start at [`README.md`](README.md).

## What this project is (30-second version)

A **FHIR R4 reference implementation for Taiwan NHI Prior Authorization (健保 事前審查 / TWPAS)** plus a
**CQL pre-submit rule check**. You describe a case in plain terms → it assembles the exact FHIR `Bundle`
the NHI expects (**0 errors** against the official HL7 validator) → optionally runs the NHI's own
published **CQL** rules to predict **核刪 (clawback) / 補件 (missing data)** before you submit.
Primary implementation is **C# (Firely SDK)**; Python is a reference mirror. It is a payload factory +
validator gate + optional pre-check — **not** a FHIR server.

Flow: **產 (assemble) → 驗 (seed check) → 查 (CQL) → 送 (submit) → 回 (ClaimResponse)**.

## Repo map

| Path | What |
|---|---|
| `spec/` | Language-agnostic source of truth: IG mirror, reference golden bundles, architecture/coverage/traceability docs. |
| `spec/docs/cql-explained.md` · `cql-wiring.md` · `cql-integration-notes.md` | Plain-language + technical CQL docs. |
| `impl/csharp/` | **Primary** implementation (Firely). Library in `src/NhiPasFhir/` (incl. `MediaDeclaration/` — 醫療費用申報 ↔ FHIR), tests in `tests/`, emitter in `samples/Emit`, media CLI in `samples/MediaTool` (`media-tool to-fhir/to-media`). |
| `impl/python/` | Reference implementation (pydantic). |
| `cql-engine/` | The CQL engine: `rules/` (official Library resources, text/cql), `elm/` (compiled), `server/` (CQF-Ruler docker + loader), `js/` (a historical JS runner). |
| `demo/NhiPasDemo/` | ASP.NET Core Razor Pages app consuming the library (the whole flow). |
| `tools/` | `fetch_validation_assets.sh` (validator + IG package), `validate.sh` (official HL7 validator wrapper), `post-to-public-server.sh` (really POST a bundle to a live public FHIR server — no creds), `media-declaration-to-fhir.py` (Python mirror of the C# `media-tool`; 健保 醫療費用申報 → FHIR; sample in `sample-media-declaration.txt`). |
| `docs/` | GitHub Pages: `index.html` landing + `flow.html` interactive diagram. |

## Setup / build / test / run (copy-paste)

The layered test strategy (external validator + goldens + contract/integration + adversarial AI review)
is in [`TESTING.md`](TESTING.md) — read it to understand what proves correctness before you change code.

```bash
# 1) Build + test the library (only .NET 8 SDK needed). 59 test cases (57 CI + 2 live-integration) incl. byte-for-byte golden regression.
dotnet test impl/csharp/NhiPasFhir.sln

# 2) Emit the sample bundles, then validate against the OFFICIAL HL7 validator (larger download).
tools/fetch_validation_assets.sh                        # validator_cli.jar + IG package (gitignored)
dotnet run --project impl/csharp/samples/Emit           # -> impl/csharp/build/*.cs.json
tools/validate.sh impl/csharp/build/pa-bundle.cs.json   # expect: Success: 0 errors

# 3) (optional) Bring up the CQL engine, then the CQL self-check works end-to-end.
cd cql-engine/server && docker compose up -d && node load-libraries.mjs && cd -   # CQF-Ruler on :8095

# 4) Run the demo app.
dotnet run --project demo/NhiPasDemo   # http://localhost:5099
```

## How the pieces fit

- Callers pass a neutral `PACase` (no FHIR types). `NhiPas.Build(case)` → `Bundle`; `Pipeline.RunAsync(case, cql:)`
  runs 產 + 驗 + (optional) 查 and returns a `PipelineResult` (Findings / Blocked / Bundle / three-state `Cql`).
- The CQL check is a **toggle + socket**: `ICqlPreCheck` (`NoCqlPreCheck` off / `CqlPreCheck` on) →
  `ICqlEngine` (`CqfRulerCqlEngine` HTTP adapter / `NotWiredCqlEngine`). Submission is `IPasSubmitter`.
  Rules live in the CQL **server**, not in code — a rule/IG update = reload + re-run the same Bundle.
- Case types self-register: an `ICaseAssembler` implementation registers via a `[ModuleInitializer]`;
  `AssemblerFactory.ForCase(case)` picks it by `CaseType`. Adding a case ≈ add an assembler + a golden test.

## Non-negotiable rules (this repo)

1. **Never fabricate FHIR values.** Every element / binding / fixed value is transcribed from the
   authoritative IG package under `.fhir/`. Unverified detail → a `TODO` with the IG URL, never a guess.
   A plausible-but-wrong value silently causes 核刪. (See [`CONTRIBUTING.md`](CONTRIBUTING.md).)
2. **Correctness is machine-proven**: outputs pass the official validator at **0 errors**; `dotnet test`
   (byte-for-byte golden regression) is green. "It compiles" ≠ "it is correct".
3. **No real patient data, ever** — fabricated identifiers only.
4. **CQL verdicts come from the official rules**, executed by a faithful engine — never re-interpreted by hand.
   The 補件/核刪 split is read from the rule's own 報告總結 output, not re-derived.
5. Work on `dev`. Code/comments/commits in English; keep 健保 domain terms in原文 (事前審查, profile/code names).

## Gotchas worth knowing (learned the hard way)

- **TW Core 0.3.2 ICD CodeSystem `url` bug**: its `url` points at a `/ValueSet/` path, so closed-slicing
  `memberOf` checks fail and cascade — even the official example fails. A terminology patch fixes it locally.
- **CQL Bundle must be a longitudinal record**: continuation/return-visit rules `retrieve` prior history;
  a Bundle with only the current case silently mis-predicts.
- **HAPI/CQF-Ruler loads CQL source from `text/cql`, not raw `elm+json`** ("could not load source" otherwise);
  load the official Library resources (which carry text/cql).
- **`InCodeSystem`** is unimplemented in the JS `cql-execution` engine → we use CQF-Ruler (Java), which implements it.
- C#: `using Hl7.Fhir.Model;` makes `Task` ambiguous with `System.Threading.Tasks.Task` — alias it.

## Where to read first (for an AI agent)

`llms.txt` → `README.md` → `spec/docs/cql-explained.md` (concepts) → `spec/docs/IG-TRACEABILITY.md`
(every IG artifact ↔ where it's covered) → the code under `impl/csharp/src/NhiPasFhir/`.
