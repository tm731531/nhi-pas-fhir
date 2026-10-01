# Feature Specification: HIS Ingest Feed — real NHI upload-format parser → FHIR

**Feature Branch**: `002-his-ingest-feed`
**Created**: 2026-10-01
**Status**: Implemented 2026-10-01 — 每日上傳 XML ingest done (84 tests green, base-R4 gate + golden); 費用申報 CSV + vendor exports deferred; 讀卡/送件 are separate features
**Input**: Replace the teaching-only `t|d|p` stand-in parser with a C# parser that reads a **real NHI
format** — the 健保署 **每日上傳 XML** (`RECS>REC>MSH/MB1/MB2`, Big5) — normalizes it, and feeds the
**existing FHIR converter** so a **fabricated sample file produces a FHIR Bundle that validates with 0
errors**. This is sub-project 1 of the "一條龍" (讀卡 → 進料 → FHIR → 送件); only **進料 (ingest)** is in
scope here. Governed by `.specify/memory/constitution.md`.

> **Constitution binding:** I (correctness proven by validator), II (authoritative source only — field
> semantics transcribed from a verified reference, never invented), III (advisory, not authority),
> IV (fail-safe/fail-loud), V (test-first, no PHI), VI (traceable/versioned).

> **North-star (context, NOT this spec's scope):** the full pipeline is
> `CardReader (讀卡/pcsc) → Ingest (進料) → Core (FHIR IG) → Submit (送件/VPN)`, built as a Hexagonal
> architecture where **every adapter depends on the pure FHIR core and the core depends on none of
> them**. This feature builds the **Ingest** inbound adapter. CardReader and Submit are separate future
> features; Submit cannot actually transmit without medical-institution credentials (see §Business
> reality, wall #1/#4, and work-order #13).

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Parse a real upload file into a conformant Bundle (Priority: P1)

A clinic exports (or the HIS emits) a 健保署 每日上傳 XML file containing one or more 就醫 records with
their 醫令 detail lines. The system parses that real-format file and turns each record into a **FHIR
Bundle the downstream accepts** — i.e. one that passes the validator.

**Why this priority**: This is the whole point of the ingest adapter — bridging messy real 健保 file
formats into the clean FHIR core. Without it, the FHIR library can only be fed hand-built structured
input; with it, real files flow in.

**Independent Test**: Provide a **fabricated** 每日上傳 XML sample → parse → generate Bundle →
`make validate FILE=...` → **0 errors**. Delivers value alone (real file → conformant artifact).

**Acceptance Scenarios**:
1. **Given** a fabricated 每日上傳 XML with one REC (MSH + MB1 + ≥1 MB2), **When** parsed and converted,
   **Then** a FHIR Bundle is produced and the validator reports **0 errors**.
2. **Given** a REC missing a field the FHIR output requires (e.g. no A12 身分證), **When** processed,
   **Then** the system **fails loud** naming the missing field — it does NOT emit a Bundle with a
   fabricated or blank identifier (Constitution IV).
3. **Given** a file with multiple RECs, **When** parsed, **Then** each REC yields its own record (one
   MB1 + its MB2 lines), none merged or dropped silently.

---

### User Story 2 — Clean architectural boundary (Priority: P1)

The real-format parser lives in a **separate ingest project** that depends on the FHIR core; the core
never depends on it. Someone consuming the open-source FHIR core gets **zero** vendor/format/encoding
code.

**Why this priority**: This is the design guarantee that keeps the open-source FHIR library clean while
the ingest layer absorbs real-world mess. It is as important as the parsing itself — a parser welded
into the core would defeat the "一條龍 that stays open-source-clean" goal.

**Independent Test**: Inspect `NhiPasFhir.csproj` dependencies → it references only Firely (no ingest,
no encoding libs). Inspect `NhiPasFhir.Ingest.csproj` → it `ProjectReference`s the core. The dependency
arrow points **ingest → core only**.

**Acceptance Scenarios**:
1. **Given** the core project, **When** its references are inspected, **Then** it contains no reference
   to the ingest project and no Big5/vendor/XML-dialect code.
2. **Given** the ingest project, **When** built, **Then** it compiles against the core via a project
   reference and produces Bundles using the core's assemblers/builders unchanged.

---

### User Story 3 — Faithful, traceable field mapping (Priority: P2)

Every field the parser reads maps to a documented NHI field whose meaning is transcribed from a
**verified reference**, not guessed. Fields that cannot be confidently mapped are marked `TODO` with the
source to confirm against — never silently invented.

**Why this priority**: Medical correctness and the repo's #1 rule. A plausible-but-wrong field mapping
is worse than an admitted gap.

**Independent Test**: Inspect the parser's field table → each mapped field cites its source
(`MB1.A12 = 身分證號`, etc.); any unmapped field is an explicit `TODO`, not a fabricated value.

**Acceptance Scenarios**:
1. **Given** any field the parser consumes, **When** inspected, **Then** its NHI meaning is documented
   and traceable to the verified reference (go-tw-his-parser `his_import.go` field comments and/or the
   官方 媒體申報/上傳格式 spec).
2. **Given** a field present in the file but not yet mapped, **When** processed, **Then** it is recorded
   as unmapped (`TODO`), never coerced into a FHIR element with an assumed meaning.

### Edge Cases
- **Encoding**: the real 每日上傳 XML is **Big5**, not UTF-8. The parser MUST decode Big5 correctly;
  a mojibake 姓名/診斷 is a failure, not a warning.
- **ROC dates**: A13 生日 / A17 就診日期 are 民國 `YYYMMDD(HHMMSS)` → MUST convert via the existing
  `RocDate` logic (民國+1911), not treated as Gregorian.
- **No 媒體申報/上傳 FHIR IG exists**: therefore the output Bundle is validated for **base R4 structural
  correctness** (plus TW Core profiles where an entity cleanly applies, e.g. Patient); there is **no**
  NHI-upload-specific TW profile to claim conformance to. This limitation is stated, not faked.
- **就醫序號 (A18) / 寫卡 / 上傳**: parsing A18 from a file is fine; actually obtaining/writing a live
  就醫序號 needs HCA + SAM and is **out of scope** (see Business reality wall #1).
- **Malformed file**: unparseable XML → fail loud with a parse error; never emit a partial "valid" Bundle.
- **Validator unavailable**: the system MUST NOT claim a Bundle is "valid" — it reports "unverified".

## Requirements *(mandatory)*

### Functional Requirements
- **FR-001**: System MUST parse the 健保署 每日上傳 XML structure (`RECS` → `REC` → `MSH` + `MB1` +
  `MB2*`), decoding **Big5**, into a normalized in-memory record (one MB1 case + its MB2 order lines).
- **FR-002**: System MUST live in a **new project `NhiPasFhir.Ingest`** that has a `ProjectReference`
  to the core; the **core MUST NOT reference the ingest project** nor contain any format/encoding/vendor
  code (Hexagonal boundary; dependency arrow ingest → core only).
- **FR-003**: System MUST **move the existing `MediaDeclaration/` layer into `NhiPasFhir.Ingest`** (it is
  an ingest concern), leaving the core = `Core/` + `Plugins/` (pure FHIR) [+ `Ltc/` flagged for a later,
  separate move — out of scope here].
- **FR-004**: System MUST convert a normalized record into a FHIR **collection Bundle** by reusing the
  existing converter/assemblers unchanged where possible (Patient + Claim + MedicationRequest +
  Coverage); any new mapping MUST use the core's builders, not re-implement FHIR shaping in ingest.
- **FR-005**: System MUST convert 民國 dates (A13/A17) with the existing `RocDate` logic; it MUST NOT
  emit a date as if it were Gregorian.
- **FR-006**: System MUST source every field's NHI meaning from a **verified reference** (go-tw-his-parser
  `his_import.go` comments and/or the 官方 上傳/媒體申報 格式 spec). No field meaning may be invented;
  unmappable fields MUST be marked `TODO` with the source to confirm (Constitution II).
- **FR-007**: System MUST **fail loud** when a field required by the FHIR output is absent (e.g. A12
  身分證 for the Patient identifier) — it MUST NOT emit a Bundle with a blank/fabricated required value
  (Constitution IV).
- **FR-008**: System MUST validate a generated Bundle with the official HL7 validator and treat **0
  errors** (base R4 structural; TW Core where applicable) as the conformance gate for this feature.
- **FR-009**: System MUST NOT process real patient data; all sample files/tests use **fabricated**
  identifiers and data (Constitution V).
- **FR-010**: Output and docs MUST state honestly that **媒體申報/上傳 has no FHIR IG**, so the Bundle
  conforms to base R4 (+ TW Core where clean), not to an NHI-upload-specific profile.
- **FR-011**: Scope is the **每日上傳 XML** format only. 費用申報 CSV and vendor-specific exports
  (耀聖/展望/看診大師) are explicitly **deferred** (YAGNI — no real samples; many variants).

### Key Entities
- **Upload File (input)**: a fabricated 健保署 每日上傳 XML (`RECS>REC>MSH/MB1/MB2`, Big5).
- **Normalized Record (internal)**: one 就醫 case (MB1 fields) + its 醫令 lines (MB2 fields), keyed by
  the official field IDs, with ROC dates converted and encoding resolved.
- **Request Bundle (output)**: the FHIR collection Bundle assembled from the normalized record via the
  core's builders (base R4 / TW Core; no upload-specific IG profile).
- **Field Map (reference)**: the documented, source-cited mapping from NHI field IDs → FHIR elements;
  unmapped fields carried as explicit `TODO`s.

## Success Criteria *(mandatory)*

### Measurable Outcomes
- **SC-001**: A fabricated 每日上傳 XML sample parses and produces a FHIR Bundle that validates with
  **0 errors** (base R4 structural; TW Core where applicable).
- **SC-002**: The core project has **0** references to ingest code and **0** Big5/vendor/XML-dialect
  code; the dependency arrow is ingest → core only (verified by inspecting the two `.csproj` files).
- **SC-003**: **0** invented field meanings — every consumed field cites a verified source; every
  unmapped field is an explicit `TODO`.
- **SC-004**: A record missing a FHIR-required field (e.g. A12) **fails loud** naming the field; **0**
  Bundles are ever emitted with a blank/fabricated required identifier (0 false "valid"s in tests).
- **SC-005**: Big5 round-trip is correct — a sample with Chinese 姓名/診斷 parses to the exact expected
  characters (0 mojibake) in tests.
- **SC-006**: A golden test reproduces the expected Bundle byte-for-byte from the sample file (toolchain
  regression guard), mirroring the existing golden-test pattern.

## Assumptions
- Scope = **每日上傳 XML** ingest only; 費用申報 CSV + vendor exports are later features (FR-011).
- The FHIR output reuses the existing converter/assemblers; where the upload format carries data the
  current converter does not yet map, that mapping is added in the core's builder layer, not faked.
- There is **no 媒體申報/上傳 FHIR IG** — validation target is base R4 (+ TW Core where clean), stated
  honestly (FR-010).
- go-tw-his-parser (`Saki-tw/go-tw-his-parser`, MIT) is used as a **read-only field-semantics reference**
  (we write our own C#); no Go code is linked or redistributed.
- All data fabricated; no PHI (Constitution V). Validator + IG package fetched via
  `tools/fetch_validation_assets.sh` (not committed).
- CardReader (讀卡) and Submit (送件) are **separate future features**; Submit's real transmission is
  blocked on medical-institution credentials (work-order #13).

## Business reality — known walls (context; NOT solvable in code, recorded so we don't fool ourselves)

These distortion points apply to commercializing the full 一條龍. They do **not** block this ingest
feature (which is pure, local, fabricated-data technical work), but are recorded here so the clean
architecture is never mistaken for a clear commercial path.

1. **Medical-institution identity wall** — HCA 醫事憑證 + 健保 VPN are issued only to a 醫事機構. No
   institution identity ⇒ cannot read live 就醫序號 or submit. Resolved only by operating as / partnering
   with a licensed clinic. *(Blocks CardReader-live + Submit, not Ingest.)*
2. **NHI-mandated dirty components** — official 讀卡/上傳 components are closed, Windows-only, legacy;
   adapters must wrap them. The Hexagonal boundary contains the mess; the core stays clean.
3. **HIS vendor lock-in** — clinic HIS exports may be deliberately unfriendly / break on version bumps /
   be contractually closed to third parties; a sidecar reading their exports can be treated as a threat.
4. **Certification / compliance** *(verify specifics before relying)* — 核刪 prediction (CQL) may edge
   toward **SaMD (醫療器材軟體, TFDA)**; storing formal 病歷 triggers the 電子病歷 regulation
   (timestamp/signature/immutability/retention); health data is **特種個資 (個資法 §6)**. Each needs
   verification, not assumption.
5. **Revenue ceiling** — NHI pays nothing extra for FHIR; value is cost-avoidance (avoid 核刪 / cut
   filing labor), so pricing is capped and must be concrete.
6. **Perpetual maintenance** — NHI codes/indications/formats re-version yearly (cf. `112.08.25 版更`);
   核刪 rules are a shifting black box; per-clinic 醫令 codes vary — the ingest parser is ongoing
   whack-a-mole, not one-and-done.
7. **Open-source × profit tension** — open-sourcing the whole thing means revenue is in the **service
   layer** (hosting / integration / maintenance / certification support), not in selling code. The
   architecture supports this (core open; profit in the operated service), but the model must be chosen
   deliberately.
