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
