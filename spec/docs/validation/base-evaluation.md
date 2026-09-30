# nhi.base (健保署基礎 IG) — evaluation

**Conclusion: nhi.base is a foundation profile library, not an instantiable case type.** There is no
"base bundle" to build; the deliverable for this IG is the evaluation below.

## What it is

- `tw.gov.mohw.nhi.base` 1.0.0, canonical `https://nhicore.nhi.gov.tw/base`.
- Dependencies: `hl7.fhir.r4.core` 4.0.1 · `tw.gov.mohw.twcore` 0.3.2 · `hl7.fhir.us.davinci-pas`
  2.2.1 · terminology 7.3.0 · uv.extensions 5.3.0 — i.e. **the same base as `nhi.pas`**.
- Contents: a `BaseModel` logical model + nine base **profiles** —
  `Patient / Practitioner / Organization / Organization-gene / Coverage / Claim /
  Observation-diagnostic / DocumentReference / Specimen`-**twnhibase**.
- **No `Bundle` profile and no example bundle.** Its `example/` holds individual resources
  (Patient-pat-min, Claim-cla-1, Coverage-cov-min, …), not a document/collection bundle.

## Why there is nothing to "build" as a case

The toolbox's unit of work is a **case → validated Bundle** (`ICaseAssembler` returns a `Bundle`).
nhi.base has no bundle: it is the reusable 健保 profile layer that health-insurance IGs are meant to
build on. None of the six IGs implemented here (`pas / ci / empd / ngs / twidir / emr`) actually
*depends on* `nhi.base` — they inherit **TW Core** (and, for pas, Da Vinci PAS) directly. So there is
no place a base-only bundle would slot into as a user-facing case.

## The base profiles are real / exercisable

The `nhi.base` package ships official example resources for each profile (Patient-pat-min /
Claim-cla-1 / Coverage-cov-min / …). To validate one:

```bash
IG_PKG=.fhir/base-package.tgz tools/validate.sh <a nhi.base example resource>.json
```

(the validator first resolves `nhi.base`'s deps — twcore 0.3.2 + Da Vinci PAS 2.2.1 — which may be a
large one-time download). The base profiles are conformant and could be adopted if a future NHI IG (or
our own case types) chooses to build on `nhi.base` instead of TW Core directly.

## If/when to revisit

If the NHI republishes `nhi.pas` (or a new IG) with a dependency on `nhi.base`, re-point the relevant
assemblers' profiles from `*-twpas` to `*-twnhibase` and add a golden. Until then, building a synthetic
base bundle would be inventing a case the IG does not define — which the repo's #1 rule forbids.
