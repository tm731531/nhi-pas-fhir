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

3. **Unsolved runtime blocker: `FHIRHelpers.ToInteger`.** In the PoC the JS engine loads
   all defines and starts executing, then fails inside `FHIRHelpers.ToInteger(...)` — a
   data-adapter / model-version alignment issue between the FHIR source and the ELM's
   expected type representation. This is the last mile between "loads + starts" and "runs
   to a verdict". Known and bounded — not a dead end. (This may also be engine-specific:
   a Java engine could behave differently — another reason not to language-lock.)

4. **Not yet wired to C#.** `ICqlEngine` is still `NotWiredCqlEngine` (fail-loud). Wiring
   means: C# invokes an engine (this sidecar, or a Java/server one), passes `(ruleId,
   Bundle)`, reads back the named expressions. See `spec/docs/cql-integration-notes.md` §5.

5. **Document `elm/` provenance per file if the set grows.** All 4 here come from the same
   v0.0.1 package; if rules from multiple versions ever coexist, record which version each
   came from (mixing versions silently mis-predicts).

**In short:** `elm/` is the language-neutral core and already ran to the `ToInteger` point
under the JS runner. Remaining work: (a) re-sync draft ELM, (b) solve `ToInteger`, (c) wire
an engine to C# — engine choice stays open.
