# demo — a real app that consumes the NhiPasFhir lib

Not part of the reference implementation (`impl/`). This is a showcase: a separate ASP.NET Core
**Razor Pages** app (`NhiPasDemo/`) that references `NhiPasFhir` as a library and drives the whole
flow through a server-rendered UI.

## What it shows

Pick a case → the lib assembles a FHIR Bundle (**產**) → the pipeline runs the seed drug↔indication
check (**驗**) → optionally the official CQL self-check against the CQF-Ruler server (**查**) → the
page renders the three-state verdict + the Bundle. (產 → 驗 → 查 → 送 → 回; this page covers 產/驗/查.)

## Run

```bash
# (optional, for the CQL 查 step) start the faithful engine + load rules:
cd ../cql-engine/server && docker compose up -d && node load-libraries.mjs && cd -

cd NhiPasDemo
dotnet run --urls http://localhost:5099
# open http://localhost:5099 → pick 癌症藥物送核, tick 「送 CQL 自查」, 跑一次
```

Without the CQL server, leave the checkbox off (or the page falls back to assemble-only and says so).
The CQL server base URL is configurable via `Cql:BaseUrl` (default `http://localhost:8095/fhir`).

## How it uses the lib (the only integration points)

- `Samples.CancerDrugCase()` / `ImmunologicCase()` / `AppealCase()` / `SelfAssessmentCase()` → a `PACase`
- `Pipeline.Run(case, cql: ...)` → `PipelineResult` (Findings / Blocked / Bundle / Cql three-state)
- `new CqfRulerCqlEngine(http, baseUrl)` + `CqlPreCheck(engine, drug→rules)` for the 查 step
- `NhiPas.ToJson(bundle)` to render the assembled Bundle

The demo holds no FHIR/CQL logic of its own — it only calls the lib. Swapping the engine or updating
the rules never touches this project (Tom's replaceability invariant).

## Pages

- `/` (Demo) — 這是什麼 + 怎麼用 + the 產→驗→查 flow, the three-state verdict, the FHIR resource
  breakdown (各個體制), and the assembled Bundle.
- `/Learn` (CQL & FHIR 問答) — renders `spec/docs/cql-explained.md` (15 Q&A: what it is).
- `/Wiring` (CQL 怎麼串) — renders `spec/docs/cql-wiring.md` (how CQL is wired: the line from
  「跑一次」to a verdict, the toggle + the socket, where the four pieces live).

## Host it under your own domain

To keep it up and expose it at `https://<your-subdomain>` through a Cloudflare tunnel, see
`host/EXPOSE-via-cloudflare-tunnel.md` (the ingress rule to add on your tunnel host) and
`host/nhi-pas-demo.service` (a systemd user unit so :5099 stays up).
