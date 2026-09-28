# EXPLAINER — what this project is, for a systems owner (not a FHIR expert)

> Audience: an experienced systems/SaaS person who is **new to healthcare FHIR**. Everything here maps
> to things you already know (APIs, schemas, validation). Read this before diving into the code.
> 給看得懂系統、但不熟醫療 FHIR 的老闆看的。每個概念都對照你已經會的東西。

## 1. FHIR in one line
**FHIR = a standardized REST API + JSON schema library for healthcare.**
It is just JSON over HTTP with agreed shapes. No magic.
中文:FHIR 就是「醫療界講好的一套 JSON 格式 + API」。

## 2. The jargon, mapped to what you already know

| FHIR term | You already know it as |
|---|---|
| Resource (Patient, Claim, Observation…) | table / entity / DTO + schema |
| Profile (`Patient-twpas`) | subclass / narrow a base type; a DB CHECK constraint |
| ValueSet / CodeSystem | enum / lookup table (allowed codes for a field) |
| StructureDefinition | the JSON Schema for a resource |
| Implementation Guide (IG) | a vendor's full API spec package: OpenAPI + examples + tests |
| Bundle | a batch / transaction request (many resources, one payload) |
| Validator | the vendor's conformance test tool |
| invariant / constraint | a business rule (DB trigger / assert), written in FHIRPath |

No new concepts — only new names for things you use in every SaaS build.

## 3. What we are actually doing (the method = third-party API integration you've done before)

Treat NHI (健保署) as a vendor whose API you must produce acceptable data for:

1. **Get the authoritative spec** — download the official IG package (`.../pas/package.tgz`). This is the
   machine-readable truth, *not* a web page. (Constitution: authoritative source only.)
2. **Read the official examples** — `package/example/*.json`. They are correct by definition.
3. **Write code** to emit that JSON — our two layers: `twcore.py` (clinical layer) + `pas.py` (assembler).
4. **Validate** with the official validator — `make validate FILE=...` = the machine judge.
5. **Fix what it flags, repeat** — until **0 errors** (we went 45 → 26 → 3 → 0).

This is exactly how you integrate any vendor API (their OpenAPI + Postman examples + conformance suite).
Only the domain (healthcare codes/rules) is new.

## 4. What "0 errors" means (why we anchor everything to it)
A **machine** — not a human opinion — certifies our output conforms to the national standard.
In healthcare, that is the line between **accepted vs 退件 (rejected)**. So in this repo, "correct" is
defined as "the official validator says 0 errors", never "it looks right / it runs".
Note: it caught **7+ values the AI had guessed wrong** — proof that guessing is not allowed here.

## 5. What FHIR changes about the *process* (the owner's view — Tom's framing, sharpened)

The business goal is old: **事前審查 (prior authorization)** = get approval **before** giving an
expensive drug, so 健保 doesn't waste points on ineligible use.

What FHIR adds is **standardization**, and standardization **enables a pre-submission check**:

```
Before (non-standard data):   fill in → SUBMIT → 被核刪/退件 → 補件/申復 → resubmit   (waste, churn)
After  (FHIR standardized):   fill in → CHECK locally (validator + rules) → submit CLEAN
```

So Tom's summary is right: **FHIR = (a) standardize all the data/process/rules + (b) it lets you add a
check step before you submit** — cutting 核刪 (clawback) and 退件 (rejection) waste.

Two distinct "checks" — don't conflate them:
- **事前審查** = a *clinical/reimbursement* approval by NHI (a business process that predates FHIR).
- **Our pre-check** (the product, Layer A) = a *format + rule* check on our side **before** we submit the
  事前審查 application. FHIR's machine-checkable spec is what makes this pre-check reliable.

## 6. What the owner needs to hold (you don't need to memorise FHIR)
1. **Where the spec lives** — the official IG package. NHI changes rules → re-download.
2. **Who the judge is** — the official validator (`make validate`). 0 errors = it counts.
3. **The method** — mirror the official examples, validate, fix. Repeat.

One sentence you can say to any doctor / IT dept / investor:
> "Everything we generate passes the NHI's own official FHIR validator with 0 errors."

## 7. Why this is a moat
It requires **domain** (knowing the codes/rules) **and** systems (building the generator that emits valid
data). Systems you already have; domain we are acquiring (e.g. via [employer] + the official package). Few people
have both — that's the defensible position.
