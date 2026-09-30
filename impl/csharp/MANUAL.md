# NhiPasFhir 使用手冊(C#)

健保事前審查(TWPAS)FHIR 參考實作。本手冊先講「**怎麼用、流程長怎樣、你要做什麼**」,再列細節。

---

# 第一部分:先搞懂這東西怎麼用

## 1. 這個 lib 是什麼、不是什麼

**是**:一台「**送審 payload 工廠 + 驗證器 + 回應處理**」。你給它「病人的事」(中性資料),它吐出「健保要的 FHIR Bundle」並保證過官方驗證。

**不是**:
- ❌ 不是 FHIR 伺服器(收件/查詢那台是**健保平台**的,不是你)
- ❌ 不是傳輸層(HTTP POST 到健保由**你的系統**做)
- ❌ 不用你懂 FHIR(你永遠不碰 profile / canonical URL / 資源結構)

> 一句話:**你講病人的事,lib 講 FHIR 的話。**

## 2. 責任分工 —— 你做什麼 vs lib 做什麼

| 步驟 | 你(整合方 / 乙方系統)做 | lib 幫你做 |
|---|---|---|
| 收資料 Capture | 從你的 EMR / 表單收臨床資料 → 填一個 `PACase` | 提供 `PACase` 型別 |
| 送前檢查 Pre-check | (呼叫 `Pipeline.Run`) | drug↔適應症 檢查,**擋核刪** |
| 組裝 Assemble | — | 產出 28~36 個 FHIR 資源、套 profile、填固定值 |
| 驗證 Validate | (呼叫 `validate.sh`) | 保證輸出 **0 errors** |
| 送出 Submit | **HTTP POST Bundle 到健保平台** | (不做傳輸) |
| 核定 Adjudicate | 收健保回傳的 `ClaimResponse` | 產回應 ✅ / 讀回應解析(規劃中) |

**重點:中間三步(檢查/組裝/驗證)lib 全包;頭(收資料)跟尾(送出/收回應)是你的系統。**

## 3. 完整生命週期(CYCLE)

```
 ┌─ 你的系統 ─────────────┐        ┌─ 這個 lib ──────────────┐        ┌─ 健保平台(甲方)─┐
 │ ① 收臨床資料           │        │                          │        │                    │
 │    → 填 PACase ────────┼───────▶│ ② Pre-check(擋核刪)     │        │                    │
 │                        │        │ ③ Assemble → FHIR Bundle │        │                    │
 │                        │◀───────┤ ④ Validate(保證 0 errors)│        │                    │
 │ ⑤ POST Bundle ─────────┼────────┼──────────────────────────┼───────▶│ ⑥ 審查            │
 │                        │        │                          │        │   核准/駁回/補件   │
 │ ⑦ 收 ClaimResponse ◀───┼────────┼──────────────────────────┼────────┤   (回傳)          │
 │    核准 → 完成          │        │                          │        │                    │
 │    補件/駁回 → 改 PACase┼──回到②─┘                          │        │                    │
 └────────────────────────┘                                            └────────────────────┘
```

- **核准** → 結束。
- **補件 / 駁回 / 申復** → 改 `PACase`(補資料 or 換申報別)→ 再跑一次 ②③④⑤。這就是為什麼申報別有「送核/補件/申復/爭議」——同一套 lib 換個參數就切換。

## 4. 這個框架怎麼跟 FHIR 合作

你**只碰 `PACase`**(一個中性的 record/字典,像填表)。lib 內部用 **Firely SDK** 把它翻成 40+ 個 FHIR 資源:

```
你填的 PACase(病人/醫師/藥/診斷…)
        │  AssemblerFactory 依 (ig, case_type) 挑對應 Assembler
        ▼
   Firely 建 FHIR 資源(Patient/Claim/Observation/…)、套 profile、填 canonical URL、固定值
        │
        ▼
   Bundle(FHIR JSON)──▶ 官方 validator ──▶ 0 errors
```

**你不需要知道** `Claim-immunologic-agent-twpas` 長怎樣、`supportingInfo` 要放什麼、ICD 碼掛哪個 system —— 那些 lib 都照官方 IG 對好了(且絕不瞎掰欄位)。

## 5. 彈性在哪裡(擴充點)

| 想改什麼 | 怎麼做 |
|---|---|
| 換藥 / 換診斷 / 換數量 | 在 `PACase.Data` 塞 `drug_code` / `diagnosis_icd` / `drug_qty`…(省略就用範例預設值) |
| 換申報別 / 案件別 | `Data["subtype_code"]`(送核/補件/申復/爭議)× `Data["priority_code"]`(一般/自主/緊急) |
| 加一種全新案件類型 | 寫一個 `AbstractCaseAssembler` 子類別 + `[ModuleInitializer]` 註冊 —— **框架/工廠/介面完全不動** |
| 換 pre-check 規則 | 擴充 `PreCheck`(未來接預檢規則 CQL IG) |
| 自己包 HTTP 送出 | 你的系統拿 `NhiPas.ToJson(bundle)` 的字串去 POST |

**設計原則:共同的臨床層(病人/醫師/機構)只寫一次,各案件只覆寫自己不同的部分。加案件不改核心。**

---

# 第二部分:實際操作參考

## 6. 三行上手

```csharp
using NhiPasFhir;
Bundle bundle = NhiPas.Build(Samples.CancerDrugCase());   // 或你自己的 PACase
string json   = NhiPas.ToJson(bundle);                     // FHIR JSON,拿去 POST
File.WriteAllText("pa-bundle.json", json);
```

## 7. PACase 欄位

| 欄位 | 說明 |
|---|---|
| `Ig` / `CaseType` | `tw.gov.mohw.nhi.pas#1.2.6` / `cancer-drug`、`immunologic-agent` |
| `Patient` | `id_card` / `name` / `gender` / `birth_date` |
| `Provider` | `doctor_id_card` / `doctor_name` / `hospital_code` / `hospital_name` |
| `Vitals` | `weight_kg` / `height_cm` |
| `Created` | 建立日期 |
| `Data` | 案件內容 + 申報別/案件別(見下) |

## 8. 案件類型(全部已驗證 0 errors)

| 案件 | 產法 | 資源數 | 官方範例 |
|---|---|---|---|
| 癌藥/一般送核 | `Build(cancer-drug)` | 28 | Bundle-bun-1 |
| 免疫製劑 | `Build(immunologic-agent)` | 36 | Bundle-bun-imm |
| 申復 | cancer-drug + `subtype_code=3` | 28 | Bundle-bun-3 |
| 自主審查 | cancer-drug + `priority_code=3`(自動內含自評 ClaimResponse) | 29 | Bundle-bun-self |
| 核定回應 | `BuildResponse(ResponseCase)` | 1 | Bundle-bun-response |

### 8.1 `Data` 案件內容鍵
`diagnosis_icd` / `diagnosis_text` / `diagnosis_date` / `procedure_icd` / `procedure_date` /
`drug_code` / `drug_qty` / `program_text` / `drug_code_2` / `drug_qty_2` / `apply_reason`。省略 → 用範例預設。

### 8.2 申報別 × 案件別
- `subtype_code`:`1`送核 · `2`送核補件 · `3`申復 · `4`爭議審議 · `5`申復補件
- `priority_code`:`1`一般 · `3`自主審查 · `4`緊急報備
- 補件/申復/爭議(2/3/4/5)需帶 `filing_ref` + `old_acpt_no`(原受理編號)。

```csharp
var appeal = Samples.CancerDrugCase() with { Data = new(...) {
    ["subtype_code"]="3", ["filing_ref"]="FHR...", ["old_acpt_no"]="202405301000002" } };
```

## 9. 送前檢查(擋核刪)

```csharp
var result = Pipeline.Run(pacase);          // 先 drug↔適應症 檢查,再組裝
if (result.Blocked) { /* 有核刪風險,別送出 */ }
Bundle bundle = result.Bundle!;             // 通過才有 bundle
```

## 10. 核定回應

```csharp
var resp = new ResponseCase(
    ResponseId:"202505301000002", PatientRef:"Patient/pat-1", HospitalRef:"Organization/org-hosp",
    ClaimRef:"Claim/cla-1", Created:"2026-09-28", Disposition:"審畢結果",
    Items:new[]{ new ResponseItem(ItemSequence:1, ApproveCode:"1", ApprovedValue:2) });
Bundle b = NhiPas.BuildResponse(resp);
```
核定意見 `ApproveCode`:`0`審核中 · `1`同意 · `2`不予同意 · `3`部份同意 · `4`補件 · `5`退件 · `6`不予同意(手術亦不支付) · `7`改核。

## 11. 建置 / 測試 / 驗證

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test                                    # 見 TESTING.md(48 test cases)
dotnet run --project samples/Emit              # 產出 build/*.cs.json
tools/validate.sh impl/csharp/build/pa-bundle.cs.json   # 官方 validator:0 errors = 合規
```
> 免疫製劑要接 TW Core ICD terminology patch(修正上游 0.3.2 CodeSystem url 錯位);`fetch_validation_assets.sh`
> 自動建、`validate.sh` 自動載入。

## 12. 完成的定義
每個案件「done」= `dotnet test` 全綠 **且** bundle 過官方 validator **0 errors**(結構+術語)。
測試策略見 TESTING.md;五種 bundle 全 0 errors。

## 13. 尚未涵蓋(見 `spec/docs/COVERAGE.md`)
讀回應解析(Client 端)· pre-check 完整規則 · Server/SearchParameter(是甲方平台的事,乙方 lib 不做)。
