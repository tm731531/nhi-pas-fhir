# COVERAGE — 「完整乙方」對甲方 (PAS IG 1.2.6) 的覆蓋盤點

> 甲方 = 健保署 PAS IG(46 profiles · 6 情境範例 · 2 CapabilityStatement · 17 SearchParameter)。
> 乙方 = 本 lib。目標:全面覆蓋。狀態:✅ 完成 · ⚠️ 部分 · ⬜ 未做。

## 維度 1｜送出組裝(申請案 Bundle)
| 情境(官方範例) | 申報別 subType | 案件別 priority | 資源數 | 狀態 |
|---|---|---|---|---|
| 一般送核·癌藥(bun-1) | 送核 | 一般 | 28 | ⚠️ 只做 9 資源精簡版(官方完整版 28,含 Specimen/基因檢測) |
| 免疫製劑(bun-imm) | 送核 | 一般 | 36 | ✅ 全 36 資源,0 errors,可吃 PACase |
| 申復(bun-3) | 申復 | 一般 | 26 | ⬜ |
| 自主審查(bun-self) | 送核 | 自主審查 | 30 | ⬜ |

缺申報別:**送核補件 / 申復 / 申復補件 / 爭議審議**。缺案件別:**自主審查 / 緊急報備**。

## 維度 2｜核定回應(ClaimResponse)—— 全缺
| | profile | 狀態 |
|---|---|---|
| NHI 回應(核准/駁回/補件)bun-response | ClaimResponse-twpas | ⬜ |
| 自主審查回應 | ClaimResponse-self-assessment-twpas | ⬜ |

只會「送出」,尚不會「讀回應 / 產回應」。

## 維度 3｜API 介面(CapabilityStatement + SearchParameter)—— 全缺
- TWPAS **Client**(送審 + 查詢流程)⬜
- TWPAS **Server**(收件 + 驗證 + 回應;17 SearchParameter)⬜

## 已建立的地基(跨情境共用)
- 框架:Interface → Abstract(共用 TW Core 臨床層 builder)→ Implementation + Factory ✅
- Pre-check(drug↔適應症)+ Pipeline gate ✅(規則僅 seed 2 條,待接預檢規則 CQL IG)
- golden-file 回歸鎖(癌藥 + 免疫製劑 byte-for-byte)✅
- 官方 validator 0 errors(結構+術語,含 TW Core ICD CodeSystem terminology patch)✅

## 建議推進順序
1. 維度 1 補滿:自主審查(bun-self)、申復(bun-3);癌藥升級到完整 28 資源版。
2. 維度 2:ClaimResponse 讀取/產生(核定結果解析)。
3. 維度 3:Client 送審流程 → Server(較重)。
4. Pre-check 接真規則(核刪防呆,最有商業價值)。
