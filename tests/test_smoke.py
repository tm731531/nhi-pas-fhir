"""Smoke test: build each core resource and assemble a Bundle. Proves the models construct
and enforce the id-card constraint. No real data."""
import sys, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "src"))
import pytest
from nhi_pas.resources import (
    PatientTWPAS, PractitionerTWPAS, OrganizationTWPAS, ClaimTWPAS, BundleTWPAS,
)


def test_build_core_and_bundle():
    pat = PatientTWPAS.with_id_card(id_card="A123456789", name="測試病人", gender="male", birth_date="1970-01-01")
    doc = PractitionerTWPAS.of(id_card="B234567890", name="測試醫師")
    org = OrganizationTWPAS.of(org_code="0123456789", name="測試醫院")
    claim = ClaimTWPAS.build(
        subtype_code="1", priority_code="1",
        patient_ref="Patient/pat1", provider_ref="Organization/org1",
        enterer_ref="Practitioner/doc1", encounter_ref="Encounter/enc1",
        created="2026-09-28T09:00:00+08:00", weight_kg=60.0, height_cm=170.0,
    )
    bundle = BundleTWPAS.assemble(pat, doc, org, claim, identifier="CASE-0001")

    assert bundle.type == "collection"
    assert bundle.meta.profile == ["https://nhicore.nhi.gov.tw/pas/StructureDefinition/Bundle-twpas"]
    assert claim.status == "active" and claim.use == "preauthorization"
    assert claim.type.coding[0].code == "institutional"
    assert len(claim.supportingInfo) == 2  # weight + height
    assert len(bundle.entry) == 4


def test_id_card_constraint_rejects_bad_value():
    with pytest.raises(Exception):
        PatientTWPAS.with_id_card(id_card="BAD", name="X", gender="male", birth_date="1970-01-01")


if __name__ == "__main__":
    test_build_core_and_bundle()
    test_id_card_constraint_rejects_bad_value()
    print("smoke OK")
