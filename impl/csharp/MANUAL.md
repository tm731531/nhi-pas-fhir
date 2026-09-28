# NhiPasFhir (C#) — 使用手冊

健保事前審查(TWPAS)FHIR 參考實作 · C#(Firely SDK)。**每個輸出都過官方 HL7 validator 0 errors。**
架構與安裝見 [README](README.md);本手冊教「怎麼產每一種案件」。

## 0. 快速開始

```csharp
using NhiPasFhir;

Bundle bundle = NhiPas.Build(Samples.CancerDrugCase());   // 或你自己的 PACase
string json   = NhiPas.ToJson(bundle);                     // FHIR JSON
File.WriteAllText("pa-bundle.json", json);
```

驗證(倉庫根目錄,需先 `tools/fetch_validation_assets.sh`):
```bash
tools/validate.sh impl/csharp/build/pa-bundle.cs.json   # 0 errors = 合規
```

## 1. 一支 PACase 打天下

`PACase` 是中性(非 FHIR)的案件描述,`AssemblerFactory` 依 `(ig, case_type)` 挑對應 assembler:

| 欄位 | 說明 |
|---|---|
| `Ig` / `CaseType` | `tw.gov.mohw.nhi.pas#1.2.6` / `cancer-drug`、`immunologic-agent` |
| `Patient` | `id_card` / `name` / `gender` / `birth_date` |
| `Provider` | `doctor_id_card` / `doctor_name` / `hospital_code` / `hospital_name` |
| `Vitals` | `weight_kg` / `height_cm` |
| `Created` | 建立日期 |
| `Data` | 案件內容(見下表)+ 申報別/案件別 |

## 2. 案件類型(已驗證 0 errors)

| 案件 | CaseType / 產法 | 資源數 | 官方範例 |
|---|---|---|---|
| 癌藥/一般送核 | `Build(cancer-drug)` | 28 | Bundle-bun-1 |
| 免疫製劑 | `Build(immunologic-agent)` | 36 | Bundle-bun-imm |
| 申復 | cancer-drug + `subtype_code=3` | 28 | Bundle-bun-3 |
| 自主審查 | cancer-drug + `priority_code=3`(自動內含自評 ClaimResponse) | 29 | Bundle-bun-self |
| 核定回應 | `BuildResponse(ResponseCase)` | 1 | Bundle-bun-response |

### 2.1 案件內容 `Data` 鍵(cancer/immunologic 通用)
`diagnosis_icd` / `diagnosis_text` / `diagnosis_date` / `procedure_icd` / `procedure_date` /
`drug_code` / `drug_qty` / `program_text` / `drug_code_2` / `drug_qty_2` / `apply_reason`。
省略任一鍵 → fallback 官方範例值。

### 2.2 申報別 × 案件別矩陣
- `subtype_code`:`1`送核 · `2`送核補件 · `3`申復 · `4`爭議審議 · `5`申復補件
- `priority_code`:`1`一般 · `3`自主審查 · `4`緊急報備
- 補件/申復/爭議(subType 2/3/4/5)需帶 `filing_ref` + `old_acpt_no`(原受理編號,invariant applType 要求)。

```csharp
var appeal = Samples.CancerDrugCase() with { Data = new(...) {
    ["subtype_code"]="3", ["filing_ref"]="FHR...", ["old_acpt_no"]="202405301000002" } };
```

## 3. 核定回應(維度2)

```csharp
var resp = new ResponseCase(
    ResponseId: "202505301000002", PatientRef: "Patient/pat-1", HospitalRef: "Organization/org-hosp",
    ClaimRef: "Claim/cla-1", Created: "2026-09-28", Disposition: "審畢結果",
    Items: new[] { new ResponseItem(ItemSequence: 1, ApproveCode: "1", ApprovedValue: 2) });
Bundle b = NhiPas.BuildResponse(resp);   // Bundle-response-twpas (searchset)
```
核定意見碼 `ApproveCode`(nhi-approve-comment):`0`審核中 · `1`同意 · `2`不予同意 · `3`部份同意 · `4`補件 · `5`退件 · `6`不予同意(對應手術亦不支付) · `7`改核。

## 4. Pre-check(送出前擋核刪)

```csharp
var result = Pipeline.Run(pacase);          // 先 drug↔適應症 檢查,再組裝
if (result.Blocked) { /* 核刪風險,別送 */ }
Bundle bundle = result.Bundle!;             // 通過才有 bundle
```
規則目前 seed 少量 drug↔適應症;完整規則待接「預檢規則 CQL IG」。`result.Advisory` 提醒此為決策輔助、不保證給付。

## 5. 驗證與 terminology patch(重要)

TW Core 0.3.2 的 ICD CodeSystem 因 `url` 錯位(掛在 `/ValueSet/`),離線 validator 會誤判 closed slicing。
`tools/fetch_validation_assets.sh` 會建 terminology patch 補正 url,`tools/validate.sh` 自動載入 → 離線也 0 errors。
詳見 `spec/docs/validation/PASS-immunologic-2026-09-28.md`。

## 6. 完成的定義

每個案件「done」= `dotnet test` 全綠(含 byte-for-byte golden 回歸)**且** 產出的 bundle 過官方 validator **0 errors**(結構+術語)。目前 20 tests 綠、五種 bundle 全 0 errors。

## 7. 尚未涵蓋(見 `spec/docs/COVERAGE.md`)
讀回應解析(Client 端)· Server/SearchParameter(甲方平台的事,乙方 lib 不做)· pre-check 完整規則。
