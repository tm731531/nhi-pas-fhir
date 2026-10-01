# 健保卡 Card Reader — 基本資料段 → FHIR Patient (ingest adapter)

> The **讀卡 (CardReader)** inbound adapter of the 一條龍 (`讀卡 → 進料 → FHIR → 送件`). It reads a 健保卡's
> **基本資料段** over PC/SC and builds a base-R4 FHIR Patient. Lives in **`NhiPasFhir.CardReader`**, which
> depends on the pure FHIR core; the core never depends on it (Hexagonal boundary). See
> `spec/specs/003-card-reader/spec.md`.

## Flow

```
健保卡 (PC/SC reader)
  └─ NhiCardReader.ReadFirstCard()     → transmit SELECT + READ APDU → 57-byte 基本資料段
       └─ NhiCardBasicParser.Parse()   → NhiCardBasic (卡號/姓名/身分證/生日/性別/發卡日)
            └─ CardToPatient.ToPatient()→ base-R4 FHIR Patient
```

The pure path (`byte[] → NhiCardBasic → Patient`) is fully unit-tested with fabricated fixtures. The
PC/SC transport is a thin shell, tested live via a **SkippableFact** (skips when no reader/card/PC-SC
subsystem — hardware/env absence is not a failure).

## Sourced APDU + layout (tw-nhi-icc-service, MIT)

> ⚠️ These are transcribed **verbatim** from `magiclen/tw-nhi-icc-service` (`src/card/mod.rs`,
> `src/card/nhi_card_basic.rs`), MIT. Do **not** edit an APDU byte or an offset without re-verifying
> against a real card — a wrong card command can return garbage or lock a card. A constant-match test
> guards the APDU bytes.

**APDU** (`NhiCardReader.ApduSelect` / `ApduRead`):
- SELECT (健保 AID): `00 A4 04 00 10 D1 58 00 00 01 00 00 00 00 00 00 00 00 00 11 00`
- READ (GET DATA): `00 CA 11 00 02 00 00`

**基本資料段 byte layout** (≥ 57 bytes):

| bytes | field | encoding | FHIR |
|---|---|---|---|
| `[0:12]` | 卡號 | ASCII | (not mapped — card no.) |
| `[12:32]` (NUL-term) | 姓名 | **Big5** | Patient.name.text |
| `[32:42]` | 身分證號 | ASCII | Patient.identifier.value |
| `[42:49]` | 生日 | ROC YYYMMDD | Patient.birthDate (via RocDate) |
| `[49]` | 性別 | 'M'/'F' | Patient.gender |
| `[50:57]` | 發卡日 | ROC YYYMMDD | (not mapped — card issue date) |

## Honest limitations / gates

- **基本資料段 is readable with just the 健保卡** — no 醫事人員卡 needed. That is the scope here.
- **就醫序號 / 寫卡 / 上傳** require a **醫事人員卡 (HCA) + 健保 SAM + controlled software** — out of scope
  (credential-gated, work-order #13).
- **Patient is base R4** (identifier/birthDate/gender/name) — **no profile claimed** (a card read is
  demographics, not a PA case). Passes strict Firely reparse. Promoting to TWCorePatient is a later,
  separate change if a standalone TW Core patient helper is added to the core.
- **身分證 identifier system URL** is a `TODO: confirm official canonical URL` — not fabricated as final.
- **Live read is a SkippableFact** — needs a PC/SC reader + card on the operating machine; skips (never
  fails) otherwise.
- **Why PC/SC, not WebUSB**: the 健保 reader is a CCID smart-card reader the OS PC/SC stack claims; WebUSB
  would have to fight that driver, is Chromium-only, and would re-implement CCID — so the native PC/SC
  path (pcsc-sharp) is the right one. (See the strategy notes.)

## No real data

All fixtures/tests use **fabricated** values (e.g. `A123456789`, 王小明). No PHI, ever.
