# NhiPasFhir (C#) — 台灣健保事前審查 FHIR 函式庫

以 **Firely .NET SDK**(`Hl7.Fhir.R4`)實作的健保事前審查(TWPAS)FHIR 函式庫。它遵循與語言無關的
`spec/` 契約:產出的 FHIR `Bundle` 一律通過**官方 HL7 FHIR validator 0 errors**。

> 英文版見 [README.md](README.md);逐案件使用教學見 [MANUAL.md](MANUAL.md)(中文)。

## 架構(介面 → 抽象 → 實作 → 工廠)

```
ICaseAssembler                      介面      — Assemble(PACase) -> Bundle
  └ AbstractCaseAssembler           抽象類別  — 共用 TW Core 臨床層 builder + 預設 Claim-twpas 組裝
      ├ CancerDrugAssembler         實作      — 癌藥/一般送核,28 資源(完整 bun-1)
      └ ImmunologicAssembler        實作      — 免疫製劑,36 資源(覆寫 Assemble:不同 Claim/Bundle profile)
AssemblerFactory                    登錄表    — ForCase(pacase) 挑實作;找不到就丟例外
NhiPas                              facade    — Build(pacase) / BuildResponse(response) / ToJson(resource)
```

加一個案件類型 = 一個 `AbstractCaseAssembler` 子類別 + 註冊。案件的 Claim/Bundle profile 若跟預設相同就
直接沿用;不同就覆寫 `Assemble` 並重用共用臨床 builder。介面/工廠/核心不動。

## 三行上手

```csharp
using NhiPasFhir;

Bundle bundle = NhiPas.Build(Samples.CancerDrugCase());   // 或你自己的 PACase
string json   = NhiPas.ToJson(bundle);                     // FHIR JSON
File.WriteAllText("pa-bundle.json", json);
```

## 建置、測試、驗證

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet build                                   # 建函式庫
dotnet test                                    # xunit 契約測試(工廠/形狀/序列化/golden 回歸)
dotnet run --project samples/Emit              # 產出 build/*.cs.json
# 官方 validator 關卡(倉庫根目錄):0 errors = 合規
tools/validate.sh impl/csharp/build/pa-bundle.cs.json
```

## 目前涵蓋(全部 0 errors)

| 案件 | 資源數 | 官方範例 |
|---|---|---|
| 癌藥/一般送核 | 28 | Bundle-bun-1 |
| 免疫製劑 | 36 | Bundle-bun-imm |
| 申復 | 28 | Bundle-bun-3 |
| 自主審查(含自評 ClaimResponse) | 29 | Bundle-bun-self |
| 核定回應 | 1 | Bundle-bun-response |

申報別(送核/補件/申復/爭議/申復補件)× 案件別(一般/自主/緊急)已參數化。詳見
[COVERAGE](../../spec/docs/COVERAGE.md) 覆蓋盤點、[MANUAL](MANUAL.md) 逐案件教學。

## 契約(與所有語言實作一致)

「完成」= xunit 測試全綠(含 byte-for-byte golden 回歸)**且**產出的 bundle 過官方 validator
**0 errors**(結構+術語)。**絕不瞎掰 FHIR 欄位** —— 一律取自釘死的 IG package `#1.2.6`。**絕不用真實病人資料。**

> 免疫製劑要接 TW Core ICD terminology patch(修正上游 0.3.2 CodeSystem url 錯位),`tools/fetch_validation_assets.sh`
> 會自動建、`tools/validate.sh` 會自動載入。詳見 `spec/docs/validation/PASS-immunologic-2026-09-28.md`。
