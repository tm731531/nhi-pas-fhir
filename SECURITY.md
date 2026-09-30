# Security Policy

## Reporting a vulnerability

Please report suspected vulnerabilities privately via GitHub's
**"Report a vulnerability"** (Security → Advisories) on this repository, rather than a
public issue.

## Scope note

This project is a **reference implementation** — a payload factory + validator gate + an
optional CQL pre-check. It is **not** a FHIR server and stores no data. It uses **no real
patient data**; all identifiers in samples/tests are fabricated. The CQL pre-check is
decision-support only and does not guarantee reimbursement.
