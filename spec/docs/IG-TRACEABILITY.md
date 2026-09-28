# IG 可追溯性對照表 (TOC) — 官方 PAS IG ↔ 本工具包

> **用途**:官方 IG(<https://nhicore.nhi.gov.tw/pas/>,`tw.gov.mohw.nhi.pas#1.2.6`)每一塊內容,對到本工具包
> 的哪個檔/哪段。**官網改版時 → 比對這份 → 立刻知道要改哪、缺什麼。**
>
> 檢查方式:重跑 `tools/fetch_validation_assets.sh` 抓最新 package → 跑本文件末的「盤點指令」→ 數字對不上就是官方動了。
>
> 目前對照基準:**IG v1.2.6 / package `#1.2.6`**(2026-08-27 generated)。artifact 統計:
> 40 Profiles · 3 Extensions · 3 Logical Models · 39 ValueSet · 21 CodeSystem · 2 ConceptMap ·
> 2 CapabilityStatement · 17 SearchParameter · 61 examples。

## 官網導覽 ↔ 本工具包(9 大類)

| # | 官網選單 | 官方內容 | 我們對應 | 狀態 |
|---|---|---|---|---|
| 1 | **應用說明 / 專案介紹·背景·範圍** | IG 導論 | `spec/docs/EXPLAINER.md`(系統人視角導讀)、`00-overview.md` | ✅ 導讀 |
| 2 | **視覺化邏輯模型** | 癌藥 / 免疫製劑 兩條流程 | `impl/csharp/…/Plugins/CancerDrugAssembler.cs`、`ImmunologicAssembler.cs` | ✅ 兩條都實作 |
| 3a | **規範文件 › 能力聲明** | CapabilityStatement × 2(Client/Server) | ❌ 未實作(是 NHI 平台/伺服器端的事,payload lib 不做) | ⬜ 標明不做 |
| 3b | **規範文件 › 查詢參數** | SearchParameter × 17 | ❌ 未實作(Server 端查詢能力) | ⬜ 標明不做 |
| 3c | **規範文件 › 邏輯模型** | Logical Model × 3(ApplyModel/ApplyImmModel/ResponseModel) | 對應 `Claim`/`ClaimResponse` 組裝邏輯(邏輯模型不可實例化,是設計藍圖) | ✅ 藍圖已落實成 assembler |
| 3d | **規範文件 › FHIR Profiles 及 Extensions** | Profile × 40 + Extension × 3 | 各 assembler 的 `Profile("X-twpas")` 呼叫 + `Sys.Ext*`(見下方 Profile 對照) | ✅ 40/40 + 3/3 |
| 3e | **規範文件 › 專門術語** | CodeSystem × 21 + ValueSet × 39 + ConceptMap × 2 | `impl/csharp/…/Constants.cs`(引用到的 system URL) | ✅ 用到的都引用;未逐一鏡像整份碼表(碼表是參考資料) |
| 4 | **範例** | example × 61 | `impl/csharp/…/Samples.cs` + `Variants.cs`;產出見 `build/*.cs.json`,盤點見 `COVERAGE.md` | ✅ bundle 5/6 + 值變體 7 + outcome |
| 5 | **結構定義與範例檔下載** | package.tgz / definitions.zip / examples.zip | `tools/fetch_validation_assets.sh`(抓 package + 建 tx-patch) | ✅ |
| 6 | **安全性 / SMART on FHIR** | 授權/存取控制規範 | ❌ 未實作(部署/傳輸層,payload lib 不含) | ⬜ 標明不做 |
| 7 | **驗證教學** | 如何用官方 validator 驗 | `tools/validate.sh` + `spec/docs/validation/`(0 errors 證據) | ✅ |
| 8 | **預檢規則(FHIR CQL)** | drug↔適應症 等預檢規則(獨立 CQL IG) | `impl/csharp/…/Core/PreCheck.cs`(seed 少量;完整規則待接 CQL IG) | 🔶 部分(seed) |
| 9 | **作者與貢獻者** | 聯絡窗口 | N/A | — |

## Profile 對照(40 Profiles → 產它的檔)

> 每個 Profile 都由某個 assembler 以 `Profile("<name>")` 產出。官方新增/改 Profile → 對這張表找對應檔改。

| 官方 Profile | 我們哪裡產 |
|---|---|
| Bundle-twpas / Claim-twpas / Encounter-twpas / Patient / Practitioner / Organization / Organization-genetic-testing / Coverage / Specimen / DiagnosticReport(-image) / ImagingStudy / Media / Observation-cancer-stage / Observation-diagnostic / Observation-laboratory-result / Observation-pat-assessment / Observation-tx-assessment / MedicationRequest-apply / MedicationRequest-treat / Procedure / Substance / DocumentReference | `CancerDrugAssembler.cs`(完整 bun-1,28 資源) |
| Bundle-immunologic-agent-twpas / Claim-immunologic-agent-twpas / Encounter-opd / Composition-opd / Condition / Observation-subjective / Observation-objective / Observation-blood-group / ClinicalImpression / CarePlan / AllergyIntolerance / Procedure-phototherapy / Substance-phototherapy | `ImmunologicAssembler.cs`(bun-imm,36 資源) |
| ClaimResponse-twpas | `Response.cs`(核定回應) |
| ClaimResponse-self-assessment-twpas | `Core/AbstractCaseAssembler.cs` → `BuildSelfAssessment`(自主審查時自動附) |
| Operationoutcome-twpas | `Outcome.cs`(錯誤回報) |
| 共用臨床層 Patient/Practitioner/Organization/Coverage/Encounter | `Core/AbstractCaseAssembler.cs`(`BuildPatient/Doctor/Hospital/Nhi/Coverage`) |
| 值變體(cancer-stage cns/tnm、pat-assessment ctcae/pdai、DiagnosticReport-image LOINC、DocumentReference phototherapy、Patient 居留證) | `Variants.cs` |

## Extension 對照(3)
| 官方 Extension | 我們 |
|---|---|
| extension-claim-encounter | `Sys.ExtClaimEncounter`(Claim 上) |
| extension-requestedService | `Sys.ExtRequestedService`(Claim.item 上) |
| extension-claimResponse-requestor | `Sys.ExtClaimResponseRequestor`(自主審查 ClaimResponse 上) |

## 官方改版時的盤點指令(貼進 terminal)

```bash
# 1. 抓最新 package
tools/fetch_validation_assets.sh
cd .fhir/pas-package/package
# 2. 重新統計各 artifact 數量,跟本文件頂部對照
python3 -c "import json,glob;from collections import Counter;c=Counter(json.load(open(f)).get('resourceType') for f in glob.glob('*.json') if f not in('package.json','.index.json'));print(dict(c),'examples:',len(glob.glob('example/*.json')))"
# 3. 版本比對
python3 -c "import json;d=json.load(open('package.json'));print(d['version'], d['dependencies'])"
```
數字 / 版本 / dependencies 有變 → 對照上表找到「哪一類多了/改了」→ 進對應檔補。**新 Profile → 加對應 `Profile(...)` 產出 + 驗 0 + 鎖 golden。**

## 目前刻意不做(非乙方 payload lib 的責任,見 COVERAGE.md)
- 能力聲明 / SearchParameter / SMART on FHIR / Server → 是甲方(NHI 平台)的事。
- 預檢規則完整 CQL → 待接獨立 CQL IG(現為 seed)。
