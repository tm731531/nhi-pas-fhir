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
    hospital = tc.Organization.hospital(id="org-hosp", org_code="1131080020", name="範例醫學中心")
    nhi = tc.Organization.govt_nhi(org_code="0000000000")
    enc = tc.Encounter.minimal(id="enc-1", patient_ref=patient.ref())
    cov = pas.Coverage.of(id="cov-1", patient_ref=patient.ref(), nhi_org_ref=nhi.ref())

    dosage = json.loads(FIX.read_text())
    med = pas.MedicationRequestApply.of(id="medReq-apply", drug_code="BC27730100",
                                        patient_ref=patient.ref(), dosage=dosage,
                                        authored_on="2024-01-01")

    dx = pas.diagnosis(icd10cm="I50.812", recorded_date="2024-01-01",
                       text="Adenocarcinoma, descending colon, cStage IVA, with liver metastases")
    item = pas.drug_item(sequence=1, med_ref=med.ref(), tbl_count=52,
                         program_text="ALK陽性晚期非小細胞肺癌第一線治療")
    claim = pas.Claim.build(
        id="cla-1", subtype_code="1", subtype_display="送核",
        priority_code="1", priority_display="一般事前審查申請",
        patient=patient, enterer=doctor, provider=hospital, encounter=enc, coverage=cov,
        created="2026-09-28T09:00:00+08:00", weight_kg=68.0, height_cm=172.0,
        diagnosis=[dx], item=[item],
    )
    return pas.Bundle.assemble(claim, enc, patient, doctor, hospital, med, cov, nhi, id="bun-demo")


def main() -> None:
    b = build()
    out = pathlib.Path(".fhir/generated/pa-bundle.json")
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(b.model_dump(exclude_none=True, by_alias=True), ensure_ascii=False, indent=2))
    print(f"wrote {out} — {len(b.entry)} entries")


if __name__ == "__main__":
    main()
