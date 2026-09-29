# CQL 怎麼串進我們的框架?(一張圖看懂)

> 前一篇 [`cql-explained.md`](cql-explained.md) 講「CQL 是什麼」。這篇講「**它怎麼接進我們這套系統**」——
> 也就是你在這個 demo 按下「跑一次」時,背後那條線是怎麼走的。

---

## 一句話

> **我們的 C# 只負責一件事:把申請組成一份 FHIR Bundle。要不要拿官方 CQL 規則自查,是一個開關;
> 開了,就把那份 Bundle 送到一台「規則引擎 server」跑官方規則,回來的結果我們翻譯成三態(通過/核刪/補件)。**

規則**住在 server**,不住在我們的程式裡。所以健保改規則 = 重載 server + 拿同一份 Bundle 重跑,**C# 一行都不用改**。

---

## 那條線(按「跑一次」發生什麼)

```
 你選案子 + 勾「送 CQL 自查」
        │
        ▼
 ① 產  Pipeline 呼叫 lib 組出 FHIR Bundle           (NhiPas.Build)
        │
        ▼
 ② 驗  種子檢查:藥 ↔ 適應症 是不是合法組合          (PreCheck)
        │
        ▼
 ③ 查  有開關才走這步 ↓
        │
        ├─ 沒開 (NoCqlPreCheck)  → 跳過,直接放行組裝結果
        │
        └─ 有開 (CqlPreCheck) →
               CqfRulerCqlEngine 把 Bundle 用 HTTP POST 到
               ┌───────────────────────────────────────────┐
               │  CQF-Ruler server (:8095)                  │
               │  Library/{規則}/$evaluate                   │
               │  用官方 ELM 規則跑(含 InCodeSystem 等)     │
               └───────────────────────────────────────────┘
               回一堆「具名結果」(申請結果_布林 / 資料存在… / 報告總結)
        │
        ▼
 ④ 翻譯  把具名結果 → 三態:通過 / 會被核刪 / 缺資料待補件
        │
        ▼
 你看到的判決 + 報告總結 + 這份 Bundle
```

---

## 關鍵設計:一個「插座」+ 一個「開關」

| 角色 | 是什麼 | 意義 |
|---|---|---|
| **開關** | `ICqlPreCheck`:`NoCqlPreCheck`(關,預設)/ `CqlPreCheck`(開) | 沒接引擎也能只做「產+驗」;要查再開 |
| **插座** | `ICqlEngine` 介面 | 引擎藏在介面後面,換引擎只換這一塊 |
| **這次用的插頭** | `CqfRulerCqlEngine` | 打 CQF-Ruler server 的 `$evaluate` |
| **規則** | 官方 ELM,住在 server | 改版=重載+重跑,不動 C# |

**換引擎不影響其他任何東西**:今天插 CQF-Ruler,明天想換 Java 或別的,只換 `CqfRulerCqlEngine` 這個插頭,
Bundle、Pipeline、UI 全都不動。這就是「規則住 server、契約是 Bundle 進/三態出」的好處。

---

## 四塊東西各住哪(repo 導覽)

| 資料夾 | 是什麼 |
|---|---|
| `cql-engine/elm/` | 官方規則的 **ELM**(機器執行形式),語言中立 |
| `cql-engine/rules/` | 官方 **Library 資源(含 CQL 文字)**,載進 server 用的 |
| `cql-engine/server/` | **CQF-Ruler**(忠實引擎)的 docker + 載入腳本 |
| `cql-engine/js/` | 一支 JS 參考 runner(當初驗證用;它缺 InCodeSystem,才改用 server) |
| `impl/csharp/…/Core/Cql.cs` | 開關 + 插座(`ICqlPreCheck` / `ICqlEngine`) |
| `impl/csharp/…/Core/CqfRulerCqlEngine.cs` | 這次的插頭(打 server) |

---

## 三態的意思(為什麼不是「過/不過」)

| 判決 | 意思 | 例子 |
|---|---|---|
| **通過 (Pass)** | 官方規則算下來會過 | 各條件、各檢測都符合 |
| **會被核刪 (WouldBeRejected)** | 條件不符,送了會被扣款 | HER2 未達陰性 |
| **缺資料待補件 (DataMissing)** | 該填的沒填,先補再送 | 沒填 ER/PR 檢測資料 |

自查最能**自動擋掉**的是「缺資料」型;「條件不符」有些還要人工判讀。所以這是**高信心預測,不是保證** ——
最終核定權還是在健保的正式回覆。
