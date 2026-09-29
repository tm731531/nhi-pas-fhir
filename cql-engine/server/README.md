# CQF-Ruler server — the faithful CQL engine (option C)

A standalone FHIR server (CQF-Ruler / HAPI FHIR 7.4.2, clinical-reasoning) that runs the
official NHI pre-check rules against our Bundle. Chosen over the JS runner because it faithfully
implements `InCodeSystem`, which `cql-execution` does not (see `../js/` and
`spec/docs/cql-integration-notes.md`).

**Replaceability invariant (Tom):** the contract is *Bundle in → verdict out*. The rules live
in this server, not in our C# code. So a rule/IG update = reload here and re-run the same Bundle
(no C# change); swapping the engine = swap only the `ICqlEngine` HTTP adapter.

## Run

```bash
docker compose up -d                       # HAPI FHIR 7.4.2 on http://localhost:8095/fhir
node load-libraries.mjs                     # wrap ../elm/*.json as Library resources + load them
```

Sanity: `curl -s -X POST http://localhost:8095/fhir/\$cql -H 'Content-Type: application/fhir+json' \
  -d '{"resourceType":"Parameters","parameter":[{"name":"expression","valueString":"5 + 3"}]}'`
→ returns `valueInteger 8`.

## Verified (2026-09-29)

- Engine runs (`$cql` 5+3 → 8).
- **`InCodeSystem` works faithfully**: a `text/cql` library with `Code 'C90.00' from ICD2023 in
  ICD2023` evaluates to `true` via `Library/$evaluate` — no full terminology load needed. This is
  the whole reason for option C.
- `load-libraries.mjs` loads the 4 rule ELM as Library resources (url aligned to the include
  canonical; FHIRHelpers → `http://hl7.org/fhir/FHIRHelpers`).

## End-to-end: DONE (2026-09-29)

- Rules: official `tw.gov.mohw.nhi.cql` Library resources, kept to `text/cql`, vendored in
  `../rules/Library-*.json` (HAPI-CR translates the CQL itself — it loads source from text/cql,
  not our raw elm+json). `load-libraries.mjs` PUTs them.
- `Library/BCAbemaciclibRule1/$evaluate` on our Bundle → 68 defines evaluated, verdict
  `乳癌Abemaciclib申請結果_布林 = false` (correct: our sample is a myeloma case, not breast cancer).
- Wired to C#: `impl/csharp/src/NhiPasFhir/Core/CqfRulerCqlEngine.cs` (`ICqlEngine`) POSTs the Bundle
  here and returns the named expressions; `CqfRulerIntegrationTests.cs` (skips if server down) is green.

Enable it: `Pipeline.Run(case, cql: new CqlPreCheck(new CqfRulerCqlEngine(http, ".../fhir"), drugToRules))`.

## Remaining (non-blocking)

Build the full drug→rule 1:N map (66 rules); assemble longitudinal history into the Bundle for
continuation/return-visit rules; a Pass-shaped sample Bundle if you want to see an approval; and
make this server persistent / host it on your own infra instead of manual localhost.
