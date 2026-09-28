# CONTRACTS & TEST PLAN — write this before the code

> Docs first → tests second → code last. The behaviours below are the contract; the tests encode them;
> the code is "done" only when the tests are green **and** the golden bundle validates at 0 errors.
> (Owner principle: get the docs clear so we can test well.)

## 1. Contracts (what each piece must guarantee)

### `PACase` (input)
- A neutral, non-FHIR description of one case. Carries: `ig`, `case_type`, `patient`, `provider`,
  `vitals`, `created`, and case-specific `data`. Knows nothing about FHIR.

### `ICaseAssembler` (interface)
- Exposes `ig`, `case_type`, and `assemble(case: PACase) -> Bundle`.
- **C1**: `assemble` MUST return a `Bundle` whose JSON, when validated against the pinned IG, has
  **0 errors** (structural + terminology) for a well-formed `PACase`.
- **C2**: every Bundle entry MUST have an absolute `fullUrl`; all references resolve to those fullUrls.
- **C3**: no empty arrays/elements in the output (pruned).

### `AbstractCaseAssembler` (abstract base)
- **C4**: builds the shared TW Core clinical resources (Patient/Practitioner/Organization/Encounter/
  Coverage/govt-Org) and weight/height supportingInfo — identically for every case type.
- **C5**: delegates case-specific parts to abstract hooks (`build_case`); a subclass that does not
  implement the hooks cannot be instantiated (ABC enforcement).

### `AssemblerFactory`
- **C6**: `for_case(case)` returns the assembler registered for `(case.ig, case.case_type)`.
- **C7**: an unregistered `(ig, case_type)` raises `KeyError` — never a wrong/silent assembler
  (fail-loud, Constitution IV).
- **C8**: `register(ig, case_type)` stamps `ig`/`case_type` onto the class and makes it discoverable.

### Case-type implementation (e.g. `CancerDrugAssembler`)
- **C9**: MUST reproduce its golden bundle in `spec/reference-bundles/<case>.json` (same shape/values).
- **C10**: MUST satisfy every IG invariant for its case (e.g. cancer: the C90/priority supporting-report
  rule) — proven by the validator, not by us asserting it.

## 2. Test plan (each test maps to a contract)

Fast tests (no Java; run in CI on every change):
| Test | Verifies |
|---|---|
| `test_factory_dispatch` | C6 — a cancer-drug PACase → `CancerDrugAssembler` |
| `test_factory_unregistered_raises` | C7 — unknown (ig, case_type) → `KeyError` |
| `test_abstract_cannot_instantiate` | C5 — instantiating an assembler missing hooks fails |
| `test_bundle_shape` | C2/C4 — required entries present; every entry has absolute fullUrl |
| `test_no_empty_arrays` | C3 — no `"[]"` / empty `coding` in output |
| `test_reproduces_golden` | C9 — assembler output == `spec/reference-bundles/cancer-drug-pa-bundle.json` |

Slow gate (Java validator; run on demand / pre-merge):
| Gate | Verifies |
|---|---|
| `make validate FILE=<built bundle>` → **0 errors** | C1/C10 — official structural + terminology conformance |

## 3. Definition of done (this framework slice)
1. All fast tests green.
2. `CancerDrugAssembler` output **reproduces the golden bundle**.
3. Golden bundle **passes the official validator at 0 errors**.
4. Factory correctly dispatches and fails loud on the unknown.
Only then do we add the next case type (a new subclass + registration + its own golden + validation).
