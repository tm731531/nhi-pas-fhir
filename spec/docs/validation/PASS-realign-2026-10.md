# IG re-alignment + full re-validation — 2026-10

Tracked upstream IG versions, bumped the drifted ones, and **re-validated every bundle against the
official HL7 validator** (not a claim — real `validator_cli.jar` runs, `-tx n/a` + the TW Core ICD
tx-patch). Evidence captured below.

## Version moves

| IG | was | now | note |
|---|---|---|---|
| pas (事前審查) | 1.2.6 | **1.2.7** | backward-compatible — assemblers unchanged, 0 errors |
| empd (電子處方箋) | 0.1.0 | **0.2.1** | **breaking** — profiles reworked; `EmpdAssembler` fully re-transcribed from the 0.2.1 official example |
| ci / ngs / base / emr / twidir | — | (already latest) | re-fetched + re-validated, still 0 errors |
| twcore | 0.3.2 | **held at 0.3.2** | every IG still declares a dependency on twcore 0.3.2; twcore 1.0.0 exists but no IG has adopted it, and its ICD CodeSystems are stubs (11/9 concepts vs 0.3.2's complete 96802/78530). Follow when the IGs bump their own dependency. |

## Validator results (all 7 case bundles — 0 errors)

| bundle | IG | result |
|---|---|---|
| cancer-drug | pas 1.2.7 | `Success: 0 errors, 54 warnings, 14 notes` |
| immunologic-agent | pas 1.2.7 | `Success: 0 errors, 68 warnings, 8 notes` |
| catastrophic-illness | ci 1.0.2 | `Success: 0 errors, 8 warnings, 4 notes` |
| ngs | ngs 1.0.0 | `Success: 0 errors, 63 warnings, 25 notes` |
| notifiable-disease | twidir 0.1.1 | `Success: 0 errors, 11 warnings, 3 notes` |
| inspection-check | emr 0.2.0 | `Success: 0 errors, 8 warnings, 6 notes` |
| e-prescription | **empd 0.2.1** | `Success: 0 errors, 30 warnings, 19 notes` |

Warnings/notes are benign (dom-6 narrative, UCUM unvalidatable without a tx server, performer best-practice);
the official examples carry the same ones. `dotnet test` = 91 passed, 1 skipped (CQL-server-gated), 0 failed.

## empd 0.2.1 — what the breaking upgrade changed (all transcribed from `Bundle-bun-01-ep.json`)

Patient `name.use=usual` (min=1) + `pat-id-1` constraint; Encounter `identifier` min=1 + `class` from
`nhi-outpatient-case-type` + `type`/`serviceType`; Practitioner `qualification.identifier.system` fixed to
`https://cdmis.fda.gov.tw`; Composition 6 sections; MedicationRequest `TotalDuration` as `valuePositiveInt`,
`SelfpayStatus=N`, required `dosageInstruction.timing.code`; Medication code system `NHIMedication-cs`;
Organization identifier system `.../empd/CodeSystem/organization-identifier-tw`. **0 fabricated values, 0 TODOs.**

> Fidelity note: `Coverage.payor` references `Organization/org-nhi-ep`, which the official example itself
> leaves dangling (not a bundle entry). Mirrored faithfully rather than fabricating an extra Organization;
> validator still reports 0 errors.

## CQL

CQL IG = `tw.gov.mohw.nhi.cql#0.0.1` (draft). Our engine is aligned to 0.0.1. **Coverage gap (tracked):**
the IG ships ~40 rule Libraries across 乳癌/大腸直腸癌/肝癌 (BC*/CRC*/HCC*); we have seeded only
**BCAbemaciclibRule1** (+ BCReusable/BCCodeConcept/FHIRHelpers). Expanding the seeded rule set is future work.
