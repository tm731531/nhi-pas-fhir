import sys, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "src"))
from nhi_pas.precheck import precheck_pairs, has_blocking_errors, RuleSet


def test_valid_pair_passes():
    findings = precheck_pairs([("KC009612B5", "C50P1")])
    assert findings == []
    assert not has_blocking_errors(findings)


def test_invalid_pair_blocks():
    findings = precheck_pairs([("KC009612B5", "C99X9")])
    assert has_blocking_errors(findings)
    assert "核刪" in findings[0].message


def test_unknown_drug_warns_not_blocks():
    findings = precheck_pairs([("ZZUNKNOWN", "C50P1")])
    assert len(findings) == 1
    assert findings[0].severity == "warning"        # seed subset only → warn, don't false-block
    assert not has_blocking_errors(findings)


def test_second_seed_rule():
    # KC010892B5 allows a wider set incl C50P3
    assert precheck_pairs([("KC010892B5", "C50P3")]) == []
    assert has_blocking_errors(precheck_pairs([("KC010892B5", "C50R9")]))
