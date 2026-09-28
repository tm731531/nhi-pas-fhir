"""Two-layer PA Bundle build (refactored) — TW Core clinical layer + pas assembler.

Assembles a cancer-drug 送核 Bundle and writes it for validation.
Run: .venv/bin/python -m examples.build_pa_bundle  (or make example)
All data fabricated; dosageInstruction is the authoritative official example fixture.
"""
from __future__ import annotations

import json
import pathlib

from nhi_pas import twcore as tc
from nhi_pas import pas

FIX = pathlib.Path(__file__).parent / "fixtures" / "official_dosage.json"


def build() -> pas.Bundle:
    patient = tc.Patient.with_id_card(id="pat-1", id_card="A123456789", name="王大明",
                                      gender="male", birth_date="1965-03-02")
    doctor = tc.Practitioner.of(id="pra-1", id_card="B234567890", name="李醫師")
    hospital = tc.Organization.hospital(id="org-hosp", org_code="0101090517", name="臺北市立聯合醫院")
    nhi = tc.Organization.govt_nhi()
    enc = tc.Encounter.minimal(id="enc-1", patient_ref=patient.ref())
    cov = pas.Coverage.of(id="cov-1", patient_ref=patient.ref(), nhi_org_ref=nhi.ref())

    dosage = json.loads(FIX.read_text())
    med = pas.MedicationRequestApply.of(id="medReq-apply", drug_code="BC27730100",
                                        patient_ref=patient.ref(), dosage=dosage,
                                        authored_on="2024-01-01")

    # Diagnosis C90 (multiple myeloma): the IG waives the supporting-report requirement for C90/C91/C92,
    # so no examination/imaging/gene report is needed for this case.
    dx = pas.diagnosis(icd10cm="C90.00", recorded_date="2024-01-01",
                       text="Multiple myeloma, not having achieved remission")
    item = pas.drug_item(sequence=1, med_ref=med.ref(), tbl_count=52,
                         program_text="多發性骨髓瘤第一線治療")
    # C90 requires a report (tests/imaging/gene) → a lab-result (tests) Observation, performer = doctor.
    lab = pas.ObservationLabResult.test(id="obs-lab", patient_ref=patient.ref(),
                                        performer_ref=doctor.ref(), effective="2024-01-01",
                                        loinc_code="777-3", value=5.1, unit="mmol/l")
    claim = pas.Claim.build(
        id="cla-1", subtype_code="1", subtype_display="送核",
        priority_code="1", priority_display="一般事前審查申請",
        patient=patient, enterer=doctor, provider=hospital, encounter=enc, coverage=cov,
        created="2026-09-28T09:00:00+08:00", weight_kg=68.0, height_cm=172.0,
        diagnosis=[dx], item=[item], supporting_reports=[("tests", lab.ref())],
    )
    return pas.Bundle.assemble(claim, enc, patient, doctor, hospital, med, cov, nhi, lab, id="bun-demo")


def main() -> None:
    b = build()
    out = pathlib.Path(".fhir/generated/pa-bundle.json")
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(b.model_dump(exclude_none=True, by_alias=True), ensure_ascii=False, indent=2))
    print(f"wrote {out} — {len(b.entry)} entries")


if __name__ == "__main__":
    main()
