"""ImmunologicAssembler — 免疫製劑事前審查 case type (registered; body is a scoped follow-up).

Proves framework extensibility: adding a case = one subclass + register, reusing the shared clinical
layer. BUT Bundle-immunologic-agent-twpas mandates ~35 resource slices (min=1) — the full SOAP note
(Composition-opd, Observation subjective/objective, ClinicalImpression, CarePlan) + blood group +
allergy + imaging/gene/lab/procedure/substance evidence + a self-assessment ClaimResponse. That body
is modelled from the IG one resource at a time (like 癌藥 45→0), each validated. Until then this fails
loud (Constitution IV) instead of emitting a knowingly-invalid bundle.
"""
from __future__ import annotations

from ..twcore import Resource
from ..pas import Claim, Item, Diagnosis
from ..core.base import AbstractCaseAssembler
from ..core.factory import AssemblerFactory
from ..core.interfaces import PACase


@AssemblerFactory.register("tw.gov.mohw.nhi.pas#1.2.6", "immunologic-agent")
class ImmunologicAssembler(AbstractCaseAssembler):
    def build_case(self, case: PACase, patient: Resource, doctor: Resource, hospital: Resource
                   ) -> tuple[list[Resource], list[Item], list[Diagnosis], list[tuple[str, Resource]]]:
        raise NotImplementedError(
            "immunologic-agent requires ~35 resources per Bundle-immunologic-agent-twpas "
            "(SOAP note + blood group + allergy + evidence + self-assessment ClaimResponse). "
            "Scoped follow-up — model from the IG package one resource at a time, validate each. "
            "See spec/docs/CASE-CATALOG.md."
        )
