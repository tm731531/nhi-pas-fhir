# CQL / ELM execution sidecar

This is the **real CQL engine** that the framework's optional CQL self-check
(`ICqlEngine` in `src/NhiPasFhir/Core/Cql.cs`) is meant to call. It is a **JavaScript
sidecar**, not part of the .NET library, because .NET has no mature native CQL engine.

- **What it does**: loads the official NHI pre-check rules (compiled to *ELM*) and runs
  them against a FHIR Bundle we produced, *before* we POST to NHI — so we send only what
  will pass, and hold back what would be rejected (核刪) or is missing data (補件).
- **Engine**: [`cql-execution`](https://github.com/cqframework/cql-execution) (the
  reference JS engine, from cqframework — the same group that maintains the CQL spec) +
  [`cql-exec-fhir`](https://github.com/cqframework/cql-exec-fhir) (feeds a FHIR Bundle in).
- **Why it lives here and not in `/tmp`**: this is the proof-of-concept from the CQL
  understanding session (2026-09-29). It is kept in the repo so the engine, its pinned
  versions, and the sample rules travel with the project. See
  `spec/docs/cql-explained.md` (the plain-language walk-through) and
  `spec/docs/cql-integration-notes.md` (the design + the 3 gotchas).

## Run it

```bash
cd impl/csharp/cql-engine
npm install                     # installs the pinned engine (3.3.2 / 2.2.0) into node_modules
node run.js <path-to-a-bundle.json>
```

`run.js` loads one rule (`BCAbemaciclibRule1`) plus its dependency closure
(`FHIRHelpers`, `BCCodeConcept`, `BCReusable`) from `elm/`, executes it against the
Bundle you pass, and prints the named result expressions (核准布林 / 報告總結 / etc.).

To get a Bundle to test with, emit one from the C# side (see `impl/csharp/README.md`) or
point it at a file under `samples/`.

---

## ⚠️ THIS SIDECAR NEEDS UPDATING — read before relying on it

It is a **snapshot / PoC**, deliberately frozen so the understanding is reproducible. Do
**not** treat it as production-ready. Before it can gate real submissions, these must be done:

1. **The `elm/` files are a DRAFT snapshot and must be re-synced.**
   They were extracted from the official CQL IG package **`tw.gov.mohw.nhi.cql` v0.0.1
   (DRAFT)**. The IG is still draft — the rules *will* change. Re-download the package and
   regenerate `elm/` when a newer version ships. Only 4 files are here (1 rule + 3
   dependencies out of ~77 libraries); the full rule set is not vendored.

2. **The engine version is pinned but not yet locked to the NHI-effective version.**
   `cql-execution` 3.3.2 / `cql-exec-fhir` 2.2.0. Confirm these match whatever NHI runs
   server-side, so our local prediction and their adjudication agree.

3. **Unsolved runtime blocker: `FHIRHelpers.ToInteger`.** In the PoC the engine loads all
   defines and starts executing, then fails inside `FHIRHelpers.ToInteger(...)` — a
   data-adapter / model-version alignment issue between `cql-exec-fhir` and the ELM's
   expected type representation. This is the one thing standing between "loads + starts"
   and "runs a rule to a verdict". It is a known, bounded problem — not a dead end.

4. **Not yet wired to C#.** `ICqlEngine` in the .NET side is still `NotWiredCqlEngine`
   (fail-loud). Wiring means: C# spawns this sidecar (or a small HTTP wrapper of it),
   passes `(ruleId, Bundle)`, and reads back the named expressions. See
   `spec/docs/cql-integration-notes.md` §5.

5. **`elm/` provenance must be documented per file if the set grows.** Right now all 4
   files come from the same v0.0.1 package; if rules from multiple versions ever coexist,
   record which version each came from (mixing versions silently mis-predicts).

**In short:** the engine is real and already ran to the `ToInteger` point on this machine.
The remaining work is (a) re-sync draft ELM, (b) solve `ToInteger`, (c) wire it to C#.
