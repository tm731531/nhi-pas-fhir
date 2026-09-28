# nhi-pas-fhir — a spec-driven, multi-language Taiwan NHI FHIR framework

A framework for generating **officially-valid** Taiwan NHI FHIR artifacts (starting with 事前審查 /
Prior Authorization). One language-agnostic **spec** is the source of truth; **each language
implementation** conforms to it and is gated by the **official HL7 FHIR validator (0 errors)**.

> SoT for the domain: <https://nhicore.nhi.gov.tw/pas/> · IG `tw.gov.mohw.nhi.pas#1.2.6` · FHIR R4.

## The model (read this first)

```
                ┌──────────────────────────────────────────┐
   YOU MONITOR  │  spec/   ← the single source of truth      │
                │    · constitution (.specify/memory/…)      │
                │    · specs/         feature specs           │
                │    · docs/          IG mirror, architecture,│
                │                     explainer, strategy      │
                │    · reference-bundles/  the validated       │
                │                     "correct" JSON (golden)  │
                └───────────────────┬──────────────────────────┘
                                    │  spec changes drive all impls
        ┌───────────────────────────┼───────────────────────────┐
        ▼                           ▼                           ▼
  impl/python/                impl/csharp/                impl/java/
  (reference / AI)            (Firely SDK — primary)      (HAPI — enterprise)
        │                           │                           │
        └───────────────┬───────────┴───────────┬───────────────┘
                        ▼                        ▼
             shared tools/ + .fhir/  →  official HL7 validator
                        every impl must reach:  0 errors
```

- **`spec/` is the master.** It is language-agnostic (rules, profiles map, architecture, the golden
  reference bundle). When it changes, every `impl/` must be updated to match — ideally in the same PR.
- **Each `impl/<lang>/` is a conforming implementation.** They never depend on each other; they each
  reproduce the `spec/reference-bundles/*.json` and each pass the official validator at **0 errors**.
- **Correctness is machine-proven, not asserted** (see `spec/` → constitution). "It runs" ≠ "it is correct".

## Layout

```
spec/                         ← source of truth (what YOU monitor)
  specs/                      feature specifications (spec-kit format)
  docs/                       EXPLAINER, ARCHITECTURE, IG mirror (00–04), strategy, validation evidence
  reference-bundles/          the validated "correct" bundles all impls must reproduce
impl/
  python/                     reference implementation (pydantic) — proven 0 errors
  csharp/                     (planned) primary — Firely .NET SDK; fits Taiwan medical (.NET) ecosystem
  java/                       (planned) HAPI FHIR — enterprise/hospital
tools/                        shared, language-agnostic: fetch IG package + run official validator
.fhir/                        (gitignored) fetched IG package + validator jar
.specify/ .claude/            spec-kit workflow + skills
CLAUDE.md AGENTS.md           working rules + agent staffing
```

## Quickstart

```bash
# 1. shared: fetch the authoritative IG package + official validator (large, gitignored)
make fetch-validator

# 2. build + validate the Python implementation
cd impl/python && make venv && make validate     # builds a bundle, validates → expect 0 errors

# validate ANY fhir json against the pinned IG (from repo root):
make validate FILE=spec/reference-bundles/cancer-drug-pa-bundle.json
```

## Adding a language implementation (the contract)
An `impl/<lang>/` is "done" when it (a) reproduces `spec/reference-bundles/*.json` for each supported
case type and (b) passes the official validator at **0 errors** (structural + terminology). Nothing
else counts as conformant.

## Governance & strategy
`spec/docs/EXPLAINER.md` (what FHIR is, for a systems owner) · `spec/docs/ARCHITECTURE.md`
(interface/entities/engine/factory — the framework design) · `spec/docs/strategy/BUSINESS.md` (the
money side). Working rules: `CLAUDE.md`. Constitution: `.specify/memory/constitution.md`.

No real patient data, ever. All examples use fabricated identifiers.
