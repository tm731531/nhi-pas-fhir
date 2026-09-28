"""Core contracts — the interface every case-type implementation depends on.

PACase is a neutral, non-FHIR description of one application (so callers never touch FHIR).
ICaseAssembler is the contract; concrete assemblers live in plugins/ and register with the factory.
"""
from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Protocol, runtime_checkable

from ..pas import Bundle


@dataclass
class PACase:
    """Neutral input for one prior-authorization case. `data` carries case-type-specific facts."""
    ig: str                      # e.g. "tw.gov.mohw.nhi.pas#1.2.6"
    case_type: str               # e.g. "cancer-drug"
    patient: dict[str, str]      # {id_card, name, gender, birth_date}
    provider: dict[str, str]     # {doctor_id_card, doctor_name, hospital_code, hospital_name}
    vitals: dict[str, float]     # {weight_kg, height_cm}
    created: str                 # dateTime
    data: dict[str, Any] = field(default_factory=dict)  # case-specific (diagnosis, drug, labs…)


@runtime_checkable
class ICaseAssembler(Protocol):
    ig: str
    case_type: str
    def assemble(self, case: PACase) -> Bundle: ...
