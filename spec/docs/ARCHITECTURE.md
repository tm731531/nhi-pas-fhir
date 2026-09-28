# ARCHITECTURE — interfaces, entities, engine, factory

> The target design. Reasoning: this system must handle **many IGs** (pas 癌藥, 免疫製劑, EMR, 長照)
> and **many case types**, all on a shared **TW Core** base, with **swappable transformations**. That is
> exactly Interface + Entities + Engine (Strategy) + Factory. Current code is the first concrete slice
> (CancerDrug); this doc is the shape we grow into. (Owner's instinct — confirmed.)

## The pipeline (one picture)

```
Caller: 診所系統 / form / (future) FHIR-EMR
        │  produces a neutral, non-FHIR
        ▼
┌───────────────────────┐
│ Domain Input: PACase  │   ← the stable INTERFACE the caller depends on
│ (patient, dx, drug,   │     (does NOT know FHIR — decouples capture from assembly)
│  indication, labs …)  │
└───────────┬───────────┘
            ▼
     AssemblerFactory.for(case)      ← FACTORY: pick the strategy by (IG, case type)
            │
   ┌────────┴─────────────────────────────┐
   ▼                 ▼                      ▼
CancerDrug        Immunologic          (future) LTC / EMR …
Assembler         Assembler            Assembler
   │  each is an ENGINE that transforms PACase → resources → Bundle
   │  and REUSES the shared TW Core clinical layer (Patient/Org/Encounter/Observation…)
   ▼
┌───────────────────────┐
│ Output: FHIR Bundle   │
└───────────┬───────────┘
            ▼
     Gate:  PreCheck (rules) + official Validator   → 0 errors
```

## The four pieces (in your terms)

### 1. Interface (契約)
Two stable contracts, so everything else can be swapped behind them:

- **`CaseSource`** — *where a PACase comes from*. Swappable input:
  `FormCaseSource` (today) / `HISAdapterCaseSource` (legacy clinic DB) / `FhirEmrCaseSource` (future).
- **`BundleAssembler`** — `assemble(case: PACase) -> Bundle`. One implementation per case type.
- **`Validator`** — `validate(bundle) -> Report`. Wrap the official validator; also the pre-check.

```python
class CaseSource(Protocol):
    def load(self, ref: str) -> "PACase": ...

class BundleAssembler(Protocol):
    ig: str            # e.g. "tw.gov.mohw.nhi.pas#1.2.6"
    case_type: str     # e.g. "cancer-drug"
    def assemble(self, case: "PACase") -> "Bundle": ...

class Validator(Protocol):
    def validate(self, bundle: "Bundle") -> "Report": ...
```

### 2. Entities (實體)
Two kinds, kept separate on purpose:
- **Domain entities** — `PACase` and its parts (neutral, non-FHIR: patient facts, diagnosis, applied
  drug + indication, weight/height, labs…). This is what the caller fills in.
- **FHIR resources** — the two layers we already have: `twcore.*` (clinical) + `pas.*` (assembler
  outputs: Claim/Coverage/MedicationRequest/Observation/Bundle).

The assembler's job = **map domain entities → FHIR entities**.

### 3. Engine (引擎 / 組裝器)
The `*Assembler` classes are the transformation engine (the **Strategy** pattern). Each knows how to
turn a PACase into a valid Bundle for its IG + case type, reusing the shared TW Core clinical builders.
Today: `CancerDrugAssembler` (what we validated to 0 errors). Later: `ImmunologicAgentAssembler`, etc.

### 4. Factory (工廠)
`AssemblerFactory` picks the right engine by `(ig, case_type)` — so the caller never hard-codes which
assembler to use; it just hands over a PACase and asks for a Bundle.

```python
class AssemblerFactory:
    _registry: dict[tuple[str, str], type[BundleAssembler]] = {}

    @classmethod
    def register(cls, ig: str, case_type: str):
        def deco(a): cls._registry[(ig, case_type)] = a; return a
        return deco

    @classmethod
    def for_case(cls, case: "PACase") -> BundleAssembler:
        return cls._registry[(case.ig, case.case_type)]()

@AssemblerFactory.register("tw.gov.mohw.nhi.pas#1.2.6", "cancer-drug")
class CancerDrugAssembler:  # ← today's logic, moved behind the interface
    ...
```

## How today's code maps onto this

| This doc | Today (already built) | Status |
|---|---|---|
| FHIR resource entities (2 layers) | `src/nhi_pas/twcore.py` + `pas.py` | ✅ done, validates 0 errors |
| BundleAssembler (CancerDrug) | logic currently inside `pas.Claim.build` + `examples/build_pa_bundle.py` | ⏭ extract into a `CancerDrugAssembler` class |
| PACase (domain input) | not yet — inputs are passed ad-hoc in the example | ⏭ define the neutral input model |
| Validator | `tools/validate.sh` + `precheck.py` | ✅ exists; ⏭ wrap behind the `Validator` interface |
| Factory | not yet | ⏭ add once there is a 2nd case type |
| CaseSource | not yet (capture is a separate feature) | ⏭ later |

## Platform decision (2026-09-28, owner)
This is **not** a single-case tool — it is a **framework/platform** intended to cover the whole Taiwan
FHIR IG family (pas 癌藥/免疫製劑, 重大傷病, 理賠, EMR, 長照…) and to be **open-sourced**. Multiplicity
is *certain* (the IG family is a fact, not a guess) and a framework *is* its abstraction — so the
Interface + Factory + core/plugin split is built **from the start**, not deferred. (This overrides the
usual YAGNI "wait for the 2nd case" rule, because the 2nd..Nth cases are known to be coming.)

## Design rules (so it stays clean & open-source-ready)
- **core / plugin separation**: `core` (interfaces, factory, TW Core layer, validator wrapper) depends on
  no specific IG; each IG/case-type lives in a `plugin` that depends only on `core`.
- **every plugin must pass the official validator at 0 errors** — enforced in CI. This is the framework's
  quality contract.
- **no PHI, no secrets** — a precondition for open-sourcing (already held).
- Adding a new IG/case = implement the interface + register + validate. Core is untouched.
- **Assemblers depend on the TW Core layer, never the reverse** — clinical layer knows nothing about pas.
- **Caller depends only on interfaces** (`PACase`, `BundleAssembler`, `Validator`) — never on concrete
  assemblers. That is what makes 免疫製劑 / 長照 pluggable later.
- Everything still ends at the same gate: **official validator → 0 errors** (Constitution I).
