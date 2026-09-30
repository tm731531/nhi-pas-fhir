# CQL 整合筆記 — 送前核刪自查(維度3 的「查 Bundle」)

> 🟢 **先看白話版:** 沒碰過 CQL/FHIR → 先讀 [`cql-explained.md`](cql-explained.md)(用 16 個問答把觀念想通)。
> 本文是「怎麼接程式」的技術面;規則(語言中立的 ELM)+ 一支參考 runner 在 [`cql-engine/`](../../cql-engine/)。
>
> 官方預檢規則 IG:`tw.gov.mohw.nhi.cql`(build.fhir.org/ig/TWNHIFHIR/cql,**v0.0.1 draft**)。
> 77 Library ≈ 66 條藥品給付規則(BC 乳癌 / LC 肺癌 / HCC 肝癌 / CRC 大腸癌 / PC 攝護腺癌)。
> 這是產品核心價值(核刪防呆)。接引擎已完成(見 §6);本文件保留設計脈絡與盲點。

## 1. 定位(一句話)
**組完 Bundle 後、POST 前,用健保『同一套』官方 CQL 規則對 Bundle 自查一次 → 只送會過的、擋掉會核刪/待補件的。** 高信心「預測」,非保證(最終核定權在健保的 ClaimResponse)。

## 2. 三個名詞(校準過)
| 名詞 | 是什麼 | SQL 類比 |
|---|---|---|
| **CQL** | 規則原始碼(人可讀文字) | SQL 原文 |
| **ELM** | CQL 編譯後的執行形式(JSON/XML,非 binary) | 執行計畫 |
| **CQL 引擎** | 執行 ELM 的軟體(JS cql-execution / Java cqframework / CQF-Ruler);.NET 無成熟原生 → 接 sidecar | SQL Server |
每個 Library 檔同時附 `text/cql` + `application/elm+xml` + `application/elm+json`;引擎跑 **ELM**,CQL 文字只給人看。

## 3. ⚠️ 三個實作會咬人的盲點(對抗驗證挖出)
1. **Bundle 不是「現成資料庫」,是你要自己組的「縱貫病歷」。** 續用/回診規則會 `retrieve` 前次申請/前次治療/本次申請日之前的用藥影像;profile 特開 `medicationRequestTreat` slice 就是要 client 把病史打包進來。**漏放病史 → retrieve 撈空 → 同份 ELM 給出跟健保不同的核刪,而且是靜默錯誤。** FHIR 路徑深層可選、缺值預設 false,不像 SQL schema 保證欄位。
2. **挑規則檔要載「整個依賴閉包」。** 每條 Rule `depends-on` `BCReusable`/`BCCodeConcept`/`FHIRHelpers`,只載單檔連 translate 都失敗。藥碼→規則是**你維護的 1:N 索引**(一藥多規 Rule1/2/3)。引擎回的是**具名 expression 值(bool/tuple),不是現成核准/核刪** → 要自己詮釋。
3. **自查輸出是三態,不是 pass/fail:** 通過 / 條件不符=核刪 / 必要資料未填=補件。自查最能自動擋的是「資料未填」型;「條件不符」仍有人工判讀、影像充足性等 **CQL 編不出的閘門**。要對齊 **評估基準日**(以申請日算年齡/24週窗)與 **規則版本**(本地=server 生效版)。這批乳癌規則多為 code 直接比對、不必然要 terminology server(逐 library 確認)。

## 4. 已做的框架接點(可開關)
`impl/csharp/src/NhiPasFhir/Core/Cql.cs` + `Pipeline.cs`:
- **`ICqlPreCheck`** — 框架面插槽。`Pipeline.RunAsync(case, cql: …)` 傳入才啟動;不傳(預設)= **沒啟動 CQL**(只跑種子 drug↔indication)。
- **`NoCqlPreCheck`**(off,預設)/ **`CqlPreCheck`**(on)。
- **`CqlOutcome`** 三態:`Pass` / `WouldBeRejected`(核刪)/ `DataMissing`(補件)+ `NotEvaluated`。
- **`ICqlEngine`** — ELM 執行引擎介面;已由 `CqfRulerCqlEngine` 實作(見 §6)。`NotWiredCqlEngine` 未接時 fail-loud。
- `CqlPreCheck` 編碼了盲點 2/3:1:N 藥碼→規則、逐規則呼叫引擎、把具名輸出詮釋成三態。**判定法**:先看 `…申請結果_布林`
  (true=通過,無此 key=NotEvaluated 不預設核刪);為 false 時,**補件/核刪的分類直接讀規則自己在 `…報告總結` 裡列的兩段**
  (`【▲不符合項目 - 必要資料未填寫】`=補件、`【▲不符合項目 - 條件或代碼不符合】`=核刪,`（參考資訊）`不計判定),
  而非自己用中間旗標猜 —— 因為規則已按初次/續用分支正確分類,且真正的分類旗標在 `BCReusable`、`$evaluate` 不回傳。核刪優先。
- 測試:`CqlTests.cs`(off 無結果 / on 三態 / fail-loud)用假引擎驗膠水,不需真 runtime。

## 5. 未做(接真引擎時的工作)

> **已完成(見 §6)。** 以下清單保留作歷史紀錄 — 實際走的路是 §6 的 CQF-Ruler 方案,細節與此處原始規劃(如「JS cql-execution」)不同。

1. **接一個 CQL 引擎 sidecar**(建議先 JS `cql-execution`)。PoC 卡點:`FHIRHelpers.ToInteger` 資料源型別對齊(cql-exec-fhir 版本/模型)。
2. **實作 `ICqlEngine`** 呼叫該 sidecar,並**載入規則的完整依賴閉包**。
3. **建 藥碼→規則 1:N 對映表**(從 66 條 Library 的 relatedArtifact/命名建索引)。
4. **組裝器補「縱貫病歷」**:讓 Bundle 帶入前次申請/治療等歷史(盲點 1)。
5. **鎖版本 + 基準日**:本地包版本 = server 生效版;統一評估日。
6. 逐 library 確認是否需 terminology server。

**狀態:CQL IG 仍 v0.0.1 draft(規則會變)。框架插槽已備好;接引擎待其穩定或商業需要時投入。**

## 6. 落地進度 — 選 C:CQF-Ruler 忠實引擎(2026-09-29)

決策(decision-server):(1) 補 InCodeSystem 選 **B=接忠實引擎**;(2) 實現方式選 **C=CQF-Ruler / HAPI
clinical-reasoning 整台 FHIR server(docker),C# 打它 API**。理由:最乾淨分離、可獨立換。

**Tom 的替換不變量:契約 = 「Bundle 進 → 三態出」。規則住在 server 不住在 C# code。** 故規則/IG 改版 =
重載 package + 拿同一份 Bundle **重跑**(C# 不改);換引擎 = 只換 `ICqlEngine` 那個 HTTP 轉接器。

已驗證(`cql-engine/server/`):
- ✅ `docker compose up` 起 cqf-ruler(HAPI FHIR 7.4.2 / R4),`http://localhost:8095/fhir`。operation:`$cql`、`Library/$evaluate`。
- ✅ 引擎會跑(`$cql` `5+3`→8)。
- ✅ **關鍵驗證:這版引擎忠實實作 `InCodeSystem`** —— 存一個 `text/cql` 的最小 library
  `Code 'C90.00' from ICD2023 in ICD2023` → `$evaluate` 回 **true**,且**不必先載整包 terminology**。
  這正是 cql-execution 3.3.2 做不到(build→null)、選忠實引擎要解的那點。**證實成立。**
- ✅ `load-libraries.mjs` 可重跑:把 `../elm/*.json` 包成 Library resource 載入(url 對齊 include canonical,
  FHIRHelpers→HL7 path,呼應 JS 端 LenientRepository 的同一個 mismatch)。

✅ **端到端打通(2026-09-29 收尾):真規則跑出核定結果了。**
- 從官方 `tw.gov.mohw.nhi.cql` package(build.fhir.org)抓官方 Library resource;每個內含
  `text/cql` + `elm+xml` + `elm+json`。**HAPI-CR 是從 `text/cql` 載 source(自己編譯),不吃 raw
  `elm+json`**(這就是先前 "Could not load source … version null" 的真因)。
- vendored 官方 Library 的 **text/cql-only** 版進 `cql-engine/rules/Library-*.json`(4 檔僅 416 KB;
  丟掉跟 `elm/` 重複的 elm+xml/json)。`load-libraries.mjs` 改成載這些。
- `Library/BCAbemaciclibRule1/$evaluate` 對我方 Bundle → **68 個 define 全求值**,拿到
  `乳癌Abemaciclib申請結果_布林=false`、`乳癌Abemaciclib申請之CQL檢核結果=✖不通過…`、`報告總結=…`、
  `主要疾病ICD資料存在=true`(InCodeSystem 有在跑)。判「不通過」正確 —— 測試 Bundle 是骨髓瘤案,不符乳癌條件。
- **C# 已接上**:`Core/CqfRulerCqlEngine.cs` = `ICqlEngine` 實作,POST `Library/{ruleId}/$evaluate`
  (subject+useServerData=false+data:Bundle),把具名結果餵回 `CqlPreCheck.Interpret()`。整合測試
  `CqfRulerIntegrationTests.cs`(server 沒開就跳過)綠:引擎回具名值、PreCheck 對骨髓瘤案 Block。

**啟用方式**:`Pipeline.RunAsync(case, cql: new CqlPreCheck(new CqfRulerCqlEngine(http, "http://localhost:8095/fhir"),
藥碼→規則map))`。不傳 cql = 照舊不啟動。

**仍待做(非阻塞)**:(1) 建完整 藥碼→規則 1:N 對映表(全 66 條);(2) 組裝器補「縱貫病歷」讓續用/回診規則
撈得到病史;(3) 若要 Pass 範例,備一份符合乳癌 Abemaciclib 條件的 Bundle;(4) server 目前 localhost 手動起,
未來要常駐/上你的 infra。
