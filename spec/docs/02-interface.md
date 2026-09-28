# 02 — Interface (介面契約)

> SoT: <https://nhicore.nhi.gov.tw/pas/> · IG v1.2.6 · declared by two CapabilityStatements

The IG declares the API from two sides:
- **TWPAS Server** (臺灣事前審查-伺服端) — the NHI side that receives & adjudicates.
- **TWPAS Client** (臺灣事前審查-用戶端) — the provider side that submits & queries.

`src/nhi_pas/interface.py` mirrors these as Python `Protocol`s.

## 1. Submit

```
POST  [base]/Bundle           body: Bundle TWPAS (transaction)
                              → 202/200 success  |  OperationOutcome TWPAS on failure
```
The request Bundle is a FHIR **transaction** carrying Claim + all supporting resources.

## 2. Query (declared SearchParameters — 14)

| Resource | SearchParameter | Use |
|---|---|---|
| Bundle | `Bundle-id` | fetch a submitted bundle |
| Claim | `Claim-id`, `Claim-identifier`, `Claim-patient`, `Claim-func-type`, `Claim-lastUpdated` | find applications |
| ClaimResponse | `ClaimResponse-request`, `ClaimResponse-identifier`, `ClaimResponse-created`, `ClaimResponse-disposition`, `ClaimResponse-adjudication-reason`, `ClaimResponse-requestor`, `ClaimResponse-include` | find adjudication results |
| Encounter | `Encounter-service-type` | by 就醫科別 |
| Organization | `Organization-identifier` | |
| Patient | `Patient-identifier`, `Patient-name` | |

Typical polling:
```
GET  [base]/Claim?patient={id}&func-type={申報類別}         # my applications
GET  [base]/ClaimResponse?request={claim-id}               # its result
```

## 3. Extensions (3)

| Extension | Attaches to | Meaning |
|---|---|---|
| `ClaimEncounter` | Claim | 就醫科別 |
| `ClaimResponseRequestor` | ClaimResponse | 審查委員身分證號 (reviewer id) |
| `RequestedService` | (request) | 事前審查品項 (requested item) |

## 4. Response semantics

- Success envelope → **Bundle Response TWPAS** → contains **ClaimResponse TWPAS**
  - `ClaimResponse.outcome` / `.disposition` → approve / reject / need-info
  - adjudication reasons bound to `NHI-...核定註記` ValueSets
- Any system-level error → **OperationOutcome TWPAS**
- 自主審查 result → **ClaimResponse Self Assessment TWPAS**

## 5. Not in scope of the IG's REST layer

- No custom FHIR `$operation` is defined (a **"預檢規則 (FHIR CQL)"** artifact exists for
  pre-submit rule checking, but is not exposed as a FHIR Operation here).
- Actual transport in production is the NHI **共通傳輸平台 (shared transport platform)** (batch
  upload); the REST shape above is the logical contract this repo models.

> `# TODO`: confirm exact base URL, auth, and batch-vs-REST framing from the IG's ImplementationGuide
> / production docs before any real integration. We model the logical contract, not a live endpoint.
