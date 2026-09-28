"""Pre-submit rule check — the highest-value feature (block 核刪 / clawback before submitting).

In TWPAS, an applied 事前審查品項 (drug code, `cancerDrugType` / MedicationRequest.medicationCodeableConcept)
is only reimbursable for a constrained set of 給付適應症 codes (`applyReason`). Submitting a
drug↔indication pair outside the allowed set → rejection / 核刪 (payment clawback).

The authoritative rule set is the NHI **預檢規則 (FHIR CQL) IG** (build.fhir.org/ig/TWNHIFHIR/cql/)
plus the 給付適應症 ValueSets. This module implements the *mechanism* and is seeded with the two
real drug↔indication constraints quoted in the pas IG. The full rule set is a data-load task:

    # TODO: load the complete drug↔indication rule set from the 預檢規則 CQL IG / ValueSets
    #       (do NOT hand-transcribe thousands of rules; ingest the published artifacts).

This keeps the engine honest: the logic is real; the seeded data is explicitly a small verified subset.
"""
from __future__ import annotations

from dataclasses import dataclass, field


@dataclass(frozen=True)
class DrugIndicationRule:
    """A drug code may only be claimed against these 給付適應症 indication codes."""

    drug_code: str
    allowed_indications: frozenset[str]


# --- Seed rules: transcribed verbatim from the pas IG (verified) -------------
# Source: https://nhicore.nhi.gov.tw/pas/  (StructureDefinition-Bundle-twpas / overview)
_SEED_RULES: dict[str, DrugIndicationRule] = {
    "KC009612B5": DrugIndicationRule(
        "KC009612B5", frozenset({"C50P1", "C50P2", "C50R1", "C16R1"})
    ),
    "KC010892B5": DrugIndicationRule(
        "KC010892B5",
        frozenset({"C50P1", "C50P2", "C50P3", "C50P4", "C50P5", "C50R1", "C16R1"}),
    ),
}


@dataclass
class Finding:
    """A pre-check result item."""

    severity: str  # "error" (would be 核刪) | "warning" | "info"
    drug_code: str
    indication_code: str | None
    message: str


@dataclass
class RuleSet:
    rules: dict[str, DrugIndicationRule] = field(default_factory=lambda: dict(_SEED_RULES))

    def known(self, drug_code: str) -> bool:
        return drug_code in self.rules

    def check_pair(self, drug_code: str, indication_code: str) -> Finding | None:
        """Return a Finding if this (drug, indication) pair is invalid; None if OK."""
        rule = self.rules.get(drug_code)
        if rule is None:
            return Finding(
                "warning", drug_code, indication_code,
                f"drug {drug_code}: no rule loaded (seed subset only) — cannot verify indication "
                f"{indication_code}. # TODO: load full rule set from 預檢規則 CQL IG.",
            )
        if indication_code not in rule.allowed_indications:
            allowed = ", ".join(sorted(rule.allowed_indications))
            return Finding(
                "error", drug_code, indication_code,
                f"drug {drug_code} with indication {indication_code} would be 核刪: "
                f"allowed indications are {{{allowed}}}.",
            )
        return None


def precheck_pairs(
    pairs: list[tuple[str, str]], ruleset: RuleSet | None = None
) -> list[Finding]:
    """Check applied (drug_code, indication_code) pairs. Empty list = all clear.

    `pairs` are extracted from an application (drug = MedicationRequest.medicationCodeableConcept;
    indication = applyReason / 給付適應症). Extraction of `indication` from the exact FHIR element is
    still TODO-verified against the IG, so callers pass pairs explicitly for now.
    """
    rs = ruleset or RuleSet()
    findings: list[Finding] = []
    for drug_code, indication_code in pairs:
        f = rs.check_pair(drug_code, indication_code)
        if f is not None:
            findings.append(f)
    return findings


def has_blocking_errors(findings: list[Finding]) -> bool:
    return any(f.severity == "error" for f in findings)
