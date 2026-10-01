# 長照 (Long-Term Care) — a NON-FHIR domain model

> The toolbox's 長照 corner. Unlike the six FHIR IGs, Taiwan LTC has **no published FHIR IG** (verified
> against the FHIR package registry — no `tw.*` long-term-care package exists). But "no FHIR IG" ≠ "no
> standard": LTC runs on well-defined **assessment scales (量表)** and a **published benefit schedule**.
> This models the parts that *are* authoritative, and is honest about the part that isn't ours to compute.

## What LTC actually runs on

| Piece | What it is | Modelled here |
|---|---|---|
| **ADL — 巴氏量表 (Barthel Index)** | Standard 10-item activities-of-daily-living scale, 0-100 | ✅ `Ltc/BarthelIndex` |
| **IADL — Lawton 工具性日常生活量表** | Standard 8-item instrumental-ADL scale, 0-8 | ✅ `Ltc/Iadl` |
| **失能等級 1-8 (CMS)** | Assigned by the county 長照管理中心 via the official **照顧管理評估量表** | ⛔ NOT computed — see below |
| **給付 (四包錢)** | 照顧及專業服務 / 交通接送 / 輔具及居家無障礙 / 喘息服務 | ✅ `Ltc/LtcBenefit` (照顧及專業服務 額度) |

## The models (faithful to the standard instruments)

- **`BarthelIndex`** — the 10 items with their exact allowed per-item scores (進食 0/5/10, 移位
  0/5/10/15, …). `Validate()` rejects any out-of-range score (fail-loud — a fabricated value can't slip
  in). `Total` is 0-100; `Band` is the standard dependency band (完全依賴 ≤20 / 嚴重 ≤60 / 中度 ≤90 /
  輕度 <100 / 完全獨立 =100).
- **`Iadl`** — the 8 Lawton items (使用電話 / 購物 / 備餐 / 家務 / 洗衣 / 交通 / 服藥 / 理財), able=1/unable=0,
  `Total` 0-8.
- **`LtcBenefit`** — the full **四包錢** benefit schedule + **部分負擔 (copay)**, by 失能等級 and 身分別.

## 四包錢 (the four packages) — published amounts + copay

Modelled in `LtcBenefit`. **All 2.0 amounts + copay below are CONFIRMED against the official primary
source** — 「長期照顧(照顧服務/專業服務/交通接送服務/輔具服務及居家無障礙環境改善服務)**給付及支付基準**」
**附表1** (衛福部; 四包錢 + 部分負擔 columns 低收/中低收/一般). Service codes in that schedule: **B/C** 碼
照顧及專業服務, **D** 碼 交通接送, **E/F** 碼 輔具/居家無障礙, **G** 碼 喘息. Every value here matches the
附表1 (照顧 L2 10,020 … L8 36,180 · 交通 1,680/1,840/2,000/2,400 · 輔具 40,000 · 喘息 32,340/48,510 ·
copay 照顧&喘息 0/5/16%, 交通&輔具 0/10/30%). The 3.0 additions (§below) are newer than this schedule and
remain secondary-sourced. All figures public, non-PHI.

| 包 | 額度 | 部分負擔 一般 / 中低收 / 低收 |
|---|---|---|
| 照顧及專業服務 | 月上限 by 等級:2→10,020 · 3→15,460 · 4→18,580 · 5→24,100 · 6→28,070 · 7→32,090 · 8→36,180 | 16% / 5% / 0% |
| 交通接送 (第4級以上) | 月額度 by 地區類別:1→1,680 · 2→1,840 · 3→2,000 · 4→2,400 | 30% / 10% / 0% |
| 輔具及居家無障礙 | 每 3 年上限 40,000 | 30% / 10% / 0% |
| 喘息服務 | 年額度:2–6級 32,340 · 7–8級 48,510 | 16% / 5% / 0% |

`CopayRate(package, payer)` + `SelfPay(amount, package, payer)` compute out-of-pocket; 等級 1 (僅衰弱) 不符資格。

> **資格範圍 (per 1966, 2025-01-01 起擴大)**: 原四類對象 + **全年齡失智且無法自理者** + 符合健保署公告條件者
> 亦納入長照服務對象。(眉角:資格 ≠ 給付額度;額度仍由失能等級決定。)

## 長照 2.0 → 3.0(2026 起,分階段上路)

長照 3.0 **不是砍掉重練**,是在 2.0(社區為基礎、以人為本、連續照顧 + 四包錢)上**加碼擴大**。
一手來源:衛福部長照專區「長照十年計畫3.0」(`1966.gov.tw`,cp-6572)+ 行政院重要政策(`ey.gov.tw`)。

**三大願景**:健康老化、在地安老、安寧善終。**八大目標**:健康促進、醫照整合、積極復能、提升機構量能、
強化家庭支持、導入智慧照顧、落實安寧善終、人力專業發展。**預算**:年 700 億 → **1,200 億**。

**分三階段:**
| 階段 | 上路 | 重點新增/調整 |
|---|---|---|
| 一 | 2025-09-01 | 聘僱外籍看護家庭**月增 3,006–10,854** 給付額度;交通接送補助提升;營養照護補助提高至 **4,500**;居家喘息調整 |
| 二 | 2026-01-01 | 服務對象擴大納入**未滿 50 歲年輕型失智者**;新增 **PAC(急性後期整合照護計畫)**收案對象 |
| 三 | 2026-07-01 | 首度納入**智慧科技輔具租賃**:每 3 年最高 **60,000**(移位/移動/沐浴排泄/居家照顧床/安全看視 五類) |

**其他 3.0 新制**:在宅責任醫師(居家醫療×長照整合)、**住宿式機構**補助(等級 4 以上:年 **12 萬 → 18 萬**,
按月認列、每半年撥付)、強化中重度照顧 + 夜間緊急服務、結合社宅布建資源。

| 維度 | 長照 2.0 | 長照 3.0 |
|---|---|---|
| 對象 | 四類 + (2025) 全齡失智 | 再擴:**不分齡 PAC 失能者 + 年輕型失智** |
| 輔具 | 輔具及居家無障礙(3年4萬) | **+ 智慧科技輔具租賃(3年6萬)** |
| 機構 | — | 住宿式補助 12萬 → **18萬**/年 |
| 醫療銜接 | 居家醫療 | **在宅責任醫師**制度 |
| 預算 | ~700億/年 | **~1,200億/年** |

> **對本 repo 的影響**:四包錢結構不變,`LtcBenefit` 基礎沿用;3.0 的新項目(智慧輔具租賃、住宿式調升)是
> **加項**,已另記為 3.0 常數(見下)。失能等級判定邏輯(CMS 八面向)不變。

## What is deliberately NOT computed (and why)

**失能等級 (1-8) is NOT derived from ADL here.** The level comes from the official **CMS 照顧管理評估量表**,
which scores **eight dimensions**, not just ADL:

> ① ADL (日常生活) · ② IADL (工具性日常生活) · ③ 認知功能 · ④ 行為/精神症狀 · ⑤ 特殊複雜照護需求 ·
> ⑥ 社會支持 · ⑦ 主要照顧者負荷 · ⑧ 居家環境

An A-level 照管專員 weighs all eight **holistically** (any one dimension needing significant help can move
the level). There is no public formula "Barthel total → 等級", so computing it from ADL alone would be
**fabrication** — forbidden by the repo's #1 rule. The ADL/IADL totals here are *inputs a care manager
uses*, not a shortcut to the level. When/if the official scoring algorithm (or a 長照 FHIR IG) is
published, wire it in and map it to `LtcBenefit`.

## Why it lives in this repo

This project is the technical foundation for a **clinic → LTC** path. The FHIR side speaks 健保; this
LTC side speaks 量表. Keeping both — clearly separated (`Ltc/` is plain C#, no FHIR types) — is what makes
it a *toolbox* rather than a single-IG library. Amounts and scales are public, non-PHI reference data.
