"""Build a cancer-drug PA Bundle via the framework (PACase -> Factory -> Assembler).
Run: .venv/bin/python -m examples.build_pa_bundle  (or make example). Fabricated data only.
"""
from __future__ import annotations

import json
import pathlib

import nhi_pas  # noqa: F401  (registers plugins)
from nhi_pas.core.factory import AssemblerFactory
from nhi_pas.core.interfaces import PACase


def sample_case() -> PACase:
    return PACase(
        ig="tw.gov.mohw.nhi.pas#1.2.6", case_type="cancer-drug",
        patient={"id_card": "A123456789", "name": "王大明", "gender": "male", "birth_date": "1965-03-02"},
        provider={"doctor_id_card": "B234567890", "doctor_name": "李醫師",
                  "hospital_code": "0101090517", "hospital_name": "臺北市立聯合醫院"},
        vitals={"weight_kg": 68.0, "height_cm": 172.0},
        created="2026-09-28T09:00:00+08:00",
        data={"drug_code": "BC27730100", "authored_on": "2024-01-01",
              "diagnosis_icd": "C90.00", "diagnosis_date": "2024-01-01",
              "diagnosis_text": "Multiple myeloma, not having achieved remission",
              "drug_qty_tbl": 52, "program_text": "多發性骨髓瘤第一線治療",
              "lab_loinc": "777-3", "lab_value": 5.1, "lab_unit": "mmol/l", "lab_date": "2024-01-01"},
    )


def build():
    case = sample_case()
    return AssemblerFactory.for_case(case).assemble(case)


def main() -> None:
    b = build()
    out = pathlib.Path(__file__).resolve().parents[1] / "build" / "pa-bundle.json"
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(b.model_dump(exclude_none=True, by_alias=True), ensure_ascii=False, indent=2))
    print(f"wrote {out} — {len(b.entry)} entries")


if __name__ == "__main__":
    main()
