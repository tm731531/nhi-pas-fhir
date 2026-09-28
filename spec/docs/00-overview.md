# 00 — Overview (概觀)

> SoT: <https://nhicore.nhi.gov.tw/pas/> · IG v1.2.6 · FHIR R4 (4.0.1)

## 1. The business problem

Some NHI-reimbursed therapies are very expensive (a targeted cancer drug can be tens of
thousands of NT$ per month). To gate misuse, NHI requires **prior authorization (事前審查)**:

> The provider must **apply and get approval BEFORE administering / claiming** the therapy,
> proving the patient meets the reimbursement indication (correct diagnosis, stage, prior line, etc.).

Two therapy domains are in scope:
- **癌症用藥** (cancer drugs)
- **免疫製劑** (immunologic agents) — uses an S/O/A/P structured note (see workflow)

## 2. What the IG standardises

Before: every hospital used its own format → NHI hard to process, providers re-implement per site.
The IG fixes this by defining **one FHIR-based interface** everyone implements against:

- **Schema layer** — WHAT data looks like: `Bundle`, `Claim`, `MedicationRequest`, `Patient`,
  `Observation`, `DiagnosticReport`, … each constrained by a **Profile** (`* TWPAS`) and bound to
  **ValueSets/CodeSystems** (the allowed codes).
- **API layer** — HOW it moves: a RESTful contract declared by two **CapabilityStatements**
  (TWPAS **Server** = NHI side, TWPAS **Client** = provider side) + standard **SearchParameters**.

## 3. Mental model for a software architect

FHIR here is just a **standardised REST+JSON API + schema library** for healthcare, plus a
specialisation mechanism (Profile / Implementation Guide). Mapping to familiar concepts:

| FHIR concept | Familiar analogue |
|---|---|
| Resource (`Claim`, `Patient`, …) | data model / entity / DTO + schema |
| Profile (`Claim TWPAS`) | subclass / narrow a base type for a context |
| ValueSet / CodeSystem | enum / controlled vocabulary |
| StructureDefinition | JSON Schema / XSD (validatable) |
| Bundle | batch / transaction request (many ops, one call) |
| REST + SearchParameter | standard CRUD + query params |
| CapabilityStatement | OpenAPI / Swagger (self-describing API) |
| Implementation Guide (this IG) | a full API-spec package for one domain |

## 4. Why this matters strategically (the leverage)

Because the interface is **standard**, a tool built against FHIR works across **every** provider
that implements the IG — build once, run everywhere. Pre-standard, integration was per-site custom
work (un-scalable). Post-standard (Taiwan's 2026 FHIR push), one integration reaches all — which is
exactly what lets a small, fast team build product where large legacy vendors struggle.

## 5. Where the real difficulty (眉角 / tacit knowledge) lives

The spec is public; the *tacit* knowledge is not:
- the drug-code ↔ indication-code constraints (get one wrong → 核刪 / clawback),
- which applications get rejected and how to phrase them to pass,
- the 補件 / 申復 / 爭議審議 (resubmit / appeal / dispute) cycle.

This repo captures the **public interface**. The tacit part is what domain immersion supplies.
See `01-workflow.md` for the flow and `03-artifacts-catalog.md` for the full artifact map.
