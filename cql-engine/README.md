# CQL pre-check rules + a runner — NOT language-locked

This folder holds the official NHI pre-check rules and *a* way to run them. The framework's
optional CQL self-check (`ICqlEngine` in `impl/csharp/src/NhiPasFhir/Core/Cql.cs`) calls an
engine like this one *before* we POST to NHI — so we send only what will pass and hold back
what would be rejected (核刪) or is missing data (補件).

```
cql-engine/
  elm/        ← the rules, LANGUAGE-NEUTRAL. This is the real shared asset.
  js/         ← ONE reference runner (JS). The engine is swappable — see below.
  README.md   ← you are here
```

## Provenance & license of the vendored rules

The `elm/` and `rules/Library-*.json` files are **official artifacts extracted from the NHI CQL
Implementation Guide** (`tw.gov.mohw.nhi.cql`, published by 衛福部/健保署 at build.fhir.org). They are
redistributed here for reference/testing under **NHI/MOHW's own terms**, and are **not** covered by this
repository's Apache-2.0 license (which applies to our code). Re-sync them from the official IG; do not
treat them as our work.

## The engine is not tied to one language

The rules are shipped as **ELM** (CQL compiled to JSON) — a language-neutral format. *Any*
conformant CQL engine can execute the same `elm/` files. We include a JS runner because it
was fastest to stand up, **not** because the implementation must be JS.

| Engine | Language | Where |
|---|---|---|
| **`cql-execution` + `cql-exec-fhir`** | JavaScript | `js/` here (the reference runner) |
| **cqframework `clinical_quality_language`** | Java | Maven Central `info.cqframework:*` (HL7 reference impl) |
| **CQF-Ruler / HAPI clinical-reasoning** | Java (as a server) | docker; C# would just call its API |

`elm/` stays put whichever engine you pick. `js/` is one binding; a future `java/` or a
server adapter would sit beside it and load the same `elm/`.

## Run the JS reference runner

```bash
cd cql-engine/js
npm install                     # installs the pinned engine (3.3.2 / 2.2.0) into node_modules
node run.js <path-to-a-bundle.json>
```

`run.js` loads one rule (`BCAbemaciclibRule1`) plus its dependency closure (`FHIRHelpers`,
`BCCodeConcept`, `BCReusable`) from `../elm/`, executes it against the Bundle you pass, and
prints the named result expressions (核准布林 / 報告總結 / etc.).

To get a Bundle to test with, emit one from the C# side (see `impl/csharp/README.md`).

See `spec/docs/cql-explained.md` (plain-language walk-through) and
`spec/docs/cql-integration-notes.md` (design + the 3 gotchas).

---

## ⚠️ NEEDS UPDATING — read before relying on this

A **snapshot / PoC**, deliberately frozen so the understanding is reproducible. Not
production-ready. Before it can gate real submissions:

1. **`elm/` is a DRAFT snapshot — re-sync it.** Extracted from **`tw.gov.mohw.nhi.cql`
   v0.0.1 (DRAFT)**. The IG is still draft; rules *will* change. Only 4 files are here
   (1 rule + 3 dependencies out of ~77 libraries); the full set is not vendored.

2. **Lock the engine version to the NHI-effective one.** The JS runner pins
   `cql-execution` 3.3.2 / `cql-exec-fhir` 2.2.0. Confirm these agree with whatever NHI
   runs server-side (whichever engine you end up using), so local prediction == adjudication.

3. **Runtime blockers (debugged 2026-09-29).** Two distinct root causes were found by
   feeding a real emitted Bundle through the runner:
   - **(SOLVED) FHIRHelpers cross-library resolution.** BCReusable does
     `include FHIRHelpers version '4.0.1'` with the HL7 path `http://hl7.org/fhir/FHIRHelpers`,
     but the shipped FHIRHelpers library self-identifies under `https://nhicore.nhi.gov.tw/cql`.
     Stock `Repository.resolve` matched neither → `ctx.get('FHIRHelpers')` undefined →
     `functionDefs.filter(...)` crashed at the first `ToInteger`. Fixed in `js/run.js` with a
     `LenientRepository` (falls back to the path's trailing id). Does NOT touch official ELM.
   - **(SUPERSEDED — decided) `InCodeSystem` is unimplemented in cql-execution 3.3.2.**
     The rule tests `diagnosis-code in ICD10CM2023 (or 2014)` via the ELM `InCodeSystem`
     operator; this engine's builder returns `null` for it (verified: `build(InCodeSystem)`
     → null, whereas `InValueSet` builds fine) → the enclosing `Or` gets a null operand and
     crashes. This blocker was resolved by dropping the JS runner for real use and switching
     to the **CQF-Ruler / HAPI clinical-reasoning server**, which implements `InCodeSystem`
     faithfully (verified against a minimal library). The JS runner in `js/` is now kept only
     as a historical reference, not the wired engine. See
     `spec/docs/cql-integration-notes.md` §6.

4. **Wired to C#.** `ICqlEngine` is implemented by `Core/CqfRulerCqlEngine.cs`, which calls
   the CQF-Ruler server's `Library/{ruleId}/$evaluate` operation and feeds the named results
   back into `CqlPreCheck.Interpret()`. Used by the demo; integration tests
   (`CqfRulerIntegrationTests.cs`) are green. See `spec/docs/cql-integration-notes.md` §6.

5. **Document `elm/` provenance per file if the set grows.** All 4 here come from the same
   v0.0.1 package; if rules from multiple versions ever coexist, record which version each
   came from (mixing versions silently mis-predicts).

**In short:** `elm/` is the language-neutral core and already ran to the `ToInteger` point
under the JS runner. Remaining work: (a) re-sync draft ELM, (b) solve `ToInteger`, (c) wire
an engine to C# — engine choice stays open.
