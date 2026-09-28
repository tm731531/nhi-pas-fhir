"""TW Core clinical layer — the base FHIR datatypes + clinical resources that pas inherits.

Per spec 001 (FR-014/FR-015): this layer models the clinical facts (Patient, Practitioner,
Organization, Encounter, …) as the TW-Core-shaped entities the pas assembler consumes. It is
deliberately independent of how the data was captured (form today, FHIR-EMR pull later).

All field shapes are transcribed from official IG example instances (Bundle-bun-1.json) and the
pinned package tw.gov.mohw.nhi.pas#1.2.6. No hand-guessed values (Constitution II).
No real patient data (Constitution V).
"""
from __future__ import annotations

import re
from typing import Literal

from pydantic import BaseModel, Field, field_validator

# --- constants (verified against official examples) -------------------------

PAS_BASE = "https://nhicore.nhi.gov.tw/pas"                       # fullUrl / canonical base
SD = f"{PAS_BASE}/StructureDefinition"

SYS_V2_0203 = "http://terminology.hl7.org/CodeSystem/v2-0203"    # identifier type codes
SYS_ID_CARD = "http://www.moi.gov.tw"                            # 身分證號
SYS_RESIDENT = "http://www.immigration.gov.tw"                   # 居留證號
SYS_MED_LICENSE = "https://dep.mohw.gov.tw/DOMA"                 # 醫師證號
SYS_ORG_ID = f"{PAS_BASE}/CodeSystem/organization-identifier-tw"  # 醫事機構代碼
SYS_UCUM = "http://unitsofmeasure.org"

ID_CARD_RE = re.compile(r"^[A-Za-z][0-9]{9}$")

# identifier type codes (v2-0203) used by the IG
ID_TYPE_ID_CARD = "NNxxx"     # 身分證號
ID_TYPE_RESIDENT = "PRC"      # 居留證號
ID_TYPE_PASSPORT = "PPN"      # 護照
ID_TYPE_MR = "MR"             # 病歷號

# --- datatypes --------------------------------------------------------------


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

    @classmethod
    def just_text(cls, text: str) -> "CodeableConcept":
        return cls(text=text)


class Identifier(BaseModel):
    use: str | None = None
    type: CodeableConcept | None = None
    system: str | None = None
    value: str | None = None


class Reference(BaseModel):
    reference: str | None = None   # relative form "ResourceType/id" (resolves against entry fullUrl)
    display: str | None = None


class HumanName(BaseModel):
    use: str | None = None
    text: str | None = None
    family: str | None = None
    given: list[str] = Field(default_factory=list)


class Quantity(BaseModel):
    value: float | None = None
    unit: str | None = None
    system: str | None = None      # UCUM
    code: str | None = None        # UCUM code (required by IG for weight/height/quantity)

    @classmethod
    def ucum(cls, value: float, code: str, unit: str | None = None) -> "Quantity":
        return cls(value=value, unit=unit or code, system=SYS_UCUM, code=code)


class Meta(BaseModel):
    profile: list[str] = Field(default_factory=list)


# --- resource base (carries id → drives Bundle fullUrl + relative references) -


class Resource(BaseModel):
    resourceType: str
    id: str
    meta: Meta | None = None

    def ref(self) -> Reference:
        """Relative reference to this resource (resolves against its Bundle entry fullUrl)."""
        return Reference(reference=f"{self.resourceType}/{self.id}")

    def full_url(self) -> str:
        return f"{PAS_BASE}/{self.resourceType}/{self.id}"


def profile(name: str) -> Meta:
    return Meta(profile=[f"{SD}/{name}"])


# --- clinical resources -----------------------------------------------------


class Patient(Resource):
    resourceType: Literal["Patient"] = "Patient"
    identifier: list[Identifier]
    name: list[HumanName]
    gender: Literal["male", "female", "other", "unknown"]
    birthDate: str

    @field_validator("identifier")
    @classmethod
    def _idcard(cls, v: list[Identifier]) -> list[Identifier]:
        for i in v:
            if i.system == SYS_ID_CARD and i.value and not ID_CARD_RE.match(i.value):
                raise ValueError(f"idCardNumber must match ^[A-Za-z][0-9]{{9}}$, got {i.value!r}")
        return v

    @classmethod
    def with_id_card(cls, *, id: str, id_card: str, name: str, gender: str, birth_date: str) -> "Patient":
        return cls(
            id=id, meta=profile("Patient-twpas"),
            identifier=[Identifier(
                use="official",
                type=CodeableConcept.of(SYS_V2_0203, ID_TYPE_ID_CARD),
                system=SYS_ID_CARD, value=id_card,
            )],
            name=[HumanName(use="usual", text=name)],
            gender=gender,  # type: ignore[arg-type]
            birthDate=birth_date,
        )


class Practitioner(Resource):
    resourceType: Literal["Practitioner"] = "Practitioner"
    identifier: list[Identifier]
    name: list[HumanName] = Field(default_factory=list)

    @classmethod
    def of(cls, *, id: str, id_card: str, name: str) -> "Practitioner":
        return cls(
            id=id, meta=profile("Practitioner-twpas"),
            identifier=[Identifier(
                use="official",
                type=CodeableConcept.of(SYS_V2_0203, ID_TYPE_ID_CARD),
                system=SYS_ID_CARD, value=id_card,
            )],
            name=[HumanName(text=name)],
        )


class Organization(Resource):
    resourceType: Literal["Organization"] = "Organization"
    identifier: list[Identifier]
    name: str | None = None

    @classmethod
    def hospital(cls, *, id: str, org_code: str, name: str | None = None) -> "Organization":
        return cls(
            id=id, meta=profile("Organization-twpas"),
            identifier=[Identifier(system=SYS_ORG_ID, value=org_code)],
            name=name,
        )

    @classmethod
    def govt_nhi(cls, *, id: str = "org-nhi", org_code: str, name: str = "衛生福利部中央健康保險署") -> "Organization":
        # Bundle entry slice 'organizationOrg' expects TW Core Organization-govt-twcore.
        return cls(
            id=id,
            meta=Meta(profile=["https://twcore.mohw.gov.tw/ig/twcore/StructureDefinition/Organization-govt-twcore"]),
            identifier=[Identifier(system=SYS_ORG_ID, value=org_code)],
            name=name,
        )


class Encounter(Resource):
    resourceType: Literal["Encounter"] = "Encounter"
    status: str = "finished"          # TODO: confirm required value from Encounter-twpas SD
    class_: dict | None = Field(default=None, alias="class")
    subject: Reference | None = None

    model_config = {"populate_by_name": True}

    @classmethod
    def minimal(cls, *, id: str, patient_ref: Reference) -> "Encounter":
        # TODO: model serviceType (就醫科別) + class per Encounter-twpas SD; minimal shell for now.
        return cls(id=id, meta=profile("Encounter-twpas"), subject=patient_ref)
