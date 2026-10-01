# Real NHI submission — adapter readiness (the "送" to 健保)

> Status of the one thing this repo does **not** do end-to-end: actually transmitting a prior-auth
> Bundle to the 健保署. The hard, valuable part — producing the **exactly-correct, validator-clean
> Bundle** — is done. Real transmission is gated on credentials + a login-walled interface spec, **not
> on code**. This doc says precisely what remains and how the adapter is wired to flip on.

## The real workflow (from official sources)

1. The institution's system **generates the PA Bundle (TWPAS)** — this repo's job. ✅ done, 0 errors.
2. It **uploads** the Bundle to the 健保署 via the **健保資訊網服務系統 (VPN)** — the NHI VPN. The VPN's
   醫事人員溝通平台 has a dedicated **事前審查** section.
3. Transport goes through the **醫療資料傳輸共通介面** (a common transmission interface the institution
   **downloads + installs before first upload**): institution identity verification, data encryption,
   automatic re-transmission.
4. Identity is a **醫事憑證 (HCA)** — issued by 衛福部醫事憑證管理中心 (`hca.nat.gov.tw`).
5. The receiver accepts (**收件**) and later returns adjudication — **收件 ≠ 核准**; 核刪 happens afterward
   (which is exactly why the CQL pre-submit self-check exists).

## Why this is a *credentials* problem, not a code problem

The three keys — the real endpoint, an **HCA cert**, and **NHI-VPN access** — are only issued to a
**醫事機構 (healthcare institution)**. The detailed **interface spec** itself lives in the login-walled
醫事機構專區. So:
- An individual **cannot** obtain them; a clinic you run, or an authorized partner institution, can.
- Until then, the exact transport **binding** (a REST endpoint reachable over the VPN, vs. calling the
  installed 共通介面 API/SDK locally) can't be confirmed — it's in the gated spec. **We do not guess it.**

## How the adapter is wired to flip on

The library already isolates transmission behind one seam — `IPasSubmitter` (the "送" socket). The lib
PRODUCES the Bundle; the submitter is a thin, swappable transport hop. Two production shapes, both just
an adapter behind the same interface:

**(a) If the real binding is REST-over-VPN** — `HttpPasSubmitter` is ready as-is; only configuration
changes (no code). HCA is a client certificate on the `HttpClient`, routed over the VPN:

```csharp
var handler = new HttpClientHandler();
handler.ClientCertificates.Add(new X509Certificate2("hca-cert.pfx", pin));   // 醫事憑證 (HCA)
var http = new HttpClient(handler);                                          // routed via the 健保 VPN
IPasSubmitter submit = new HttpPasSubmitter(http, "<real NHI PAS endpoint from the gated 介面規格>");
var result = await submit.SubmitAsync(bundle);   // 收件 ≠ 核准 — read result.Status / the ClaimResponse
```

**(b) If the real binding is the 共通介面 API/SDK** — write one small `IPasSubmitter` that calls that
local API and maps its response to `SubmitResult`. Same socket, ~1 class. The Bundle it sends is
unchanged (this repo already produces it).

## What remains (the whole of #13), and the gate on each

| Step | State | Gate |
|---|---|---|
| Produce correct Bundle | ✅ done, validated 0 errors | — |
| `IPasSubmitter` seam + `HttpPasSubmitter` shell | ✅ done, tested (TransportTests, stub handler) | — |
| Exact endpoint / transport binding | ⛔ unknown | login-walled 介面規格 (醫事機構專區) |
| HCA 醫事憑證 | ⛔ not held | 醫事機構 identity |
| 健保 VPN + 共通介面 install | ⛔ not available | 醫事機構 identity |
| Real end-to-end 真打 + its integration test | ⛔ blocked | all of the above |

**Bottom line:** the moment a 醫事機構 context provides the cert + VPN + spec, this is a configuration +
(at most) one adapter class + one live integration test — not a rebuild. The `post-to-public-server.sh`
tool already proves the Bundle is accepted by a real FHIR server today (just not the 健保 one).
