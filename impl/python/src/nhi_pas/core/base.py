"""AbstractCaseAssembler — shared assembly behaviour; case types override the abstract hooks.

Shared (concrete): builds the TW Core clinical resources (Patient/Practitioner/Organization/Encounter/
Coverage/govt-Org), weight/height supportingInfo, and the fullUrl-aware Bundle.
Case-specific (abstract): the drug/item/diagnosis/supporting-report specifics per case type.
"""
from __future__ import annotations

from abc import ABC, abstractmethod

from .. import twcore as tc
from .. import pas
from ..pas import Bundle, Claim, Item, Diagnosis, Reference
from ..twcore import Resource
from .interfaces import ICaseAssembler, PACase


class AbstractCaseAssembler(ABC, ICaseAssembler):
    ig: str = ""
    case_type: str = ""

    # --- shared: build the TW Core clinical layer from a PACase -------------
    def _clinical(self, case: PACase):
        p = case.patient
        pr = case.provider
        patient = tc.Patient.with_id_card(id="pat-1", id_card=p["id_card"], name=p["name"],
                                          gender=p["gender"], birth_date=p["birth_date"])
        doctor = tc.Practitioner.of(id="pra-1", id_card=pr["doctor_id_card"], name=pr["doctor_name"])
        hospital = tc.Organization.hospital(id="org-hosp", org_code=pr["hospital_code"],
                                            name=pr.get("hospital_name"))
        nhi = tc.Organization.govt_nhi()
        enc = tc.Encounter.minimal(id="enc-1", patient_ref=patient.ref())
        cov = pas.Coverage.of(id="cov-1", patient_ref=patient.ref(), nhi_org_ref=nhi.ref())
        return patient, doctor, hospital, nhi, enc, cov

    def assemble(self, case: PACase) -> Bundle:
        patient, doctor, hospital, nhi, enc, cov = self._clinical(case)
        # case-specific parts (each implementation fills these from its IG)
        extras, med_items, diagnoses, reports = self.build_case(case, patient, doctor, hospital)
        claim = Claim.build(
            id="cla-1",
            subtype_code="1", subtype_display="送核",
            priority_code="1", priority_display="一般事前審查申請",
            patient=patient, enterer=doctor, provider=hospital, encounter=enc, coverage=cov,
            created=case.created,
            weight_kg=case.vitals["weight_kg"], height_cm=case.vitals["height_cm"],
            diagnosis=diagnoses, item=med_items,
            supporting_reports=[(cat, r.ref()) for cat, r in reports],
        )
        report_res = [r for _, r in reports]
        return Bundle.assemble(claim, enc, patient, doctor, hospital, *extras, cov, nhi,
                               *report_res, id="bun-demo")

    # --- abstract hooks: each case type implements these --------------------
    @abstractmethod
    def build_case(self, case: PACase, patient: Resource, doctor: Resource, hospital: Resource
                   ) -> tuple[list[Resource], list[Item], list[Diagnosis], list[tuple[str, Resource]]]:
        """Return (extra resources e.g. MedicationRequest, Claim.items, diagnoses, [(category, report)])."""
        raise NotImplementedError
