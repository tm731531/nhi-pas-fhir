# NhiPasFhir (C#) — Taiwan NHI 事前審查 FHIR library

A C# implementation of the framework, built on the **Firely .NET SDK** (`Hl7.Fhir.R4`). It follows the
same contract as the language-agnostic `spec/`: it produces a FHIR `Bundle` that passes the **official
HL7 FHIR validator at 0 errors**.

## Architecture (Interface → Abstract → Implementation → Factory)

```
ICaseAssembler                      interface  — Assemble(PACase) -> Bundle
  └ AbstractCaseAssembler           abstract   — shared TW Core clinical builders + default Claim-twpas Assemble
      ├ CancerDrugAssembler         concrete   — 癌藥 case, 9 resources (uses the default Assemble template)
      └ ImmunologicAssembler        concrete   — 免疫製劑 case, 36 resources (overrides Assemble: different
                                                 Claim/Bundle profiles + full SOAP note + evidence chain)
AssemblerFactory                    registry   — ForCase(pacase) picks the impl; unknown = throws
NhiPas                              facade     — Build(pacase) / ToJson(resource)
```

Adding a case type = one `AbstractCaseAssembler` subclass + register it. A case whose Claim/Bundle
profiles match the default gets it for free (cancer-drug); a divergent case overrides `Assemble` and
reuses the shared clinical builders (immunologic). Core/interface/factory untouched.

Both cases validate at **0 errors** (structural + terminology). Immunologic needs the ICD terminology
patch that `tools/fetch_validation_assets.sh` builds — see `spec/docs/validation/PASS-immunologic-2026-09-28.md`.

## Use it (3 lines)

```csharp
using NhiPasFhir;

Bundle bundle = NhiPas.Build(Samples.CancerDrugCase());  // or your own PACase
string json   = NhiPas.ToJson(bundle);                   // FHIR JSON
File.WriteAllText("pa-bundle.json", json);
```

Build your own case:

```csharp
var pacase = new PACase(
    Ig: "tw.gov.mohw.nhi.pas#1.2.6", CaseType: "cancer-drug",
    Patient:  new Dictionary<string,string>{ ["id_card"]="A123456789", ["name"]="…", ["gender"]="male", ["birth_date"]="1965-03-02" },
    Provider: new Dictionary<string,string>{ ["doctor_id_card"]="…", ["doctor_name"]="…", ["hospital_code"]="0101090517" },
    Vitals:   new Dictionary<string,double>{ ["weight_kg"]=68, ["height_cm"]=172 },
    Created:  "2026-09-28T09:00:00+08:00",
    Data:     new Dictionary<string,object>{ ["drug_code"]="…", ["diagnosis_icd"]="C90.00", /* … */ });
var bundle = NhiPas.Build(pacase);
```

## Build, test, validate

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet build                                   # builds the lib
dotnet test                                    # xunit contract tests (factory/shape/serialization)
dotnet run --project samples/Emit              # writes ../build/pa-bundle.cs.json
# official validator gate (from repo root): 0 errors = conformant
make validate FILE=impl/csharp/build/pa-bundle.cs.json
```

## Contract (same as every impl)
"Done" = xunit tests green **and** the emitted bundle passes the official validator at **0 errors**
(structural + terminology). No fabricated FHIR values — all from the pinned IG package `#1.2.6`.
No real patient data.
