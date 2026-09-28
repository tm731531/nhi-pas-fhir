"""Contract tests — one per contract in spec/docs/CONTRACTS-and-TESTS.md."""
import json, pathlib, sys
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "src"))
import pytest
import nhi_pas  # registers plugins
from nhi_pas.core.factory import AssemblerFactory
from nhi_pas.core.interfaces import PACase
from nhi_pas.core.base import AbstractCaseAssembler
from examples.build_pa_bundle import sample_case, build

GOLDEN = pathlib.Path(__file__).resolve().parents[3] / "spec" / "reference-bundles" / "cancer-drug-pa-bundle.json"


def test_factory_dispatch():                       # C6
    a = AssemblerFactory.for_case(sample_case())
    assert a.case_type == "cancer-drug"


def test_factory_unregistered_raises():            # C7 (fail-loud)
    bad = PACase(ig="x", case_type="nope", patient={}, provider={}, vitals={}, created="")
    with pytest.raises(KeyError):
        AssemblerFactory.for_case(bad)


def test_abstract_cannot_instantiate():            # C5
    with pytest.raises(TypeError):
        AbstractCaseAssembler()  # abstract build_case not implemented


def test_bundle_shape():                           # C2/C4
    d = build().model_dump(exclude_none=True, by_alias=True)
    assert d["type"] == "collection"
    assert all(e["fullUrl"].startswith("https://nhicore.nhi.gov.tw/pas/") for e in d["entry"])
    rtypes = {e["resource"]["resourceType"] for e in d["entry"]}
    for req in ["Claim", "Encounter", "Patient", "Practitioner", "Organization", "MedicationRequest", "Coverage", "Observation"]:
        assert req in rtypes


def test_no_empty_arrays():                        # C3
    s = json.dumps(build().model_dump(exclude_none=True, by_alias=True))
    assert '"coding": []' not in s


def test_reproduces_golden():                      # C9
    got = build().model_dump(exclude_none=True, by_alias=True)
    want = json.loads(GOLDEN.read_text())
    assert got == want, "assembler output drifted from the golden reference bundle"


def test_factory_has_two_case_types():             # framework extensibility proof
    from nhi_pas.core.factory import AssemblerFactory
    reg = dict.fromkeys(AssemblerFactory.registered())
    assert ("tw.gov.mohw.nhi.pas#1.2.6", "cancer-drug") in reg
    assert ("tw.gov.mohw.nhi.pas#1.2.6", "immunologic-agent") in reg


def test_immunologic_dispatches_and_fails_loud():  # C7-style honesty (Constitution IV)
    from nhi_pas.core.factory import AssemblerFactory
    from nhi_pas.plugins.immunologic import ImmunologicAssembler
    case = PACase(ig="tw.gov.mohw.nhi.pas#1.2.6", case_type="immunologic-agent",
                  patient={"id_card": "A123456789", "name": "x", "gender": "male", "birth_date": "1970-01-01"},
                  provider={"doctor_id_card": "B234567890", "doctor_name": "y", "hospital_code": "0101090517"},
                  vitals={"weight_kg": 60.0, "height_cm": 170.0}, created="2026-09-28T09:00:00+08:00", data={})
    a = AssemblerFactory.for_case(case)
    assert isinstance(a, ImmunologicAssembler)     # dispatch works (mechanism proven)
    with pytest.raises(NotImplementedError):        # body is a scoped follow-up, fails loud (not fake-valid)
        a.assemble(case)
