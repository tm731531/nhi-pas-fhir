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

Modelled in `LtcBenefit`. Amounts are the widely-cited published 給付額度 (≈2026); **non-PHI public
reference, but reconfirm against the official 1966 / 衛福部 給付額度表 before production** (secondary-sourced).

| 包 | 額度 | 部分負擔 一般 / 中低收 / 低收 |
|---|---|---|
| 照顧及專業服務 | 月上限 by 等級:2→10,020 · 3→15,460 · 4→18,580 · 5→24,100 · 6→28,070 · 7→32,090 · 8→36,180 | 16% / 5% / 0% |
| 交通接送 (第4級以上) | 月額度 by 地區類別:1→1,680 · 2→1,840 · 3→2,000 · 4→2,400 | 30% / 10% / 0% |
| 輔具及居家無障礙 | 每 3 年上限 40,000 | 30% / 10% / 0% |
| 喘息服務 | 年額度:2–6級 32,340 · 7–8級 48,510 | 16% / 5% / 0% |

`CopayRate(package, payer)` + `SelfPay(amount, package, payer)` compute out-of-pocket; 等級 1 (僅衰弱) 不符資格。

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
