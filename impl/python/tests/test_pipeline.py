"""Pipeline (pre-check → assemble) tests — the 核刪 gate wired into the framework flow."""
import copy, pathlib, sys
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "src"))
from nhi_pas.core import pipeline
from examples.build_pa_bundle import sample_case


def _case_with(drug, indication):
    c = sample_case()
    d = dict(c.data); d["drug_code"] = drug; d["indication"] = indication
    return type(c)(ig=c.ig, case_type=c.case_type, patient=c.patient, provider=c.provider,
                   vitals=c.vitals, created=c.created, data=d)


def test_pipeline_clear_when_no_indication():
    r = pipeline.run(sample_case())
    assert not r.blocked and r.bundle is not None and len(r.bundle.entry) == 9


def test_pipeline_allows_good_pair():
    r = pipeline.run(_case_with("KC009612B5", "C50P1"))   # allowed
    assert not r.blocked and r.bundle is not None


def test_pipeline_blocks_bad_pair():
    r = pipeline.run(_case_with("KC009612B5", "C99X9"))    # not allowed → 核刪
    assert r.blocked and r.bundle is None
    assert "核刪" in r.findings[0].message


def test_advisory_present():
    r = pipeline.run(sample_case())
    assert "does not guarantee" in r.advisory   # Constitution III: advisory, not authority
