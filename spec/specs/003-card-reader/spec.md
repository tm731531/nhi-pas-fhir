# Feature Specification: 健保卡 Card Reader — 基本資料段 → FHIR Patient

**Feature Branch**: `003-card-reader`
**Created**: 2026-10-01
**Status**: Draft — design approved in brainstorming 2026-10-01
**Input**: Read a 健保卡's **基本資料段** (basic-data segment) over PC/SC and turn it into a **base-R4 FHIR
Patient**. This is sub-project 2 of the "一條龍" (讀卡 → 進料 → FHIR → 送件). Scope = reading the basic
segment (readable **without** a 醫事人員卡) and producing Patient demographics. Governed by
`.specify/memory/constitution.md`.

> **Constitution binding:** I (correctness proven by a reparse/parse gate), II (authoritative source
> only — APDU + byte layout transcribed from a verified reference, never invented), III (advisory, not
> authority), IV (fail-safe/fail-loud), V (test-first, no PHI), VI (traceable/versioned).

> **North-star (context, NOT this spec's scope):** `CardReader (讀卡) → Ingest (進料) → Core (FHIR IG) →
> Submit (送件/VPN)`, Hexagonal — every adapter depends on the pure FHIR core; the core depends on none
> of them. This feature builds the **CardReader** inbound adapter (demographics). 就醫序號/寫卡/上傳 and
> 送件 are credential-gated and out of scope (see §Business reality / work-order #13).

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Parse a basic-data segment into a Patient (Priority: P1)

Given the raw bytes of a 健保卡 基本資料段, the system parses the documented fields (卡號 / 姓名 / 身分證號 /
生日 / 性別 / 發卡日) and builds a base-R4 FHIR Patient. This is the pure, hardware-independent core — it
is what makes the reader testable and reusable.

**Why this priority**: The byte→Patient mapping is the reusable, verifiable value; the PC/SC transport is
a thin shell around it. Getting the layout right (sourced, not guessed) is the whole point.

**Independent Test**: Provide a **fabricated** 57-byte basic segment → parse → build Patient → the Patient
carries the id_no as its identifier, the correct birthDate (ROC→西元) and gender; the Patient JSON passes
strict Firely reparse. No physical reader needed.

**Acceptance Scenarios**:
1. **Given** a fabricated basic segment (卡號, Big5 姓名, 身分證, ROC 生日, 'M'/'F', ROC 發卡日), **When**
   parsed, **Then** every field decodes to its exact value (Big5 姓名 with 0 mojibake; ROC 生日→西元 date).
2. **Given** a buffer shorter than the documented minimum (57 bytes), **When** parsed, **Then** it **fails
   loud** (not a partial/garbage Patient).
3. **Given** a parsed basic record, **When** converted, **Then** the Patient has `identifier.value` = 身分證,
   `birthDate` = 西元 date, `gender` = male/female, and reparses as structurally valid base-R4 FHIR.

---

### User Story 2 — Read a live card over PC/SC (Priority: P2)

With a PC/SC reader and a 健保卡 present, the system connects, transmits the documented SELECT + READ
APDUs, and returns the basic segment bytes to User Story 1's parser.

**Why this priority**: This is the physical front door Tom wants ("插卡→跳資料"), but it depends on
hardware and on US1 being correct first.

**Independent Test**: With a reader + card attached, read → a populated NhiCardBasic. In CI / no-hardware
environments the test is **skipped** (SkippableFact), never failed — mirroring the CQF-Ruler integration
test pattern.

**Acceptance Scenarios**:
1. **Given** a connected reader with a 健保卡, **When** read, **Then** the returned bytes parse (US1) to a
   Patient with a non-empty 身分證.
2. **Given** no reader / no card, **When** the integration test runs, **Then** it is **skipped**, not
   failed (hardware absence is not a code failure).

---

### User Story 3 — Traceable, sourced APDU + layout (Priority: P2)

Every APDU byte and every field offset/length is transcribed from a verified reference and cited; nothing
is guessed. A wrong APDU can return garbage or lock a card, so unverified detail is a `TODO`, not a
plausible invention.

**Why this priority**: The repo's #1 rule and the physical risk of wrong card commands.

**Independent Test**: Inspect the APDU constants + the parser's offset table → each cites its source
(`tw-nhi-icc-service`, MIT); any uncertain value is an explicit `TODO`, not a fabricated byte.

**Acceptance Scenarios**:
1. **Given** the SELECT/READ APDU constants, **When** inspected, **Then** they match the cited reference
   and carry a source comment.
2. **Given** the field layout, **When** inspected, **Then** each offset/length cites the reference; no
   offset is guessed.

### Edge Cases
- **Encoding**: 姓名 is **Big5**, NUL-terminated within its field — decode Big5 up to the NUL; mojibake is a
  failure, not a warning.
- **ROC dates**: 生日/發卡日 are 民國 `YYYMMDD` (ASCII digits) → convert via the core `RocDate` (民國+1911).
- **Sex byte**: only 'M'/'F' are valid; any other byte fails loud (never guess a gender).
- **Short/garbage buffer**: < documented minimum → fail loud (not a 健保卡).
- **就醫序號 / 寫卡 / 上傳**: require 醫事人員卡 (HCA) + SAM + 健保 controlled software — **out of scope**;
  this feature reads only the basic segment.

## Requirements *(mandatory)*

### Functional Requirements
- **FR-001**: System MUST parse a 健保卡 基本資料段 byte buffer into a record with 卡號 / 姓名 / 身分證號 /
  生日 / 性別 / 發卡日, using field offsets/lengths transcribed from the verified reference.
- **FR-002**: System MUST decode 姓名 as **Big5** up to the field's NUL terminator; a mojibake result is a
  failure (fail loud), never emitted.
- **FR-003**: System MUST convert 生日/發卡日 from 民國 `YYYMMDD` via the core `RocDate` (民國+1911).
- **FR-004**: System MUST **fail loud** on a buffer shorter than the documented minimum or an invalid sex
  byte — never emit a partial/guessed Patient.
- **FR-005**: System MUST build a **base-R4 FHIR Patient** (identifier = 身分證, birthDate = 西元 date,
  gender = male/female); the Patient MUST pass strict Firely reparse. (No profile is claimed — a card read
  is demographics, not a PA case.)
- **FR-006**: System MUST live in a **new project `NhiPasFhir.CardReader`** with a `ProjectReference` to
  the core; the **core MUST NOT reference it** and MUST contain no PC/SC or APDU code (Hexagonal boundary).
- **FR-007**: The PC/SC transport MUST transmit the documented SELECT (健保 AID) + READ APDUs; the
  live-read test MUST be a **SkippableFact** that skips (not fails) when no reader/card is present.
- **FR-008**: Every APDU byte and field offset/length MUST cite its source (`tw-nhi-icc-service`, MIT);
  any uncertain value MUST be an explicit `TODO`, never a fabricated byte (Constitution II).
- **FR-009**: System MUST NOT process real patient data; all fixtures/tests use **fabricated** values
  (Constitution V).
- **FR-010**: Output/docs MUST state honestly that this reads only the **basic segment** (no 醫事人員卡
  needed) and that 就醫序號/寫卡/上傳 + 送件 are credential-gated and out of scope.

### Key Entities
- **Basic Segment (input)**: the raw bytes of a 健保卡 基本資料段 (≥ documented minimum length).
- **NhiCardBasic (internal)**: parsed record — 卡號, 姓名 (Big5), 身分證號, 生日 (ROC→date), 性別, 發卡日.
- **Patient (output)**: a base-R4 FHIR Patient (identifier/birthDate/gender) built from NhiCardBasic.
- **APDU constants (reference)**: SELECT (健保 AID) + READ (GET DATA) byte sequences, cited to the source.

## Success Criteria *(mandatory)*

### Measurable Outcomes
- **SC-001**: A fabricated basic segment parses to the exact documented fields — Big5 姓名 with **0
  mojibake**, ROC 生日 → correct 西元 date.
- **SC-002**: The built Patient passes strict Firely reparse (base-R4 structural validity).
- **SC-003**: The core project has **0** references to the card-reader project and **0** PC/SC/APDU code.
- **SC-004**: A short/invalid buffer **fails loud**; **0** partial/guessed Patients are ever emitted.
- **SC-005**: **0** guessed APDU bytes or offsets — every one cites the verified source; the live-read
  test skips (never fails) with no hardware.

## Assumptions
- APDU + 基本資料段 layout are transcribed from `tw-nhi-icc-service` (`magiclen`, MIT): SELECT
  `00 A4 04 00 10 D1 58 00 00 01 00 00 00 00 00 00 00 00 00 11 00`, READ `00 CA 11 00 02 00 00`; layout
  卡號[0:12] / 姓名[12:32] Big5 NUL-term / 身分證[32:42] / 生日[42:49] ROC / 性別[49] / 發卡日[50:57] ROC.
- PC/SC access via the `PCSC` (pcsc-sharp) NuGet; Big5 via `System.Text.Encoding.CodePages` — both in the
  CardReader project only, never the core.
- Patient output is **base R4** (promoting to TWCorePatient is a later, separate change if a standalone
  TW Core patient helper is added to the core).
- All data fabricated; no PHI. Live-read is hardware-dependent → SkippableFact.
- 就醫序號/寫卡/上傳 (HCA+SAM) and 送件 (VPN) are out of scope (work-order #13).

## Business reality — known walls (context; not solvable in code)

- **醫事人員卡 / SAM wall**: the basic segment is readable with just the 健保卡, but 就醫序號/寫卡/上傳 need
  a 醫事人員卡 + 健保 SAM + controlled software — not obtainable without medical-institution identity.
- **Reader/driver reality**: a PC/SC reader + driver must be present on the operating machine; this is why
  the live read is a SkippableFact, and why WebUSB was rejected (it fights the OS PC/SC driver — see the
  strategy notes).
