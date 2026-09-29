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

## Remaining (bounded)

`Library/$evaluate` loads source from **CQL text (`text/cql`)**, not our raw `application/elm+json`
(the real rule errors `Could not load source … version null`; a text/cql library succeeds). We
vendored only ELM. **Next:** fetch the rules' `.cql` from the official `tw.gov.mohw.nhi.cql`
package, vendor it, then the proven text/cql path runs the real rule to a verdict. Then wire the
C# `ICqlEngine` to POST our Bundle here and read back the named expressions.

Nothing here is wired to C# yet (`ICqlEngine` is still `NotWiredCqlEngine`).
