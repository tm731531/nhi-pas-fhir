"""FHIR resource models (Profiles) for TWPAS.

Core-first: only resources on the submit→response critical path are modelled with real fields.
Everything else is a TODO stub — we do NOT invent fields. Pull each from the IG StructureDefinition
before implementing:  https://nhicore.nhi.gov.tw/pas/artifacts.html

Modelled (core): Bundle TWPAS, Claim TWPAS, MedicationRequest Apply/Treat TWPAS,
Patient/Practitioner/Organization TWPAS, Condition TWPAS, Coverage TWPAS,
ClaimResponse TWPAS, OperationOutcome TWPAS.

TODO (stub): Observation* (Cancer Stage / Patient Assessment / Lab / Genetic / SOAP),
DiagnosticReport* / ImagingStudy / Media, Procedure* / Substance*, Encounter*,
CarePlan / ClinicalImpression / AllergyIntolerance / Composition (免疫製劑 SOAP), etc.
See docs/03-artifacts-catalog.md for the full list and status.
"""
# TODO: model core Profiles as pydantic models once each StructureDefinition is pulled from the IG.
#       Keeping this a placeholder rather than fabricating field sets.
