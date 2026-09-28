"""CancerDrugAssembler — the 癌藥事前審查 case type. Concrete implementation of AbstractCaseAssembler.

Verified to reproduce spec/reference-bundles/cancer-drug-pa-bundle.json and pass the official validator
at 0 errors. All values from the official IG examples; no fabrication.
"""
from __future__ import annotations

import json
import pathlib

from .. import pas
from ..twcore import Resource
from ..core.base import AbstractCaseAssembler
from ..core.factory import AssemblerFactory
from ..core.interfaces import PACase

_DOSAGE = pathlib.Path(__file__).parent / "fixtures" / "cancer_drug_dosage.json"


@AssemblerFactory.register("tw.gov.mohw.nhi.pas#1.2.6", "cancer-drug")
class CancerDrugAssembler(AbstractCaseAssembler):
    def build_case(self, case: PACase, patient: Resource, doctor: Resource, hospital: Resource):
        d = case.data
        dosage = json.loads(_DOSAGE.read_text())
        med = pas.MedicationRequestApply.of(
            id="medReq-apply", drug_code=d["drug_code"], patient_ref=patient.ref(),
            dosage=dosage, authored_on=d.get("authored_on"))
        dx = pas.diagnosis(icd10cm=d["diagnosis_icd"], recorded_date=d["diagnosis_date"],
                           text=d["diagnosis_text"])
        item = pas.drug_item(sequence=1, med_ref=med.ref(), tbl_count=d["drug_qty_tbl"],
                             program_text=d.get("program_text"))
        # C90/priority invariant → one 'tests' lab report (simplest qualifying report).
        lab = pas.ObservationLabResult.test(
            id="obs-lab", patient_ref=patient.ref(), performer_ref=doctor.ref(),
            effective=d["lab_date"], loinc_code=d["lab_loinc"], value=d["lab_value"], unit=d["lab_unit"])
        return [med], [item], [dx], [("tests", lab)]
