# 健保署「每日上傳 XML」→ FHIR (ingest adapter)

> The **進料 (ingest)** inbound adapter of the 一條龍 (`讀卡 → 進料 → FHIR → 送件`). It parses a real
> NHI format — the 健保署 **每日上傳 XML** (`RECS>REC>MSH/MB1/MB2`, **Big5**) — and feeds the existing
> `MediaDeclarationConverter` to produce a FHIR Bundle. Lives in the **`NhiPasFhir.Ingest`** project,
> which depends on the pure FHIR core; the core never depends on it (Hexagonal boundary). See
> `spec/specs/002-his-ingest-feed/spec.md`.

## Flow

```
每日上傳 XML (Big5)
   └─ NhiUploadXmlParser.Parse(byte[])      → List<NhiUploadRecord>   (MSH/MB1/MB2, faithful shape)
        └─ NhiUploadToMediaRecord.Map(rec)  → MediaRecord            (translate to 媒體申報 t/d/p IDs)
             └─ MediaDeclarationConverter.ToFhir(media)  → Bundle    (reused unchanged)
```

`NhiUploadPipeline.ToFhir(byte[])` wires all three and returns the first REC's Bundle.

## Why a field-ID translation (not pass-through)

每日上傳 (MSH/MB1/MB2) and 媒體申報 (t/d/p) use **different field numbering** (e.g. 每日上傳 `MB2.p1` =
醫令類別 vs 媒體申報 `p3` = 醫令類別). The mapper translates explicitly into the 媒體申報 vocabulary so the
existing converter is reused untouched. Every row below cites its source; fields with **no faithful
target are intentionally NOT mapped** (mapping them would require inventing a value — forbidden by the
repo's #1 rule).

| 每日上傳 (source: his_import.go) | → 媒體申報 (converter) | FHIR element |
|---|---|---|
| `MSH.h1` 醫事機構代號 | `t2` 服務機構代號 | Organization.identifier |
| `MSH.h2` 費用年月 (YYYMM) | `t3` 費用年月 | — (carried, converter uses t6 for Claim.created) |
| `MB1.A12` 身分證 **(required)** | `d3` 身分證 | Patient.identifier |
| `MB1.A13` 出生日期 (ROC) | `d11` 出生年月日 | Patient.birthDate (via RocDate) |
| `MB1.A17` 就診日期時間 (ROC, 取前 7 碼 YYYMMDD) | `d9` + `d10` | Encounter.period |
| `MB1.A18` 就醫序號 | `d29` 就醫序號 | Encounter.identifier (IC 卡) |
| `MB1.D19` 主診斷 | `d19` 主診斷 | Condition + Claim.diagnosis |
| `MB2.p1` 醫令類別 (1藥品/2診療/9藥事服務費) | `p3` 醫令類別 | Claim.item + (1→MedicationRequest, 2→Procedure) |
| `MB2.p2` 醫令代碼 (健保碼) | `p4` 項目代號 | Claim.item.productOrService |
| `MB2.p5` 使用頻率 | `p7` 頻率 | Dosage.text |
| `MB2.p6` 給藥途徑 | `p9` 途徑 | Dosage.text |
| `MB2.p7` 總量 | `p10` 總量 | Claim.item.quantity |
| `MB2.p8` 單價 | `p11` 單價 | Claim.item.unitPrice |
| (generated running index) | `p13` 醫令序 | Claim.item.sequence |

**Intentionally NOT mapped** (no faithful target — TODO if ever needed):
- `MB1.D20` 病患姓名 — the converter keys Patient on the identifier, not a name element.
- `MB2.p3` 藥品名稱 — the converter keys orders on the code (`p4`), not the drug name.
- 點數 (媒體申報 `p12`) — 每日上傳 is a submission, not a billing total; no points field.

## Honest limitations

- **No 媒體申報/上傳 FHIR IG exists.** The output Bundle is therefore **base-R4 structurally valid**
  (+ TW Core where the converter applies it), **not** conformant to any NHI-upload-specific profile. We
  do not claim a profile we cannot point to.
- **Conformance gate** = strict Firely reparse (the repo's no-IG structural gate) + a byte-exact golden
  (`tests/.../goldens/nhi-upload-bundle.json`). This is what caught the empty-identifier bug (see the
  `healthcare-fhir-interop` brain): an identifier/coding with value `''` is invalid FHIR — the converter
  now omits absent optional fields rather than emitting empty values or fabricating them.
- **Encoding**: 每日上傳 is **Big5 (cp950)**, decoded via `CodePagesEncodingProvider`. A UTF-8 assumption
  would mojibake 姓名/診斷 — tested explicitly (0 mojibake).
- **ROC dates** (A13/A17) convert via the core `RocDate` (民國 + 1911); A17's time portion is dropped to
  the date.
- **Fail-loud**: a record missing 身分證 (`MB1.A12`) throws naming the field — never a blank Patient id.

## Scope

- **In scope**: 每日上傳 XML only.
- **Deferred**: 費用申報 CSV and vendor-specific exports (耀聖/展望/看診大師) — many variants, no real
  samples (YAGNI until a concrete need + sample appears).
- **Separate features**: 讀卡 (CardReader, pcsc) and 送件 (Submit, VPN). 送件's real transmission is
  blocked on medical-institution credentials (HCA + 健保 VPN) — see work-order #13 and the spec's
  §Business reality.

## No real data

All samples/tests use **fabricated** identifiers (e.g. `A123456789`, 王小明). No PHI, ever.
