# nhi-pas-fhir

A Python study/reference implementation of Taiwan NHI's **Prior Authorization** (事前審查)
FHIR Implementation Guide (IG).

- **Source of truth (SoT):** <https://nhicore.nhi.gov.tw/pas/> — 臺灣健保事前審查實作指引 v1.2.6
  published by 衛生福利部中央健康保險署 (NHI). FHIR base: **R4 (4.0.1)**.
- **Goal:** faithfully mirror the IG's *interface* (schemas + API contract) and *workflow*,
  documented clearly, so the domain becomes buildable. Code follows docs, not the reverse.

> **pas is one IG in a family.** Taiwan has a whole FHIR IG set — all inheriting **TW Core** —
> incl. EMR exchange (電子病歷) and a **Long-Term Care IG (長期照顧)**. See `docs/04-ig-landscape.md`.
> This repo starts with pas but is structured so sibling IGs (EMR, LTC) plug in later on a shared base.

## What "事前審查 / Prior Authorization" is (one line)

Expensive NHI-reimbursed therapies — mainly **癌症用藥 (cancer drugs)** and
**免疫製劑 (immunologic agents)** — must be **applied for and approved before use**.
A provider packages the clinical evidence into a FHIR `Bundle`, uploads it to NHI, and NHI
adjudicates (approve / reject / request more info).

## Coverage policy (honest)

The IG is large (~40 profiles, 44 ValueSets, 22 CodeSystems, 3 Bundle types, 65 examples).
This repo covers it in two layers:

| Layer | Coverage |
|---|---|
| **Docs / catalog** (`docs/`) | **Complete** — every IG artifact is catalogued & mapped |
| **Code** (`src/`) | **Core-first** — the Bundle→submit→ClaimResponse path + key resources; the rest is stubbed with `# TODO: map from IG` and never fabricated |

Anything not yet verified against the IG is marked `TODO` — we do **not** invent fields.

## Layout

```
docs/
  00-overview.md          What 事前審查 is, and how FHIR models it
  01-workflow.md          End-to-end flow (submit → validate → review → result) + case types
  02-interface.md         The API contract (from TWPAS Server/Client CapabilityStatements)
  03-artifacts-catalog.md Full catalogue of every IG artifact (profiles/valuesets/codesystems)
src/nhi_pas/
  interface.py            Python Protocols mirroring TWPAS Client/Server capabilities
  resources.py            Core resource models (Bundle/Claim/Patient/... — subset, typed)
  valuesets.py            (TODO) code enums subset
examples/                 Build a sample Bundle (TODO)
```

## Status

Progress: A ✅ core models · B ✅ worked example + 核刪 pre-check · next C (LTC IG) → D (app decision).

## License / data

Study material. Uses **no real patient data**. All identifiers in examples are fabricated.

## Governance & strategy (this is a serious project)

- `CLAUDE.md` — working rules (never fabricate FHIR fields; English code; dev-branch; verify).
- `AGENTS.md` — agent-team staffing map (spin up on demand).
- `docs/strategy/BUSINESS.md` — the money side: goal, market, ICP, product, monetisation, competitor.
- Strategy memory: `~/.claude/projects/-home-tom/memory/project-nhi-pas-fhir.md`.

## Quickstart

```bash
make venv          # create .venv + install deps
. .venv/bin/activate
make test          # pytest (should pass)
```
