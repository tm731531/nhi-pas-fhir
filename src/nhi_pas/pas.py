"""pas assembler layer — applies 事前審查 (TWPAS) constraints onto TW Core clinical data and
assembles the request Bundle.

All shapes/values transcribed from official IG example Bundle-bun-1.json (pinned
tw.gov.mohw.nhi.pas#1.2.6). Verified values only; anything not yet confirmed is marked TODO
(Constitution I/II). No PHI (V).
"""
from __future__ import annotations

from typing import Any, Literal

from pydantic import BaseModel, Field

from .twcore import (
    PAS_BASE, SD, SYS_UCUM, CodeableConcept, Coding, Quantity, Reference, Resource, Meta, profile,
)

# --- CodeSystems (verified against official Claim example) ------------------
CS_APPLY_TYPE = f"{PAS_BASE}/CodeSystem/nhi-apply-type"            # Claim.subType 申報類別
CS_TMHB_TYPE = f"{PAS_BASE}/CodeSystem/nhi-tmhb-type"             # Claim.priority 申請案件類別
CS_SUPPORTINGINFO = f"{PAS_BASE}/CodeSystem/nhi-supporting-info-type"
CS_ORDER_TYPE = f"{PAS_BASE}/CodeSystem/nhi-order-type"           # Claim.item.productOrService 醫令類別
CS_CONTINUATION = f"{PAS_BASE}/CodeSystem/nhi-continuation-status"  # 續用註記
CS_LINE_OF_THERAPY = f"{PAS_BASE}/CodeSystem/nhi-line-of-therapy"   # 用藥線別
CS_MEDICATION = f"{PAS_BASE}/CodeSystem/nhi-medication"           # 用藥品項
SYS_CLAIM_TYPE = "http://terminology.hl7.org/CodeSystem/claim-type"

# extensions (verified)
EXT_CLAIM_ENCOUNTER = f"{SD}/extension-claim-encounter"
EXT_REQUESTED_SERVICE = f"{SD}/extension-requestedService"
EXT_DX_RECORDED_DATE = "http://hl7.org/fhir/us/davinci-pas/StructureDefinition/extension-diagnosisRecordedDate"

SYS_ICD10CM_TW = "https://twcore.mohw.gov.tw/ig/twcore/CodeSystem/icd-10-cm-2023-tw"


# --- Coverage (verified: status/beneficiary/payor) --------------------------


class Coverage(Resource):
    resourceType: Literal["Coverage"] = "Coverage"
    status: str = "active"
    beneficiary: Reference
    payor: list[Reference]

    @classmethod
    def of(cls, *, id: str, patient_ref: Reference, nhi_org_ref: Reference) -> "Coverage":
        return cls(id=id, meta=profile("Coverage-twpas"),
                   beneficiary=patient_ref, payor=[nhi_org_ref])


# --- MedicationRequest Apply (verified: status=on-hold, intent=plan) --------


class MedicationRequestApply(Resource):
    resourceType: Literal["MedicationRequest"] = "MedicationRequest"
    status: str = "on-hold"          # verified (official medReq-apply)
    intent: str = "plan"             # verified
    medicationCodeableConcept: CodeableConcept
    subject: Reference
    authoredOn: str | None = None
    # dosageInstruction is terminology-heavy (timing GTS + nhi freq, SNOMED route, doseAndRate);
    # kept as verified raw structures supplied by the caller to avoid fabricating internals.
    dosageInstruction: list[dict[str, Any]] = Field(default_factory=list)

    @classmethod
    def of(cls, *, id: str, drug_code: str, patient_ref: Reference,
           dosage: list[dict[str, Any]], authored_on: str | None = None) -> "MedicationRequestApply":
        return cls(
            id=id, meta=profile("MedicationRequest-apply-twpas"),
            medicationCodeableConcept=CodeableConcept.of(CS_MEDICATION, drug_code),
            subject=patient_ref, authoredOn=authored_on, dosageInstruction=dosage,
        )


# --- Claim (the spine — verified against Claim-cla-1 / Bundle-bun-1) ---------


class Extension(BaseModel):
    url: str
    valueReference: Reference | None = None
    valueDate: str | None = None


class SupportingInfo(BaseModel):
    sequence: int
    category: CodeableConcept
    valueQuantity: Quantity | None = None
    valueBoolean: bool | None = None
    valueReference: Reference | None = None


class Diagnosis(BaseModel):
    sequence: int
    diagnosisCodeableConcept: CodeableConcept
    type: list[CodeableConcept] = Field(default_factory=list)
    extension: list[Extension] = Field(default_factory=list)


class Insurance(BaseModel):
    sequence: int
    focal: bool
    coverage: Reference


class Item(BaseModel):
    sequence: int
    productOrService: CodeableConcept
    modifier: list[CodeableConcept] = Field(default_factory=list)
    programCode: list[CodeableConcept] = Field(default_factory=list)
    quantity: Quantity | None = None
    extension: list[Extension] = Field(default_factory=list)


class Claim(Resource):
    resourceType: Literal["Claim"] = "Claim"
    status: Literal["active"] = "active"
    type: CodeableConcept = Field(default_factory=lambda: CodeableConcept.of(SYS_CLAIM_TYPE, "institutional"))
    use: Literal["preauthorization"] = "preauthorization"
    subType: CodeableConcept
    priority: CodeableConcept
    patient: Reference
    created: str
    enterer: Reference
    provider: Reference
    insurance: list[Insurance]
    item: list[Item]
    diagnosis: list[Diagnosis] = Field(default_factory=list)
    supportingInfo: list[SupportingInfo] = Field(default_factory=list)
    extension: list[Extension] = Field(default_factory=list)

    @classmethod
    def build(cls, *, id: str, subtype_code: str, subtype_display: str,
              priority_code: str, priority_display: str,
              patient: Resource, enterer: Resource, provider: Resource,
              encounter: Resource, coverage: Resource,
              created: str, weight_kg: float, height_cm: float,
              diagnosis: list[Diagnosis], item: list[Item]) -> "Claim":
        return cls(
            id=id, meta=profile("Claim-twpas"),
            subType=CodeableConcept.of(CS_APPLY_TYPE, subtype_code, subtype_display),
            priority=CodeableConcept.of(CS_TMHB_TYPE, priority_code, priority_display),
            patient=patient.ref(), enterer=enterer.ref(), provider=provider.ref(),
            created=created,
            extension=[Extension(url=EXT_CLAIM_ENCOUNTER, valueReference=encounter.ref())],
            insurance=[Insurance(sequence=1, focal=True, coverage=coverage.ref())],
            item=item,
            diagnosis=diagnosis,
            supportingInfo=[
                SupportingInfo(sequence=1, category=CodeableConcept.of(CS_SUPPORTINGINFO, "weight"),
                               valueQuantity=Quantity.ucum(weight_kg, "kg")),
                SupportingInfo(sequence=2, category=CodeableConcept.of(CS_SUPPORTINGINFO, "height"),
                               valueQuantity=Quantity.ucum(height_cm, "cm")),
            ],
        )


# --- helpers to build a drug item + diagnosis (verified shapes) -------------


def drug_item(*, sequence: int, med_ref: Reference, tbl_count: int,
              continuation_code: str = "1", continuation_display: str = "初次使用",
              line_code: str = "1", line_display: str = "第一線治療",
              program_text: str | None = None) -> Item:
    return Item(
        sequence=sequence,
        extension=[Extension(url=EXT_REQUESTED_SERVICE, valueReference=med_ref)],
        productOrService=CodeableConcept.of(CS_ORDER_TYPE, "1", "藥品"),
        modifier=[
            CodeableConcept.of(CS_CONTINUATION, continuation_code, continuation_display),
            CodeableConcept.of(CS_LINE_OF_THERAPY, line_code, line_display),
        ],
        programCode=[CodeableConcept.just_text(program_text)] if program_text else [],
        quantity=Quantity(value=tbl_count, system=SYS_UCUM, code="{tbl}"),
    )


def diagnosis(*, sequence: int = 1, icd10cm: str, recorded_date: str, text: str) -> Diagnosis:
    return Diagnosis(
        sequence=sequence,
        extension=[Extension(url=EXT_DX_RECORDED_DATE, valueDate=recorded_date)],
        diagnosisCodeableConcept=CodeableConcept.of(SYS_ICD10CM_TW, icd10cm),
        type=[CodeableConcept.just_text(text)],
    )


# --- Bundle assembler (fullUrl-aware) ---------------------------------------


class BundleEntry(BaseModel):
    fullUrl: str
    resource: dict[str, Any]


class Bundle(Resource):
    resourceType: Literal["Bundle"] = "Bundle"
    type: Literal["collection"] = "collection"
    entry: list[BundleEntry] = Field(default_factory=list)

    @classmethod
    def assemble(cls, *resources: Resource, id: str) -> "Bundle":
        return cls(
            id=id, meta=profile("Bundle-twpas"),
            entry=[BundleEntry(fullUrl=r.full_url(),
                               resource=r.model_dump(exclude_none=True, by_alias=True))
                   for r in resources],
        )
