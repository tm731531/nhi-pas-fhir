"""Interface contract for Taiwan NHI Prior Authorization (事前審查 / TWPAS).

Mirrors the two CapabilityStatements declared by the IG:
- TWPAS **Server** (伺服端) — the NHI side that receives bundles and returns adjudications.
- TWPAS **Client** (用戶端) — the provider side that submits and polls.

SoT: https://nhicore.nhi.gov.tw/pas/  ·  IG v1.2.6  ·  FHIR R4 (4.0.1)

This module defines the *logical* contract as typing.Protocols. It is transport-agnostic:
production uses NHI's 共通傳輸平台 (shared transport / batch); the REST shape here is the
logical model (see docs/02-interface.md). No live endpoint, no real data.
"""
from __future__ import annotations

from dataclasses import dataclass
from enum import Enum
from typing import Protocol, runtime_checkable

# --- Case categories (from IG ValueSets) -----------------------------------


class ApplyCategory(str, Enum):
    """Claim.priority — NHI-健保事前審查-申請案件類別值集."""

    NORMAL = "一般事前審查申請"      # normal prior-auth application
    SELF_ASSESSMENT = "自主審查"      # provider self-assessment (e.g. 心/肝移植)
    EMERGENCY = "緊急報備"            # administer first, report/审 after


class FilingCategory(str, Enum):
    """Claim.subType — NHI-健保事前審查-申報類別值集 (the submit/appeal loop)."""

    SUBMIT = "送核"
    SUBMIT_SUPPLEMENT = "送核補件"
    APPEAL = "申復"
    APPEAL_SUPPLEMENT = "申復補件"
    DISPUTE = "爭議審議"


class Disposition(str, Enum):
    """ClaimResponse outcome (bound to NHI 核定註記 ValueSets in the IG)."""

    APPROVED = "核准"
    REJECTED = "駁回"
    NEED_INFO = "補件"


# --- Lightweight envelopes (opaque FHIR payloads for now) -------------------
# Real FHIR resources live in resources.py; here we keep the contract shape.


@dataclass
class SubmitResult:
    """Format-validation outcome of an upload (before clinical review)."""

    accepted: bool
    bundle_id: str | None = None
    # On failure the IG returns an OperationOutcome TWPAS.
    operation_outcome: dict | None = None


@dataclass
class Adjudication:
    """Clinical-review outcome, from a Bundle Response TWPAS / ClaimResponse TWPAS."""

    claim_id: str
    disposition: Disposition
    reason_codes: list[str]        # NHI 核定註記 codes
    raw_claim_response: dict | None = None


# --- Client side (provider) -------------------------------------------------


@runtime_checkable
class TWPASClient(Protocol):
    """Provider-side capabilities (臺灣事前審查-用戶端)."""

    def submit(self, bundle: dict) -> SubmitResult:
        """POST a request Bundle TWPAS (a FHIR transaction). Returns format-validation result."""
        ...

    def query_applications(
        self,
        *,
        patient: str | None = None,
        func_type: FilingCategory | None = None,  # SearchParameter: Claim-func-type
        claim_id: str | None = None,
        identifier: str | None = None,
    ) -> list[dict]:
        """GET /Claim?... — find my submitted applications."""
        ...

    def query_result(self, *, claim_id: str) -> Adjudication | None:
        """GET /ClaimResponse?request={claim_id} — poll the adjudication."""
        ...


# --- Server side (NHI) ------------------------------------------------------


@runtime_checkable
class TWPASServer(Protocol):
    """NHI-side capabilities (臺灣事前審查-伺服端)."""

    def receive(self, bundle: dict) -> SubmitResult:
        """Validate format of an incoming Bundle; ack or return OperationOutcome TWPAS."""
        ...

    def adjudicate(self, claim_id: str) -> Adjudication:
        """Reviewer decision → Bundle Response TWPAS (ClaimResponse TWPAS inside)."""
        ...


# Declared FHIR SearchParameters (docs/02-interface.md), for reference:
SEARCH_PARAMETERS: dict[str, list[str]] = {
    "Bundle": ["id"],
    "Claim": ["id", "identifier", "patient", "func-type", "lastUpdated"],
    "ClaimResponse": [
        "request", "identifier", "created", "disposition",
        "adjudication-reason", "requestor", "include",
    ],
    "Encounter": ["service-type"],
    "Organization": ["identifier"],
    "Patient": ["identifier", "name"],
}
