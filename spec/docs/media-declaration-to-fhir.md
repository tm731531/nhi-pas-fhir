# 健保「醫療費用申報」媒體/XML 格式 ↔ FHIR mapping

> The mainstream, daily NHI billing format (媒體申報 / XML 申報) mapped to FHIR R4. This is the
> **legacy world → new world** bridge from `nhi-fhir-migration.md` §1 (world ①→②), made concrete.
>
> **Authoritative source (fields transcribed verbatim, never guessed):**
> 「特約醫事服務機構門診醫療費用點數申報格式及填表說明」(XML 檔案格式), 版更日期 **112.08.25**,
> 中央健康保險署 → 醫療費用XML申報格式 (`nhi.gov.tw/ch/np-2756-1.html` →
> 點數申報格式及填表說明 `cp-5367-8d6c5-2757-1.html`). This doc covers the **門診 (outpatient)**
> format; 住院 (inpatient) and 交付機構 have sibling specs with the same 3-segment shape.
>
> **Honest scope:** there is **no published FHIR IG** for 健保費用申報 (billing). So the FHIR target
> here is **base R4 `Claim` (use = `claim`)** on TW Core, **not** a conformance-validated profile
> (unlike 事前審查, which has `Claim-twpas`). Correctness = valid base-R4 FHIR (accepted by a real
> server / `-ig` off validator), plus a faithful, source-cited field mapping. Code tables not yet
> transcribed are marked **TODO** with the note number in the source. **Nothing is fabricated.**

---

## 1. The shape: 3 segments → a FHIR Claim graph

The 媒體申報 record is **three segment types**, nested one-to-many:

```
總表段 (t)  一份申報檔一個            → submission envelope   (Bundle / one summary Claim header)
  └ 點數清單段 (d)  一筆就醫案件一個      → ONE Claim (use=claim) + Patient + Encounter + Condition(s)
       └ 醫令清單段 (p)  一條醫令一個     → ONE Claim.item  (+ MedicationRequest / Procedure)
```

- **總表段 (t)** = the batch header: who is filing (服務機構), for which 費用年月, and per-category
  totals (件數/點數). FHIR-side it is the **submission envelope** — a transaction `Bundle`, or a
  summary — not clinical data.
- **點數清單段 (d)** = one **就醫案件** (a patient's visit). This is the natural **`Claim`**: it carries
  patient identity, dates, diagnoses, procedures, the treating clinician.
- **醫令清單段 (p)** = one **醫令** (order line: a drug, a procedure, a material). Each becomes a
  **`Claim.item`**, and, when clinically meaningful, a **`MedicationRequest`** (drugs) or
  **`Procedure`** (診療/處置).

---

## 2. 總表段 (t) → submission envelope

| 欄位 | 資料名稱 | 長度/屬性 | → FHIR |
|---|---|---|---|
| t1 | 資料格式 | 2 X (門診=`10`) | Bundle context / a code on the envelope |
| t2 | 服務機構代號 | 10 X (衛福部編定) | filing `Organization.identifier` |
| t3 | 費用年月 | 5 X (ROC YYYMM) | reporting period |
| t4 | 申報方式 | 1 X (1書面/2媒體/3連線) | metadata |
| t5 | 申報類別 | 1 X (1送核/2補報) | metadata (補報 ⇒ see d12 補報原因) |
| t6 | 申報日期 | 7 X (ROC YYYMMDD) | submission date |
| t7…t36 | 各案件分類 件數/點數 | 9 | **derived totals** — recompute from the d/p entries, don't map inbound |
| t37/t38 | 申請件數/點數總計 | 9 | derived total |
| t39/t40 | 部分負擔件數/點數總計 | 9 | derived (sum of d-level 部分負擔) |

> t7–t40 are **aggregates** of the detail segments. Converting **d/p → FHIR**, ignore them on the way
> in and **recompute** them on the way out (FHIR → 媒體申報). Trusting inbound totals hides errors.

---

## 3. 點數清單段 (d) → `Claim` + `Patient` + `Encounter` + `Condition`

| 欄位 | 資料名稱 | 長度/屬性 | → FHIR |
|---|---|---|---|
| d1 | 案件分類 | 2 X | `Claim.subType` / `Claim.type` — **code table TODO (註11/註19)** |
| d2 | 流水編號 | 6 9 | `Claim.identifier` (per 案件分類 sequence) |
| **d3** | **身分證統一編號** | 10 X | **`Patient.identifier`** (國民身分證; 外籍→居留證/護照; 檢核原則 in spec) |
| **d11** | **出生年月日** | 7 X (ROC) | **`Patient.birthDate`** (ROC→西元, see §6) |
| d8 | 就醫科別 | 2 X | `Encounter.serviceType` / `Claim.item.category` — **code table TODO (註13)** |
| **d9** | **就醫日期** | 7 X (ROC) | **`Encounter.period.start` / `Claim.billablePeriod.start`** |
| d10 | 治療結束日期 | 7 X (ROC) | `Encounter.period.end` / `Claim.billablePeriod.end` |
| d14 | 給付類別 | 1 X | `Coverage` / `Claim.insurance` — **code table TODO** |
| d15 | 部分負擔代號 | 3 X | copay context — **code table TODO (註10)** |
| **d19** | **主診斷代碼** | 9 X (ICD-10-CM, 小數點免填) | **`Claim.diagnosis[0]` (sequence 1, `type=principal`) + `Condition`** |
| d20–d23 | 次診斷代碼(一)…(四) | 9 X | `Claim.diagnosis[1..]` + `Condition` (secondary) |
| d24 | 主手術(處置)代碼 | 9 X | `Claim.procedure[0]` + `Procedure` — ICD-10-PCS/處置碼 |
| d25–d26 | 次手術(處置)代碼 | 9 X | `Claim.procedure[1..]` + `Procedure` |
| d18 | 病患是否轉出 | 1 X | `Encounter.hospitalization.dischargeDisposition` (referral out) |
| d16/d17 | 轉診/處方調劑/特定檢查 共享註記 + 服務機構 | 2 X / 10 X | `Claim.referral` / `Encounter` — the upstream 醫事機構代號 |
| d27 | 給藥日份 | 3 9 | supporting info (連續處方 day supply) |
| d28 | 處方調劑方式 | 1 X (1交付調劑…) | `MedicationRequest.dispenseRequest` context |
| d29 | 就醫序號 | 4 X (健保IC卡就醫序號) | `Encounter.identifier` (IC-card visit seq) |
| **d30** | **診治醫事人員代號** | 10 X | **`Practitioner.identifier` → `Claim.careTeam` (role=primary)**  |
| d31 | 藥師代號 | 10 X | `Practitioner` (pharmacist) → `Claim.careTeam` |
| d4–d7 | 特定治療項目代號(一)…(四) | 2 X | `Claim.supportingInfo` — **code table TODO** |
| d12 | 補報原因註記 | 1 X | `Claim` note when t5=2 補報 — **code table TODO** |
| d32–d34 | 用藥/診療/特材 點數小計 | 8 9 | **derived** from p12 by p3 類別 — recompute, don't trust inbound |

---

## 4. 醫令清單段 (p) → `Claim.item` (+ `MedicationRequest` / `Procedure`)

| 欄位 | 資料名稱 | 長度/屬性 | → FHIR |
|---|---|---|---|
| **p3** | **醫令類別** | 1 X | **switches the resource type** — `0`診察費 / `1`用藥明細→`MedicationRequest` / `2`診療明細→`Procedure` / `3`特殊材料→`Device`/`SupplyRequest` / `4`不得另計價 / `9`藥事服務費 |
| **p4** | **藥品(項目)代號** | 12 X | **`Claim.item.productOrService`** (健保藥品/支付標準碼); if 用藥→`MedicationRequest.medication` |
| p5 | 藥品用量 | 7 9 | `Dosage.doseAndRate` (健保藥品使用標準碼) |
| p7 | 藥品使用頻率 | 18 X | `Dosage.timing` (使用標準碼) |
| p9 | 給藥途徑/作用部位 | 4 X | `Dosage.route` (使用標準碼) |
| p1 | 藥品給藥日份 | 3 9 | `MedicationRequest.dispenseRequest.expectedSupplyDuration` |
| p6 | 診療之部位 | 18 X | `Procedure.bodySite` (牙科 FDI 齒位碼) |
| p8 | 支付成數 | 6 9 | `Claim.item.factor` (加成/折扣) |
| **p10** | **總量** | 7 9 | **`Claim.item.quantity`** |
| p11 | 單價 | 10 9 | `Claim.item.unitPrice` (點值) |
| **p12** | **點數** | 8 9 (總量×單價, 加成取整) | **`Claim.item.net`** |
| **p13** | **醫令序** | 3 9 | **`Claim.item.sequence`** (order within the 案件) |
| p14/p15 | 執行時間 起/迄 | 11 X (ROC+time) | `Procedure.performedPeriod` |
| p16 | 執行醫事人員代號 | 10 X | `Procedure.performer` / `Claim.careTeam` (執行者) — 必填 for 醫令 in 註32 |
| p2 | 醫令調劑方式 | 1 X | dispense context — **code table TODO** |

---

## 5. Which FHIR resources one 案件 (d + its p-lines) produces

```
點數清單段 d   ──►  Claim (use=claim)         ← the billing case
                    ├─ Patient   (d3 身分證, d11 生日)
                    ├─ Encounter (d9/d10 日期, d8 科別, d29 就醫序號)
                    ├─ Condition × (d19 主 + d20-23 次)      → Claim.diagnosis[]
                    ├─ Procedure × (d24 主 + d25-26 次)       → Claim.procedure[]
                    ├─ Practitioner (d30 醫師, d31 藥師)      → Claim.careTeam[]
                    └─ Organization (t2 filer, d17 上游機構)
醫令清單段 p × N ─►  Claim.item[] (p13 序, p4 碼, p10 量, p12 點)
                    ├─ MedicationRequest  (p3=1 用藥: p4 藥碼, p5/p7/p9 用法)
                    └─ Procedure          (p3=2 診療: p4 處置碼, p6 部位)
```

This is the **same resource family** the 事前審查 (pas) side already assembles — Patient / Encounter /
Condition / MedicationRequest / Claim — so the C# assemblers and the CQL pre-check can be reused. The
difference is only `Claim.use` (`claim` here vs `preauthorization` for pas) and the profile (base R4
here vs `Claim-twpas` there).

---

## 6. ROC date conversion (verified from the spec)

Every date field (t3/t6, d9/d10/d11, p14/p15) is **民國 (ROC) year**, zero-padded, per the spec:
> 第1、2、3碼為民國年份，不足位者前補0（民國99年→`099`）；第4、5碼為月份（5月→`05`）；
> 第6、7碼為日期（9日→`09`）。民國前的年份為負 (d11 生日 note).

`西元 = 民國 + 1911`. So `0990501` (7 X) → `2010-05-01`. **d11 生日** can predate 民國 (民國前),
handled as the spec notes. The converter does this conversion explicitly (§7).

---

## 7. Reference converter

`tools/media-declaration-to-fhir.py` — a reference converter that parses a 門診 media-declaration
record (t / d / p segments, pipe-delimited synthetic sample with **fabricated** identifiers) and emits
a FHIR **transaction Bundle** (Patient + Encounter + Condition + Claim + MedicationRequest). It maps
the **verified** fields above and marks unverified code tables `TODO`. Prove the output is real FHIR:

```bash
python3 tools/media-declaration-to-fhir.py tools/sample-media-declaration.txt > /tmp/claim.json
tools/validate.sh /tmp/claim.json            # base R4 structural (no billing IG exists to bind to)
tools/post-to-public-server.sh /tmp/claim.json   # 真的打: POST to a live FHIR server (no creds)
```

**Direction FHIR → 媒體申報** (for a HIS that keeps FHIR internally and must file with the NHI) is the
inverse of the tables above, plus **recomputing** the t7–t40 totals from the d/p entries. Not yet built.

---

## 8. What is NOT covered (honest TODO)

- **Code tables** behind d1 案件分類, d8 科別, d14/d15 給付/部分負擔, p3 醫令類別 detail, and the
  補報/轉出 codes — each cites a 註 in the source; transcribe on demand, never guess.
- **住院 / 交付機構** formats (sibling specs, same 3-segment shape).
- **申復格式** (醫療費用XML申復格式, `np-2753-1.html`) — the appeal channel, a separate spec.
- A conformance **profile**: there is no published 健保費用申報 FHIR IG, so we bind to base R4 + TW Core
  and validate structurally. If/when the NHI publishes one, swap it in the way pas uses `Claim-twpas`.
