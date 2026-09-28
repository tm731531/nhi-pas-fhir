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
    PAS_BASE, SD, SYS_UCUM, CodeableConcept, Coding, Quantity, Reference, Resource, Meta, profile, prune,
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
SYS_SNOMED = "http://snomed.info/sct"
SYS_LOINC = "http://loinc.org"
CS_NCI_THESAURUS = f"{PAS_BASE}/CodeSystem/nci-thesaurus"


class ObservationLabResult(Resource):
    """Observation-laboratory-result-twpas — 檢驗 (tests); the simplest report that satisfies the
    priority=1/3 and C90/C91/C92 supportingInfo invariants (performer is a Practitioner)."""
    resourceType: Literal["Observation"] = "Observation"
    status: str = "final"
    category: list[CodeableConcept]
    code: CodeableConcept
    subject: Reference
    effectiveDateTime: str
    performer: list[Reference]
    valueQuantity: Quantity

    @classmethod
    def test(cls, *, id: str, patient_ref: Reference, performer_ref: Reference, effective: str,
             loinc_code: str, value: float, unit: str) -> "ObservationLabResult":
        return cls(
            id=id, meta=profile("Observation-laboratory-result-twpas"),
            category=[CodeableConcept.of(CS_SUPPORTINGINFO, "tests")],
            code=CodeableConcept.of(SYS_LOINC, loinc_code),
            subject=patient_ref, effectiveDateTime=effective, performer=[performer_ref],
            valueQuantity=Quantity(value=value, unit=unit),
        )


class ObservationDiagnostic(Resource):
    """Observation-diagnostic-twpas — 基因資訊 (geneInfo); satisfies the priority=1/3 report invariant."""
    resourceType: Literal["Observation"] = "Observation"
    status: str = "final"
    category: list[CodeableConcept]
    code: CodeableConcept
    subject: Reference
    effectiveDateTime: str
    performer: list[Reference]
    valueString: str

    @classmethod
    def gene(cls, *, id: str, patient_ref: Reference, performer_ref: Reference,
             effective: str, loinc_code: str = "69548-6", value: str = "基因檢測報告結果") -> "ObservationDiagnostic":
        return cls(
            id=id, meta=profile("Observation-diagnostic-twpas"),
            category=[CodeableConcept.of(CS_SUPPORTINGINFO, "geneInfo")],
            code=CodeableConcept.of(SYS_LOINC, loinc_code),
            subject=patient_ref, effectiveDateTime=effective, performer=[performer_ref],
            valueString=value,
        )


class ObservationCancerStage(Resource):
    """Observation-cancer-stage-twpas — a supporting report (satisfies the priority=1/3 invariant)."""
    resourceType: Literal["Observation"] = "Observation"
    status: str = "final"
    category: list[CodeableConcept]
    code: CodeableConcept
    subject: Reference
    effectiveDateTime: str
    performer: list[Reference]
    valueCodeableConcept: CodeableConcept

    @classmethod
    def figo(cls, *, id: str, patient_ref: Reference, performer_ref: Reference,
             effective: str, stage_code: str, stage_system_code: str = "385361009") -> "ObservationCancerStage":
        return cls(
            id=id, meta=profile("Observation-cancer-stage-twpas"),
            category=[CodeableConcept.of(CS_SUPPORTINGINFO, "cancerStage")],
            code=CodeableConcept.of(SYS_SNOMED, stage_system_code),
            subject=patient_ref, effectiveDateTime=effective, performer=[performer_ref],
            valueCodeableConcept=CodeableConcept.of(CS_NCI_THESAURUS, stage_code),
        )


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
              diagnosis: list[Diagnosis], item: list[Item],
              supporting_reports: list[tuple[str, Reference]] | None = None) -> "Claim":
        # weight/height are supportingInfo seq 1/2; supporting reports (imagingReport/cancerStage/
        # examinationReport/geneInfo/…) follow — priority 1/3 requires at least one report.
        si = [
            SupportingInfo(sequence=1, category=CodeableConcept.of(CS_SUPPORTINGINFO, "weight"),
                           valueQuantity=Quantity.ucum(weight_kg, "kg")),
            SupportingInfo(sequence=2, category=CodeableConcept.of(CS_SUPPORTINGINFO, "height"),
                           valueQuantity=Quantity.ucum(height_cm, "cm")),
        ]
        for i, (cat_code, ref) in enumerate(supporting_reports or [], start=3):
            si.append(SupportingInfo(sequence=i,
                                     category=CodeableConcept.of(CS_SUPPORTINGINFO, cat_code),
                                     valueReference=ref))
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
            supportingInfo=si,
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
                               resource=prune(r.model_dump(exclude_none=True, by_alias=True)))
                   for r in resources],
        )
