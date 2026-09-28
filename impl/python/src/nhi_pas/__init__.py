"""nhi_pas — study/reference package for Taiwan NHI Prior Authorization (事前審查) FHIR IG.

SoT: https://nhicore.nhi.gov.tw/pas/ · IG v1.2.6 · FHIR R4. No real patient data.
"""
from . import twcore, pas  # noqa: F401  (two-layer architecture, spec 001)
from . import core, plugins  # noqa: F401  (framework: interfaces/factory + registered case types)
from .interface import (
    Adjudication, ApplyCategory, Disposition, FilingCategory,
    SubmitResult, TWPASClient, TWPASServer, SEARCH_PARAMETERS,
)
from .core.precheck import Finding, RuleSet, precheck_pairs, has_blocking_errors
from .resources import (
    BundleTWPAS, ClaimTWPAS, PatientTWPAS, PractitionerTWPAS,
    OrganizationTWPAS, MedicationRequestApplyTWPAS, EncounterTWPAS, CoverageTWPAS,
    CodeableConcept, Reference,
)

__all__ = [
    "Adjudication", "ApplyCategory", "Disposition", "FilingCategory",
    "SubmitResult", "TWPASClient", "TWPASServer", "SEARCH_PARAMETERS",
    "BundleTWPAS", "ClaimTWPAS", "PatientTWPAS", "PractitionerTWPAS",
    "OrganizationTWPAS", "MedicationRequestApplyTWPAS", "EncounterTWPAS",
    "CoverageTWPAS", "CodeableConcept", "Reference",
    "Finding", "RuleSet", "precheck_pairs", "has_blocking_errors",
]
__version__ = "0.0.1"
