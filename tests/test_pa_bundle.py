"""Regression: the refactored PA bundle builds with the expected conformant shape.
(Full FHIR validation is `make validate`; this keeps the build/shape green in fast CI.)"""
import sys, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "src"))
from examples.build_pa_bundle import build


def test_bundle_shape():
    b = build()
    d = b.model_dump(exclude_none=True, by_alias=True)
    assert d["type"] == "collection"
    # every entry has an absolute fullUrl + a resource
    assert all(e["fullUrl"].startswith("https://nhicore.nhi.gov.tw/pas/") for e in d["entry"])
    assert all("resource" in e and e["resource"].get("resourceType") for e in d["entry"])
    # required entry resource types present
    rtypes = {e["resource"]["resourceType"] for e in d["entry"]}
    for req in ["Claim", "Encounter", "Patient", "Practitioner", "Organization",
                "MedicationRequest", "Coverage", "Observation"]:
        assert req in rtypes, f"missing required entry: {req}"


def test_no_empty_arrays_in_bundle():
    import json
    b = build()
    s = json.dumps(b.model_dump(exclude_none=True, by_alias=True))
    assert '"coding": []' not in s and '"[]"' not in s  # prune removed empty arrays
