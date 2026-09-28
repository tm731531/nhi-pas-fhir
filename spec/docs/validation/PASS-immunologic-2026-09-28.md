# Validation evidence — 免疫製劑 (immunologic-agent) PA Bundle → 0 errors

**Date:** 2026-09-28 · **IG:** `tw.gov.mohw.nhi.pas#1.2.6` (FHIR R4) · **Impl:** `impl/csharp` (Firely SDK)

## Result

```
impl/csharp/build/pa-bundle-immunologic.cs.json  (36 resources)
Success: 0 errors, 50 warnings, 3 notes     (full terminology, tx.fhir.org + patch)
Success: 0 errors, 61 warnings, 5 notes     (offline: -tx n/a + patch)
```

Reproduce (from repo root, after `tools/fetch_validation_assets.sh`):

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet run --project impl/csharp/samples/Emit          # emits both bundles
tools/validate.sh impl/csharp/build/pa-bundle-immunologic.cs.json   # 0 errors
```

## What the bundle contains (36 resources)

Claim (`Claim-immunologic-agent-twpas`, 2 applied-drug items) + full SOAP note
(`Composition-opd` → Observation subjective/objective + ClinicalImpression + CarePlan) + two
Encounters (`Encounter-opd` clinical visit, `Encounter` the claim anchor) + two Conditions + blood
group + allergy + imaging chain (DiagnosticReport-image + ImagingStudy + Media) + examination report
+ two lab Observations (incl. CBC components) + document references + patient/treatment assessments +
procedure/substance + phototherapy procedure/substance + Coverage + govt Organization.

All values transcribed from the official IG example `Bundle-bun-imm.json` (fabricated, no PHI);
patient/provider/vitals/created come from the `PACase`.

## The terminology patch (why it was needed)

Validating first produced **25 errors** — and so does the **official example itself**
(`Bundle-bun-imm.json` → 25 errors under the same validator). Root cause is an upstream bug in
**TW Core 0.3.2**: `CodeSystem-icd-10-cm-2023-tw.json` (and `-pcs-`) declare a **wrong canonical
`url`** pointing at a `/ValueSet/` path, while the ValueSets those slices bind to include the
`/CodeSystem/` url. The validator therefore cannot resolve the CodeSystem → the ValueSet cannot
expand → the `memberOf` discriminator on `Condition/Procedure/Substance.code` CLOSED slicing cannot
be evaluated → hard "does not match any known slice" errors that cascade up to `Bundle.entry:claim`.

The codes are actually present (`content=complete`: 96 802 ICD-10-CM, 78 530 ICD-10-PCS concepts,
incl. `M17.11`). `tools/fetch_validation_assets.sh` rewrites the `url` to the correct `/CodeSystem/`
canonical and `tools/validate.sh` loads it via `-ig .fhir/tx-patch`. With the patch, `memberOf`
resolves locally and the bundle validates at 0 errors — i.e. **our model is more conformant than the
official example, and validator-proven** (Constitution I). We patch the terminology *environment*, not
our data. See `~/.claude/.../brain/healthcare-fhir-interop.md` for the reusable lesson.
