# Contributing

Thanks for your interest. A few project-specific rules that keep this correct:

1. **Never fabricate FHIR values.** Every element / binding / fixed value must be
   transcribed from the authoritative IG package. Unverified detail → a `TODO` with the IG
   URL, never a guess. A plausible-but-wrong value silently causes 核刪 (payment clawback).
2. **Correctness is proven, not asserted.** Outputs must pass the official HL7 FHIR
   validator at **0 errors**; `dotnet test` (byte-for-byte golden regression) must be green.
3. **No real patient data, ever.** Samples use fabricated identifiers only.
4. Work on `dev` (not `main`). Keep code/comments/commits in English; 健保 domain terms stay
   in原文 (事前審查, profile/code names) — they are the real identifiers.

## Build & test

```bash
tools/fetch_validation_assets.sh                 # IG package + official validator (large, gitignored)
cd impl/csharp && dotnet test                    # contract + golden-regression tests
tools/validate.sh impl/csharp/build/pa-bundle.cs.json   # 0 errors = conformant
```
