# 01 — Workflow (流程)

> SoT: <https://nhicore.nhi.gov.tw/pas/> · IG v1.2.6

## 1. End-to-end flow

```
 Provider (院所)                         NHI (健保署)
 ─────────────                          ───────────
 1. Build Bundle TWPAS
    (package clinical evidence
     into one FHIR transaction)
 2. Upload via shared transport  ─────▶  3. Format validation
    platform (共通傳輸平台)                  ├─ pass → "upload success"
                                            └─ fail → "upload fail" (OperationOutcome TWPAS)
 4. Query upload result          ◀────▶
                                         5. Reviewer (審核醫師) adjudicates
                                            ├─ approve  (核准給付)
                                            ├─ reject   (駁回)
                                            └─ need more info (要求補件)
 6. Query review result          ◀────▶  → returns Bundle Response TWPAS
                                            (contains ClaimResponse TWPAS)
```

Request side  = **Bundle TWPAS** (癌藥) / **Bundle Immunologic Agent TWPAS** (免疫製劑).
Response side = **Bundle Response TWPAS** wrapping **ClaimResponse TWPAS**.
System messages = **OperationOutcome TWPAS**.

## 2. Case category — 申請案件類別 (`Claim.priority`)

| Code (概念) | Meaning |
|---|---|
| 一般事前審查申請 | Normal prior-auth application |
| 自主審查 | Provider self-assessment (e.g. heart / liver transplant) → `ClaimResponse Self Assessment TWPAS` |
| 緊急報備 | Emergency: administer first, report/审 after |

ValueSet: `NHI-健保事前審查-申請案件類別值集`.

## 3. Filing category — 申報類別 (`Claim.subType`)

The submit ⇄ resubmit ⇄ appeal loop:

```
送核 ──▶ (rejected / need info) ──▶ 送核補件
      ──▶ 申復 ──▶ 申復補件
      ──▶ 爭議審議
```

ValueSet: `NHI-健保事前審查-申報類別值集`.

## 4. Immunologic agents — the S/O/A/P note

免疫製劑 applications carry a structured clinical note (SOAP):

| Part | Profile |
|---|---|
| S — 主觀描述 | `Observation Subjective TWPAS` |
| O — 客觀描述 | `Observation Objective TWPAS` |
| A — 評估 | `ClinicalImpression TWPAS` |
| P — 計畫 | `CarePlan TWPAS` |

Plus `Composition OPD TWPAS` (門診病歷), `AllergyIntolerance TWPAS` (過敏史),
`Observation Blood Group TWPAS` (血型).

## 5. What goes inside a request Bundle (癌藥, typical)

Assembled per the *Logical Model* 「申請(Apply)癌症用藥事前審查之資料模型」:

- `Claim TWPAS` — the application + indication conditions (the spine)
- `MedicationRequest Apply TWPAS` — item(s) being applied for
- `MedicationRequest Treat TWPAS` — actual treatment medication
- `Patient TWPAS`, `Practitioner TWPAS`, `Organization TWPAS`
- `Condition TWPAS` — 主要疾病 + 共病 (comorbidity)
- `Observation Cancer Stage TWPAS` — TNM / FIGO / CNS staging
- `Observation Patient Assessment TWPAS` — CTCAE / NYHA / PDAI …
- `Observation Laboratory Result TWPAS`, `Observation Diagnostic TWPAS` (基因)
- `DiagnosticReport TWPAS` / `DiagnosticReport Image TWPAS`, `ImagingStudy TWPAS` / `Media TWPAS`
- `Procedure TWPAS` (放射治療) / `Procedure Phototherapy TWPAS` (照光治療) + `Substance*`
- `Coverage TWPAS` (健保事前審查計畫), `DocumentReference TWPAS` (治療計畫/基因報告)

Full per-resource field mapping: see `03-artifacts-catalog.md` (and the IG Logical Models).

## 6. Key validation constraint (the clawback trap)

Codes are cross-constrained. Example from the IG: drug code `KC010892B5` ⇒ indication code
**must be one of** `C50P1, C50P2, C50P3, C50P4, C50P5, C50R1, C16R1`.

Wrong pairing → rejection / 核刪 (payment clawback). Pre-submit rule checking is the highest-value
feature; the IG references a **"預檢規則 (FHIR CQL)"** for exactly this.
