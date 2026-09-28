"""Worked example (B): assemble a 癌藥送核 Bundle and run the 核刪 pre-check.

Run:  make example      (or: .venv/bin/python -m examples.build_sample_bundle)

No real patient data — all identifiers are fabricated.
"""
from __future__ import annotations

import json

from nhi_pas.resources import (
    BundleTWPAS, ClaimTWPAS, PatientTWPAS, PractitionerTWPAS, OrganizationTWPAS,
    MedicationRequestApplyTWPAS, CodeableConcept, Reference,
)
from nhi_pas.precheck import precheck_pairs, has_blocking_errors

VS_DRUG = "https://nhicore.nhi.gov.tw/pas/ValueSet/nhi-medication"  # 用藥品項值集


def build_bundle(*, drug_code: str) -> BundleTWPAS:
    patient = PatientTWPAS.with_id_card(
        id_card="A123456789", name="王小明", gender="male", birth_date="1965-03-02"
    )
    doctor = PractitionerTWPAS.of(id_card="B234567890", name="李醫師")
    hospital = OrganizationTWPAS.of(org_code="1131080020", name="範例醫學中心")

    med = MedicationRequestApplyTWPAS(
        medicationCodeableConcept=CodeableConcept.of(VS_DRUG, drug_code),
        subject=Reference(reference="Patient/pat-1"),
        authoredOn="2026-09-28",
    )

    claim = ClaimTWPAS.build(
        subtype_code="1",     # 送核
        priority_code="1",    # 一般事前審查
        patient_ref="Patient/pat-1",
        provider_ref="Organization/org-1",
        enterer_ref="Practitioner/doc-1",
        encounter_ref="Encounter/enc-1",
        created="2026-09-28T09:00:00+08:00",
        weight_kg=68.0, height_cm=172.0,
    )

    # NOTE: required Bundle entries also include Encounter / Coverage / TWCoreOrganizationGovt.
    # Those are minimal stubs for now (see resources.py TODO). This example focuses on the
    # spine (Claim + Patient + Practitioner + Organization + MedicationRequest) + the pre-check.
    return BundleTWPAS.assemble(patient, doctor, hospital, claim, med, identifier="CASE-DEMO-0001")


def main() -> None:
    drug = "KC009612B5"

    print("=" * 70)
    print("CASE 1 — correct indication (should PASS pre-check)")
    print("=" * 70)
    bundle = build_bundle(drug_code=drug)
    print(json.dumps(bundle.model_dump(exclude_none=True), ensure_ascii=False, indent=2)[:700] + "\n  ...")
    good = precheck_pairs([(drug, "C50P1")])   # C50P1 is in the allowed set
    print(f"\npre-check findings: {len(good)}  → {'BLOCKED' if has_blocking_errors(good) else 'CLEAR ✅'}")

    print("\n" + "=" * 70)
    print("CASE 2 — wrong indication (should BLOCK → would be 核刪)")
    print("=" * 70)
    bad = precheck_pairs([(drug, "C99X9")])    # not in allowed set
    for f in bad:
        print(f"  [{f.severity.upper()}] {f.message}")
    print(f"\npre-check result: {'BLOCKED ❌ (do not submit)' if has_blocking_errors(bad) else 'clear'}")

    print("\n" + "=" * 70)
    print("This is the Layer-A value: catch 核刪 BEFORE submitting, not after clawback.")
    print("=" * 70)


if __name__ == "__main__":
    main()
