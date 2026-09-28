# 04 — Taiwan FHIR IG Landscape (家族全景)

> pas (事前審查) is **one IG in a family**. This file maps the whole set so we never mistake the
> part for the whole. All IGs inherit the same base — **TW Core** — which is the strategic key.

## The foundation: TW Core IG

- **臺灣核心實作指引 (TW Core IG)** — <https://twcore.mohw.gov.tw/ig/twcore/>
- Base profiles for Taiwan healthcare data exchange (Taiwan's equivalent of US Core).
- Built on **FHIR R4.0.1** + **IPS** (International Patient Summary).
- Steward: 衛福部資訊處 (MOHW Dept. of Information).
- **Everything below inherits from TW Core.** Build against TW Core once → extend per IG.

## Family tree

```
TW Core IG  (twcore.mohw.gov.tw)  ── the base
│
├── NHI camp — 健保署 (nhicore.nhi.gov.tw)
│   ├── 事前審查  TWPAS        ← this repo's focus (docs 00–03)
│   ├── 重大傷病  TWCI (catastrophic illness)
│   ├── 預檢規則  CQL   (pas rule engine; CQL logic modules)
│   ├── 醫療保險理賠  (insurance claims)
│   └── 健保署基礎 IG (base)
│
└── MOHW camp — 衛福部 (twcore.mohw.gov.tw / medstandard.mohw.gov.tw)
    ├── 電子病歷交換  EMR-IG  ★ flagship (2026 cross-hospital interop)
    │     forms: 出院病摘 · 門診病歷 · 檢驗檢查 · 醫療影像報告 · 電子處方箋 · 調劑單張
    ├── 傳染病檢驗報告 IG
    ├── 電子處方箋與調劑 IG
    ├── 🎯 臺灣長期照顧實作指引 (Long-Term Care IG)  ← mission market; already FHIR
    └── HealthBank 健康存摺 IG (ig.dicom.tw)
```

## Why this matters (strategy)

Because every IG inherits **TW Core**, a tool built against TW Core generalises across the family:

- **Clinic beachhead** rides: TWPAS (事前審查) + EMR-IG (門診/檢驗/處方) + 理賠.
- **LTC mission** rides: 長期照顧 IG — **same TW Core base, different profiles**.
- The clinic→LTC bridge is therefore *"same base, swap the IG"*, not two separate worlds. This
  structurally mitigates the beachhead trap (cash market and mission market share one foundation).

## Repo scope note

This repo starts with **pas** (a concrete, high-value, self-contained金流 use case). The structure
(`docs/` catalogue + `src/` core-first) is deliberately generic so sibling IGs (EMR, LTC) can be
added as parallel packages later, all sharing a TW Core layer.

`# TODO`: pull TW Core base profiles; add `docs/05-twcore.md`; later add `ig/emr/` and `ig/ltc/`.
