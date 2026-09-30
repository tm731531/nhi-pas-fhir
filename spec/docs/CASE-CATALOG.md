# CASE CATALOG — all case types, states, flows, and the class hierarchy

> Goal: one place that enumerates **every** case type the framework will cover, the **universal
> lifecycle** they share, and the **Interface → Abstract → Implementation** class design that realises them.
>
> ⚖️ **Constitution rule (ratified):** the *structure* below is written up-front (framework readiness),
> but each case type's **detailed flow/fields are filled from its authoritative IG package**, never
> fabricated. Status per case: ✅ verified (validated to 0 errors) · 🔶 partial (profiles seen) ·
> ⬜ registered, IG not yet ingested.

## 1. Universal lifecycle (shared by all NHI 事前審查-style cases)

```
 [Capture]      caller fills a PACase (neutral, non-FHIR)
     │
 [Assemble]     Factory → case Assembler → FHIR Bundle
     │
 [Pre-check]    our rules (drug↔indication, required reports…)  ── fail ─▶ fix, back to Assemble
     │ pass                                                       (block 核刪 before submit)
 [Validate]     official HL7 validator (structural + terminology) ── errors ─▶ fix
     │ 0 errors
 [Submit]       upload Bundle to NHI (共通傳輸平台)
     │
 [Adjudicate]   NHI reviewer
     ├─ 核准 (approved)         → done
     ├─ 駁回 (rejected)         → 申復 / 爭議審議 → resubmit
     └─ 補件 (need more info)   → 送核補件 → resubmit
```

**States** (a case moves through these): `draft → assembled → prechecked → validated → submitted →
adjudicated{approved|rejected|need_info} → (resubmit) …`
**Filing categories** (`Claim.subType`): 送核 · 送核補件 · 申復 · 申復補件 · 爭議審議.
**Application categories** (`Claim.priority`): 一般事前審查 · 自主審查 · 緊急報備.

## 2. Case-type catalog

### 2a. Within the pas IG (`tw.gov.mohw.nhi.pas`)
| Case type | Profiles / notes | Status |
|---|---|---|
| **癌藥 (cancer drug)** | Bundle/Claim/MedicationRequest-apply TWPAS + supporting reports; drug↔適應症 invariants | ✅ verified (0 errors) |
| **免疫製劑 (immunologic agent)** | Bundle-immunologic-agent-twpas: **36 resources** — full SOAP (Composition-opd + Observation subjective/objective + ClinicalImpression + CarePlan), two Encounters, blood group, allergy, imaging (DiagnosticReport-image + ImagingStudy + Media), exam/lab (incl. CBC components) evidence, procedure/substance + phototherapy, treatment/patient assessment, and **two applied drugs** | ✅ **verified (0 errors, full terminology)** — C# `ImmunologicAssembler` overrides `Assemble` (its Claim/Bundle profiles differ from 癌藥), reuses the shared TW Core layer. Needed a **terminology patch**: TW Core 0.3.2 ships the ICD CodeSystems with a wrong `/ValueSet/` canonical url that breaks CLOSED slicing — the official example itself trips on this; we correct the url and load it via `-ig` (see `docs/validation/`). |
| **自主審查 (self-assessment)** | ClaimResponse Self Assessment TWPAS (心/肝移植) | ✅ verified (0 errors) — reproduces golden Bundle-bun-self (29 resources), priority_code=3 + auto-embedded ClaimResponse-self-assessment-twpas |
| **申復 (appeal)** | Claim.subType=申復 (3) carrying the original acceptance number (old_acpt_no) | ✅ verified (0 errors) — reproduces golden Bundle-bun-3 (28 resources), subtype_code=3 + old_acpt_no invariant |

### 2b. Other NHI IGs (nhicore.nhi.gov.tw)
| Case type | IG | Status |
|---|---|---|
| 重大傷病 (catastrophic illness) | `tw.gov.mohw.nhi.ci` (TWCI) | ⬜ registered; ingest IG |
| 醫療保險理賠 (insurance claims) | NHI claims IG | ⬜ registered; ingest IG |
| 預檢規則 (pre-check rules, CQL) | NHI CQL IG | ⬜ feeds the Pre-check step (rules-as-code) |

### 2c. MOHW IGs (twcore.mohw.gov.tw) — the broader family
| Case type | IG | Status |
|---|---|---|
| 電子病歷交換 EMR (出院病摘/門診/檢驗/影像/處方/調劑) | EMR-IG | ⬜ registered; ingest IG |
| **長期照顧 (LTC)** ← mission market | 臺灣長期照顧實作指引 | ⬜ registered; ingest IG |
| 傳染病檢驗報告 | infectious-disease IG | ⬜ registered |
| 電子處方箋與調劑 | e-prescription IG | ⬜ registered |

All inherit **TW Core** → the shared clinical layer is reused across every case type.

## 3. Class hierarchy (Interface → Abstract → Implementation)

The pattern you specified, realised per language (Python shown; C#/Java mirror it):

```
ICaseAssembler                         « interface / Protocol »   — the contract callers depend on
   ├─ ig: str,  case_type: str
   └─ assemble(case: PACase) -> Bundle

AbstractCaseAssembler(ICaseAssembler)  « abstract base class »     — shared behaviour, no duplication
   ├─ builds Patient/Practitioner/Organization/Encounter/Coverage (TW Core layer)   [concrete, shared]
   ├─ wires Bundle fullUrls + relative refs                                          [concrete, shared]
   ├─ weight/height supportingInfo                                                   [concrete, shared]
   ├─ @abstract build_claim(case, refs) -> Claim                                     [each case fills]
   ├─ @abstract build_items(case, refs) -> list[Item]                               [each case fills]
   └─ @abstract required_supporting_reports(case) -> list[Report]                    [each case fills]

CancerDrugAssembler(AbstractCaseAssembler)     « implementation »  ✅  — today's validated logic
ImmunologicAssembler(AbstractCaseAssembler)    « implementation »  🔶  — stub, fill from IG
LtcAssembler(AbstractCaseAssembler)            « implementation »  ⬜  — later
…                                                                    (one class per case type)

AssemblerFactory   registry (ig, case_type) -> ICaseAssembler       — picks the impl; caller stays generic
```

**Lifecycle of the library** = caller builds a `PACase` → `AssemblerFactory.for(case)` → an
`AbstractCaseAssembler` subclass does the shared work + its case-specific overrides → `Bundle` → the
same Pre-check + official Validator gate. Adding a case type = **one new `*Assembler` subclass +
register it**; the interface, factory, abstract base, TW Core layer, and validator gate are untouched.

## 4. Definition of done, per case type
A case-type implementation is "done" only when it **reproduces its golden bundle in
`spec/reference-bundles/` and passes the official validator at 0 errors** (structural + terminology).
Until an IG is ingested and a case validated, it stays a registered stub — honest, not fabricated.
