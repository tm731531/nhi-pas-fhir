"""Pipeline — the framework flow: PRE-CHECK (block 核刪) → ASSEMBLE → (then Validate, externally).

The pre-check runs BEFORE assembly/submission so a drug↔indication mismatch is caught before it can
cause 核刪 (clawback). Advisory only (Constitution III): it assists; it does not guarantee給付.
"""
from __future__ import annotations

from dataclasses import dataclass

from ..pas import Bundle
from .factory import AssemblerFactory
from .interfaces import PACase
from .precheck import Finding, precheck_pairs, has_blocking_errors


@dataclass
class PipelineResult:
    findings: list[Finding]        # pre-check findings (empty = clear)
    blocked: bool                  # True if a blocking (核刪) finding was raised
    bundle: Bundle | None          # assembled bundle (None if blocked and stop_on_block)
    advisory: str = ("Pre-check is decision-support only; it does not guarantee reimbursement. "
                     "Final responsibility rests with the clinician and NHI adjudication.")


def _pairs(case: PACase) -> list[tuple[str, str]]:
    """Extract (drug_code, indication_code) pairs to pre-check. Indication is optional in the case."""
    d = case.data
    drug = d.get("drug_code")
    ind = d.get("indication")            # 給付適應症 code, if the caller supplied one
    return [(drug, ind)] if drug and ind else []


def run(case: PACase, *, stop_on_block: bool = True) -> PipelineResult:
    """Pre-check → assemble. If blocked and stop_on_block, do not assemble (fail-loud)."""
    findings = precheck_pairs(_pairs(case))
    blocked = has_blocking_errors(findings)
    if blocked and stop_on_block:
        return PipelineResult(findings=findings, blocked=True, bundle=None)
    bundle = AssemblerFactory.for_case(case).assemble(case)
    return PipelineResult(findings=findings, blocked=blocked, bundle=bundle)
