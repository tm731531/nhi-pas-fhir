"""FHIR resource models (Profiles) for TWPAS — core critical path.

Every field/fixed-value/binding below is transcribed from the IG StructureDefinitions
(https://nhicore.nhi.gov.tw/pas/ , v1.2.6). Where a detail was not verified against the IG it is
marked `# TODO` rather than invented.

Modelled here (enough to assemble a valid request Bundle):
  BundleTWPAS, ClaimTWPAS, PatientTWPAS, PractitionerTWPAS, OrganizationTWPAS,
  MedicationRequestApplyTWPAS, EncounterTWPAS(min), CoverageTWPAS(min), plus shared FHIR datatypes.

Not yet modelled (supporting/optional resources) — see docs/03-artifacts-catalog.md:
  Observation* / DiagnosticReport* / ImagingStudy / Media / Procedure* / Substance* /
  Condition / CarePlan / ClinicalImpression / AllergyIntolerance / Composition / ClaimResponse.
"""
from __future__ import annotations

import re
from typing import Any, Literal

from pydantic import BaseModel, Field, field_validator

# --- Canonical constants (from IG) -----------------------------------------

SD = "https://nhicore.nhi.gov.tw/pas/StructureDefinition"

class P:  # profile canonical URLs (meta.profile fixed values)
    BUNDLE = f"{SD}/Bundle-twpas"
    CLAIM = f"{SD}/Claim-twpas"
    PATIENT = f"{SD}/Patient-twpas"
    PRACTITIONER = f"{SD}/Practitioner-twpas"
    ORGANIZATION = f"{SD}/Organization-twpas"
    MED_APPLY = f"{SD}/MedicationRequest-apply-twpas"

# identifier systems (fixed in IG)
SYS_ID_CARD = "http://www.moi.gov.tw"                 # 身分證號
SYS_RESIDENT = "http://www.immigration.gov.tw"        # 居留證號
SYS_MED_LICENSE = "https://dep.mohw.gov.tw/DOMA"      # 醫師證號
SYS_ORG_ID = "https://nhicore.nhi.gov.tw/pas/CodeSystem/organization-identifier-tw"  # 醫事機構代碼

ID_CARD_RE = re.compile(r"^[A-Za-z][0-9]{9}$")        # Patient.identifier:idCardNumber constraint

# --- Shared FHIR datatypes (subset) ----------------------------------------


class Coding(BaseModel):
    system: str | None = None
    code: str | None = None
    display: str | None = None


class CodeableConcept(BaseModel):
    coding: list[Coding] = Field(default_factory=list)
    text: str | None = None

    @classmethod
    def of(cls, system: str, code: str, display: str | None = None) -> "CodeableConcept":
        return cls(coding=[Coding(system=system, code=code, display=display)])


class Identifier(BaseModel):
    use: str | None = None
    system: str | None = None
    value: str | None = None
    type: CodeableConcept | None = None


class Reference(BaseModel):
    reference: str | None = None  # e.g. "Patient/xxx" or "urn:uuid:..."
    display: str | None = None


class HumanName(BaseModel):
    use: str | None = None
    text: str | None = None
    family: str | None = None
    given: list[str] = Field(default_factory=list)


class Quantity(BaseModel):
    value: float | None = None
    unit: str | None = None
    system: str | None = None
    code: str | None = None


class Meta(BaseModel):
    profile: list[str] = Field(default_factory=list)
    source: str | None = None


# --- Resource base ----------------------------------------------------------


class _Res(BaseModel):
    resourceType: str
    id: str | None = None
    meta: Meta | None = None


# --- Patient TWPAS ----------------------------------------------------------


class PatientTWPAS(_Res):
    resourceType: Literal["Patient"] = "Patient"
    identifier: list[Identifier]          # 1..2 (idCard / resident / MR)
    name: list[HumanName]                 # 1..1 name:usual, text 1..1 (<=40)
    gender: Literal["male", "female", "other", "unknown"]
    birthDate: str                        # YYYY-MM-DD

    @field_validator("identifier")
    @classmethod
    def _check_id_card(cls, v: list[Identifier]) -> list[Identifier]:
        for ident in v:
            if ident.system == SYS_ID_CARD and ident.value and not ID_CARD_RE.match(ident.value):
                raise ValueError(f"idCardNumber must match ^[A-Za-z][0-9]{{9}}$, got {ident.value!r}")
        return v

    @classmethod
    def with_id_card(cls, *, id_card: str, name: str, gender: str, birth_date: str) -> "PatientTWPAS":
        return cls(
            meta=Meta(profile=[P.PATIENT]),
            identifier=[Identifier(system=SYS_ID_CARD, value=id_card)],
            name=[HumanName(use="usual", text=name)],
            gender=gender,  # type: ignore[arg-type]
            birthDate=birth_date,
        )


# --- Practitioner TWPAS -----------------------------------------------------


class PractitionerTWPAS(_Res):
    resourceType: Literal["Practitioner"] = "Practitioner"
    identifier: list[Identifier]          # 1..2 (idCard / resident / medicalLicense)
    name: list[HumanName] = Field(default_factory=list)  # text or family required

    @classmethod
    def of(cls, *, id_card: str, name: str) -> "PractitionerTWPAS":
        return cls(
            meta=Meta(profile=[P.PRACTITIONER]),
            identifier=[Identifier(system=SYS_ID_CARD, value=id_card)],
            name=[HumanName(text=name)],
        )


# --- Organization TWPAS -----------------------------------------------------


class OrganizationTWPAS(_Res):
    resourceType: Literal["Organization"] = "Organization"
    identifier: list[Identifier]          # 1..1 醫事機構代碼 (system fixed, value bound to 特約醫事機構值集)
    name: str | None = None

    @classmethod
    def of(cls, *, org_code: str, name: str | None = None) -> "OrganizationTWPAS":
        return cls(
            meta=Meta(profile=[P.ORGANIZATION]),
            identifier=[Identifier(system=SYS_ORG_ID, value=org_code)],
            name=name,
        )


# --- MedicationRequest Apply TWPAS ------------------------------------------
# medicationCodeableConcept 1..1 bound to ValueSet「NHI-健保事前審查-用藥品項值集」


class Dosage(BaseModel):
    text: str | None = None
    # TODO: timing.code / route / doseAndRate bindings per IG


class MedicationRequestApplyTWPAS(_Res):
    resourceType: Literal["MedicationRequest"] = "MedicationRequest"
    status: str = "active"                 # TODO: confirm required value from IG
    intent: str = "order"                  # TODO: confirm required value from IG
    medicationCodeableConcept: CodeableConcept   # 1..1 (事前審查品項代碼)
    subject: Reference                     # 1..1 -> Patient TWPAS
    authoredOn: str | None = None
    requester: Reference | None = None
    dosageInstruction: list[Dosage] = Field(default_factory=list)  # 1..*
    reasonReference: list[Reference] = Field(default_factory=list)


# --- Encounter / Coverage (minimal; required Bundle entries) ----------------


class EncounterTWPAS(_Res):
    resourceType: Literal["Encounter"] = "Encounter"
    status: str = "finished"               # TODO: confirm from IG
    # serviceType -> 就醫科別 ; class ; subject  # TODO: model from Encounter-twpas SD


class CoverageTWPAS(_Res):
    resourceType: Literal["Coverage"] = "Coverage"
    status: str = "active"                 # TODO: confirm from IG
    beneficiary: Reference | None = None   # TODO: model from Coverage-twpas SD


# --- Claim TWPAS (the spine) ------------------------------------------------
# Fixed: status=active, type=institutional, use=preauthorization.
# subType  -> ValueSet nhi-apply-type   (申報類別: 1送核/2補件/3申復/4爭議/5補件)
# priority -> ValueSet nhi-tmhb-type    (申請案件類別: 1一般/3自主/4緊急)
# extension:encounter (1..1) -> Encounter-twpas
# supportingInfo: weight(1..1) + height(1..1) as Quantity; pregnancy(0..1); + referenced evidence.

VS_APPLY_TYPE = "https://nhicore.nhi.gov.tw/pas/ValueSet/nhi-apply-type"
VS_TMHB_TYPE = "https://nhicore.nhi.gov.tw/pas/ValueSet/nhi-tmhb-type"
CS_SUPPORTINGINFO_CATEGORY = "https://nhicore.nhi.gov.tw/pas/CodeSystem/supportinginfo-category"  # TODO verify id


class ClaimSupportingInfo(BaseModel):
    sequence: int
    category: CodeableConcept
    valueQuantity: Quantity | None = None
    valueBoolean: bool | None = None
    valueReference: Reference | None = None


class ClaimTWPAS(_Res):
    resourceType: Literal["Claim"] = "Claim"
    status: Literal["active"] = "active"
    type: CodeableConcept = Field(
        default_factory=lambda: CodeableConcept.of(
            "http://terminology.hl7.org/CodeSystem/claim-type", "institutional"
        )
    )
    use: Literal["preauthorization"] = "preauthorization"
    subType: CodeableConcept              # 申報類別 (ValueSet nhi-apply-type)
    priority: CodeableConcept             # 申請案件類別 (ValueSet nhi-tmhb-type)
    patient: Reference                    # -> Patient TWPAS
    created: str                          # 申請日期 (dateTime)
    enterer: Reference                    # -> Practitioner TWPAS (申請醫師)
    provider: Reference                   # -> Organization TWPAS
    supportingInfo: list[ClaimSupportingInfo] = Field(default_factory=list)  # 2..* (weight+height min)
    # extension:encounter (1..1) — kept as a plain reference for now
    encounter: Reference | None = None    # maps to Claim.extension:encounter
    identifier: list[Identifier] = Field(default_factory=list)  # orig_file_name / old_acpt_no

    @classmethod
    def build(
        cls,
        *,
        subtype_code: str,        # 申報類別 code, e.g. "1" (送核)
        priority_code: str,       # 申請案件類別 code, e.g. "1" (一般)
        patient_ref: str,
        provider_ref: str,
        enterer_ref: str,
        encounter_ref: str,
        created: str,
        weight_kg: float,
        height_cm: float,
    ) -> "ClaimTWPAS":
        return cls(
            meta=Meta(profile=[P.CLAIM]),
            subType=CodeableConcept.of(VS_APPLY_TYPE, subtype_code),
            priority=CodeableConcept.of(VS_TMHB_TYPE, priority_code),
            patient=Reference(reference=patient_ref),
            provider=Reference(reference=provider_ref),
            enterer=Reference(reference=enterer_ref),
            encounter=Reference(reference=encounter_ref),
            created=created,
            supportingInfo=[
                ClaimSupportingInfo(
                    sequence=1,
                    category=CodeableConcept.of(CS_SUPPORTINGINFO_CATEGORY, "weight"),
                    valueQuantity=Quantity(value=weight_kg, unit="kg"),
                ),
                ClaimSupportingInfo(
                    sequence=2,
                    category=CodeableConcept.of(CS_SUPPORTINGINFO_CATEGORY, "height"),
                    valueQuantity=Quantity(value=height_cm, unit="cm"),
                ),
            ],
        )


# --- Bundle TWPAS -----------------------------------------------------------
# type = collection (fixed). Required entries: claim, encounter, patient, practitioner(1..*),
# organization, medicationRequestApply(1..*), coverage, organizationOrg(TWCoreOrganizationGovt).


class BundleEntry(BaseModel):
    fullUrl: str | None = None
    resource: dict[str, Any]


class BundleTWPAS(_Res):
    resourceType: Literal["Bundle"] = "Bundle"
    type: Literal["collection"] = "collection"
    identifier: Identifier | None = None
    timestamp: str | None = None          # 緊急報備日期
    entry: list[BundleEntry] = Field(default_factory=list)

    @classmethod
    def assemble(cls, *resources: _Res, identifier: str | None = None) -> "BundleTWPAS":
        """Wrap resources into a collection Bundle. (Entry ordering/slicing per IG is TODO-validated.)"""
        return cls(
            meta=Meta(profile=[P.BUNDLE]),
            identifier=Identifier(value=identifier) if identifier else None,
            entry=[BundleEntry(resource=r.model_dump(exclude_none=True)) for r in resources],
        )
