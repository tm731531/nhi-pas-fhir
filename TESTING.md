# Testing & how to trust AI-assisted code here

This project is built with substantial AI assistance. The danger with AI-generated code — especially
FHIR — is output that is **plausible but wrong**: it compiles, it looks right, and it silently produces
a bundle the NHI will 核刪 (claw back payment for). So correctness here is never asserted; it is
**machine-proven, in layers**, against an authority outside the AI. This page is that story.

Run everything with: `dotnet test impl/csharp/NhiPasFhir.sln` (92 test cases: 89 CI-enforced + 3 live-integration). See also the detailed
plan in [`spec/docs/CONTRACTS-and-TESTS.md`](spec/docs/CONTRACTS-and-TESTS.md).

## The layers (weakest guarantee at the bottom, strongest at the top)

### 5. Independent AI review audits — catches "plausible but wrong"
The distinctly-AI-project layer. The outputs were put through **three independent adversarial review
passes** (each by separate agents, told to *refute*, not rubber-stamp):
- **IG conformance** — re-ran the official validator on every bundle, diffed the structures against the
  official examples, and re-audited every system URL/code against the IG package. (0 findings.)
- **CQL conformance** — drove the *live* rule engine, flipped a case to a renewal, and proved the
  補件/核刪 sub-classifier had to read the rule's own report (fixed).
- **Architecture / security** — async correctness, thread-safety, and a full history scan before going public.

Unit tests can't catch a confidently-wrong interpretation of a spec; an adversarial second reader can.
This layer is why that class of bug was found and fixed.

### 4. External ground truth — the official HL7 FHIR validator (0 errors)
Every emitted bundle is validated against the **official HL7 validator** with the pinned IG
(`tw.gov.mohw.nhi.pas#1.2.7` + TW Core + Da Vinci PAS). **7 bundle types, all `Success: 0 errors`.**
This is an authority *outside* this codebase and outside the AI — it cannot be talked into passing.
Evidence: [`spec/docs/validation/`](spec/docs/validation/). Reproduce: `tools/validate.sh <bundle>`.

### 3. Golden byte-for-byte regression — catches drift
**14 golden baselines** capture the exact, validator-clean JSON of every case (cancer, immunologic,
appeal, self-assessment, uuid-style, response, outcome, + 7 value variants). Any change to the
assembled output — including an AI "improvement" — fails the test until the golden is deliberately
re-blessed. This is what stops silent regressions between edits. (`GoldenTests`, `CaseVariantsTests`.)

### 2. Contract & behaviour tests
- **Pipeline** (產→驗→查): blocking vs clear, assembly, entry counts. (`PipelineTests`)
- **CQL three-state interpretation**: pass / 會被核刪 / 缺資料待補 / NotEvaluated, including the subtle
  cases (a true verdict beats a false alternative leg; a missing verdict is never defaulted to 核刪;
  when both sections appear, 核刪 wins and no reason is dropped). (`CqlTests`)
- **Transport adapters** (送/回/查): the CQL engine and the submitter, with a stub `HttpMessageHandler`
  — HTTP-error fail-loud, OperationOutcome fail-loud, value mapping, 收件≠核准 status. (`TransportTests`)
- **Fail-loud seams**: `NotWiredCqlEngine` / `AssemblerFactory` throw with actionable messages. (`FrameworkTests`)

### 1. Live end-to-end integration
`CqfRulerIntegrationTests` runs the real CQL engine (CQF-Ruler :8095) against a real emitted bundle.
It uses **`[SkippableFact]`** — when the server is absent the test reports as **SKIPPED, not passed**
(honest: a no-op that counts as green would lie), so CI stays green without the sidecar while telling
the truth; the same adapter shape is covered offline by the transport stubs above.

## Two languages, one source of truth
`impl/python/tests/` mirrors the core contracts (framework, pipeline, pre-check, bundle, smoke) so the
language-agnostic `spec/` is proven by more than one implementation. **Both run in CI** — a `test` job
(`dotnet test`) and a `python` job (`pytest`) — so neither language's contract can regress unnoticed.

## What is deliberately NOT claimed
- The **CQL rules IG is v0.0.1 DRAFT**; the vendored rules are a snapshot and the drug→rule map currently
  covers the worked sample rule — so CQL coverage is one rule end-to-end, not all ~66 yet.
- **長照** is designed-for but not implemented, so it has no tests.
Honest test coverage means saying where the proof stops.
