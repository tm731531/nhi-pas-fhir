# nhi-pas-fhir Constitution

> Medical software. Correctness is not a preference — it is the product. This constitution
> supersedes convenience, speed, and cleverness. When in doubt, stop and verify.

## Core Principles

### I. Correctness is proven, never asserted (NON-NEGOTIABLE)
No output is "correct" because a human or an AI read a web page and believed it. Correctness means:
- every generated FHIR artifact **passes the official HL7 FHIR validator** against the pinned IG, and
- every clinical/billing rule (e.g. drug↔indication) **traces to an authoritative IG artifact**.
Unvalidated output is labelled *unverified* and MUST NOT be presented as usable. "It runs" ≠ "it is correct."

### II. Authoritative source of truth only
The source of truth is the **machine-readable IG package** (FHIR NPM `tw.gov.mohw.nhi.pas`,
StructureDefinitions / ValueSets / CodeSystems) at a **pinned version**. Web-page summaries, blog
posts, and AI recollection are **research leads, never sources**. Every field, code, and URL carries
a provenance note (artifact + IG version). No value is hand-guessed; if unknown → `TODO: unverified`.

### III. Advisory, not authority (patient-safety boundary)
This system **assists**; it never **decides**. The pre-check is a decision-support aid, not a
guarantee of reimbursement or clinical appropriateness. Every result states its limits. Final
responsibility remains with the licensed clinician and the payer's adjudication. This positioning
is stated in-product and in every report. We do not practise medicine and we do not guarantee 給付.

### IV. Fail safe and fail loud
Ambiguity must never silently become a "pass":
- unknown drug / missing rule → **warn**, never a false CLEAR;
- rule data older than its source → **flag as stale**, do not trust;
- validator unavailable → **block "verified" claims**, do not fake them.
A false CLEAR (missed 核刪) is the worst outcome; the system is biased toward surfacing doubt.

### V. Test-first, and never real patient data (NON-NEGOTIABLE)
TDD: a failing test/spec precedes implementation. Every rule and constraint has a test. **No real
PHI, ever** — examples use fabricated identifiers only. Any patient-pattern feature (the "1+1"
layer) is **de-identified pattern intelligence, never a named blacklist**, with a documented legal
basis reviewed before code.

### VI. Traceability & versioning
Every schema element and rule links to its IG artifact and IG version. When the IG changes, changes
are diffed and re-validated, never silently absorbed. Our released behaviour is pinned to a stated
IG version; upgrades are explicit, tested events.

## Regulatory & Compliance Constraints
- **FHIR conformance:** R4 (4.0.1); conform to TW Core + the target NHI IG; validate before ship.
- **Taiwan law:** 個人資料保護法 (PDPA), 醫療法, 醫師法 — patient data handling and the 1+1 layer
  must have a legal opinion on de-identification before implementation.
- **Standards to hold:** track HL7 FHIR / 健保署 IG updates; do not diverge from published profiles.

## Development Workflow & Quality Gates
- **Spec-driven:** constitution → spec → (clarify) → plan → tasks → implement. No production code
  without an agreed spec.
- **Git on `dev`**, never `main`. Commit only when asked. English code/commits; 健保 terms in原文.
- **Validation gate (definition of done):** `make test` green **and** the FHIR validator passes on
  any generated artifact **and** every new value has provenance. Missing any → not done.
- **No fabricated FHIR fields** — the repo-level rule in `CLAUDE.md` is subordinate to and consistent
  with Principle I & II here.

## Governance
This constitution supersedes other practices in this repo. Amendments require: a written rationale,
Tom's approval, and a migration/re-validation note. Every plan and review must check compliance with
these principles; violations block "done." Complexity must be justified against Principle I.

**Version**: 0.1.0 (draft — under discussion) | **Ratified**: TODO | **Last Amended**: 2026-09-28
