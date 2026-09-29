# CQL 整合筆記 — 送前核刪自查(維度3 的「查 Bundle」)

> 官方預檢規則 IG:`tw.gov.mohw.nhi.cql`(build.fhir.org/ig/TWNHIFHIR/cql,**v0.0.1 draft**)。
> 77 Library ≈ 66 條藥品給付規則(BC 乳癌 / LC 肺癌 / HCC 肝癌 / CRC 大腸癌 / PC 攝護腺癌)。
> 這是產品核心價值(核刪防呆)。本文件是「未來接引擎」的起跑點,已對抗驗證過理解。

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
- **`ICqlPreCheck`** — 框架面插槽。`Pipeline.Run(case, cql: …)` 傳入才啟動;不傳(預設)= **沒啟動 CQL**(只跑種子 drug↔indication)。
- **`NoCqlPreCheck`**(off,預設)/ **`CqlPreCheck`**(on)。
- **`CqlOutcome`** 三態:`Pass` / `WouldBeRejected`(核刪)/ `DataMissing`(補件)+ `NotEvaluated`。
- **`ICqlEngine`** — ELM 執行引擎介面(sidecar 實作;本 lib 不含)。`NotWiredCqlEngine` 未接時 fail-loud。
- `CqlPreCheck` 編碼了盲點 2/3:1:N 藥碼→規則、逐規則呼叫引擎、把具名輸出詮釋成三態(慣例:`…申請結果_布林`、`…報告總結`、`…資料存在/有填…`)。
- 測試:`CqlTests.cs`(off 無結果 / on 三態 / fail-loud)用假引擎驗膠水,不需真 runtime。

## 5. 未做(接真引擎時的工作)
1. **接一個 CQL 引擎 sidecar**(建議先 JS `cql-execution`)。PoC 卡點:`FHIRHelpers.ToInteger` 資料源型別對齊(cql-exec-fhir 版本/模型)。
2. **實作 `ICqlEngine`** 呼叫該 sidecar,並**載入規則的完整依賴閉包**。
3. **建 藥碼→規則 1:N 對映表**(從 66 條 Library 的 relatedArtifact/命名建索引)。
4. **組裝器補「縱貫病歷」**:讓 Bundle 帶入前次申請/治療等歷史(盲點 1)。
5. **鎖版本 + 基準日**:本地包版本 = server 生效版;統一評估日。
6. 逐 library 確認是否需 terminology server。

**狀態:CQL IG 仍 v0.0.1 draft(規則會變)。框架插槽已備好;接引擎待其穩定或商業需要時投入。**
