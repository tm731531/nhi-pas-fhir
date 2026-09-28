# Feature Specification: Validator-clean Cancer-Drug Prior-Authorization Bundle

**Feature Branch**: `001-cancer-drug-pa-bundle`
**Created**: 2026-09-28
**Status**: Draft (for review)
**Input**: Produce a 癌藥事前審查 (cancer-drug prior-authorization) request `Bundle` that **passes the
official HL7 FHIR validator with 0 errors** against the pinned NHI pas IG, and pre-checks the
drug↔indication constraint before submission. Governed by `.specify/memory/constitution.md`.

> **Constitution binding:** I (correctness proven by validator), II (authoritative IG package only),
> III (advisory, not authority), IV (fail-safe/fail-loud), V (test-first, no PHI), VI (traceable/versioned).

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Assemble a conformant request Bundle (Priority: P1)

A clinic system holds the structured facts of a cancer-drug PA case (patient, physician, institution,
diagnosis, staging, the applied drug + indication, weight/height, supporting evidence). It needs to
turn those facts into a **FHIR `Bundle` that the NHI system will accept** — i.e. that passes the
official IG validator.

**Why this priority**: Nothing else matters if the Bundle is not conformant — a non-conformant Bundle
is rejected/退件 at the door. This is the foundation the whole product sits on.

**Independent Test**: Provide a fabricated but complete cancer-drug case → generate the Bundle →
run `make validate FILE=...` → **0 errors**. Delivers value alone (a correct submission artifact).

**Acceptance Scenarios**:
1. **Given** a complete fabricated cancer-drug case, **When** the Bundle is generated and validated
   against IG `tw.gov.mohw.nhi.pas#1.2.6`, **Then** the validator reports **0 errors**.
2. **Given** a case missing a required element (e.g. no diagnosis), **When** generated, **Then** the
   system refuses to emit a "valid" Bundle and reports which required element is missing (fail-loud).

---

### User Story 2 — Pre-check drug↔indication before submit (Priority: P2)

Before a clinician submits, the system checks that the applied drug code is allowed for the chosen
給付適應症 (indication) — catching a 核刪 (clawback) cause before it happens.

**Why this priority**: This is the clinic-facing value (Layer A). But it depends on P1 (a well-formed
Bundle) to extract the pair from.

**Independent Test**: Feed a (drug, indication) pair that the IG profile/rules disallow → the system
**blocks with a clear reason**; feed an allowed pair → it passes.

**Acceptance Scenarios**:
1. **Given** drug `KC009612B5` with indication `C50P1`, **When** pre-checked, **Then** result is CLEAR.
2. **Given** drug `KC009612B5` with indication `C99X9`, **When** pre-checked, **Then** result is
   BLOCKED with "would be 核刪; allowed indications are {…}".
3. **Given** a drug with **no loaded rule**, **When** pre-checked, **Then** result is a **WARNING**
   (unverified), never a false CLEAR (Constitution IV).

---

### User Story 3 — Traceability & advisory framing (Priority: P3)

Every value the system emits can be traced to an IG artifact + version, and every pre-check result
states it is decision-support, not a guarantee.

**Why this priority**: Required by the constitution (III, VI) and by medical/legal defensibility, but
does not block a first working Bundle.

**Independent Test**: Inspect any generated field → it carries provenance (artifact + IG version);
every pre-check output carries the advisory disclaimer.

**Acceptance Scenarios**:
1. **Given** any emitted CodeSystem/ValueSet URL, **When** inspected, **Then** it matches the pinned
   IG package (not a hand-guessed value).
2. **Given** any pre-check report, **When** shown, **Then** it states "advisory; final responsibility
   rests with the clinician and NHI adjudication; human review required".

### Edge Cases
- Applied drug present but indication absent → BLOCK (cannot verify) — never silent pass.
- Rule data older than the pinned IG version → flag as **stale**, do not trust.
- Validator unavailable → the system must NOT claim a Bundle is "valid"; it reports "unverified".
- Bundle references between entries → must resolve via `fullUrl` (a relative reference with no
  `fullUrl` is an error).

## Requirements *(mandatory)*

### Functional Requirements
- **FR-001**: System MUST assemble a `Bundle` of `type = collection` conforming to
  `Bundle-twpas`, containing all required entries: claim, encounter, patient, practitioner (≥1),
  organization, medicationRequestApply (≥1), coverage, and government organization (TWCoreOrganizationGovt).
- **FR-002**: System MUST give every Bundle entry a `fullUrl` (e.g. `urn:uuid:…`) and make all
  intra-Bundle references resolve to those fullUrls.
- **FR-003**: System MUST populate the `Patient` identifier so it matches a defined slice
  (idCardNumber/residentNumber/passportNumber/medicalRecord) — including the `type.coding`
  discriminator (system `…v2-0203`, code e.g. `NNxxx`), not merely system+value.
- **FR-004**: System MUST include on `Claim`: `item` (≥1), `insurance` (≥1), `diagnosis` (with a
  `sequence` value of exactly one `1`), and `extension:encounter` (not a plain `encounter` element).
- **FR-005**: System MUST express weight/height `supportingInfo` as `Quantity` with `value`, `unit`,
  `system = http://unitsofmeasure.org`, and `code` (`kg`/`cm`).
- **FR-006**: System MUST omit empty arrays/elements (absent, not `[]`).
- **FR-007**: System MUST use CodeSystem/ValueSet/profile URLs and fixed values taken **only** from
  the pinned IG package `tw.gov.mohw.nhi.pas#1.2.6`; no value may be hand-guessed (Constitution II).
- **FR-008**: System MUST validate a generated Bundle against the pinned IG using the official HL7
  validator and treat **0 errors** as the definition of "conformant".
- **FR-009**: System MUST pre-check applied (drug, indication) pairs and BLOCK disallowed pairs with a
  human-readable reason; unknown/unloaded rules MUST warn, never falsely clear (Constitution IV).
- **FR-010**: System MUST NOT process real patient data; all examples/tests use fabricated identifiers.
- **FR-011**: Every pre-check/validation output MUST state its advisory nature and its IG version.
- **FR-012**: The drug↔indication rule set MUST load from an authoritative artifact (預檢規則 CQL IG /
  ValueSets); the seeded subset is explicitly labelled as such until the full set is ingested.

*Unclear (to resolve in /speckit-clarify):*
- **FR-013**: Terminology binding conformance — is v1 "done" at structural `0 errors` (`-tx n/a`), or
  MUST it also pass full terminology validation (kg/cm/ICD codes via a tx server)?
  [NEEDS CLARIFICATION: which terminology gate for v1?]
- **FR-014**: Source of the structured case data (manual input vs EMR extraction) —
  [NEEDS CLARIFICATION: assumed provided/out of scope for this feature?]

### Key Entities
- **PA Case (input)**: the structured facts of one cancer-drug PA application (patient, physician,
  institution, diagnosis + staging, applied drug + indication + quantity/dosage, weight/height,
  supporting reports). De-identified; not FHIR-shaped.
- **Request Bundle (output)**: the `Bundle-twpas` collection assembled from a PA Case.
- **Drug↔Indication Rule**: an allowed set of 給付適應症 codes per drug code (authoritative source).
- **Validation Report**: validator outcome (errors/warnings) + provenance; the correctness gate.

## Success Criteria *(mandatory)*

### Measurable Outcomes
- **SC-001**: A generated cancer-drug Bundle from a complete fabricated case validates with **0 errors**
  against `tw.gov.mohw.nhi.pas#1.2.6` (baseline today: 45 errors → target 0).
- **SC-002**: Pre-check correctly BLOCKS every known-bad drug↔indication pair in the seeded rule set and
  CLEARS every known-good pair (100% on the seeded test set).
- **SC-003**: 0 hand-guessed URLs/codes remain in emitted output — every value traces to the pinned IG.
- **SC-004**: An unknown-drug pre-check never returns CLEAR (0 false clears in tests).
- **SC-005**: Re-validating an official IG example still yields 0 errors (toolchain regression guard).

## Assumptions
- Pinned IG version is **`tw.gov.mohw.nhi.pas#1.2.6`**; upgrades are explicit, re-validated events (VI).
- Scope of this feature is **cancer-drug** PA only; 免疫製劑 (immunologic agents) is a later feature.
- The structured PA Case is provided to the system (data capture/EMR integration is out of scope here).
- Terminology-server checks (UCUM/ICD/SNOMED) are handled as a separate, explicit gate; this feature's
  primary DoD is structural `0 errors` unless FR-013 resolves otherwise.
- All data is fabricated; no PHI (Constitution V).
- Official validator + IG package are fetched via `tools/fetch_validation_assets.sh` (not committed).
