# BUSINESS.md — the money side (錢)

> Living doc. The technical repo exists to serve this. Owner: Tom.
> Companion strategy memory: `~/.claude/projects/-home-tom/memory/project-nhi-pas-fhir.md`.

## 0. The real goal (anchor — do not lose)

Not "build a big LTC company." The goal, in priority:

1. **[goal]** — a floor of reliable income. (Already largely secured: ~[redacted] [redacted] + dividends.)
2. **[goal]** — control over time, low-treadmill work.
3. **[goal]** — the deepest driver; 1 & 2 exist to enable this.
4. **長照** — the mission. A *bonus* if reached, explicitly acceptable if not.

Design every decision to optimise **cash sufficiency + time control**, not VC-scale growth.

## 1. Thesis

- Regulated healthcare IT is an AI-resistant niche **because the rules keep changing** → continuous
  human judgment is required → AI can't keep up, but a domain-fluent small team can.
- **2026 is the inflection**: Taiwan mandates FHIR (electronic medical records, NHI 給付 on FHIR:
  Claim/EOB/CQL). Standardisation turns integration from *per-site custom work* into *build-once
  against TW Core* → a small AI-native team can now reach every provider.
- The incumbent ([competitor] / [competitor], 興櫃 [redacted], ~40% share) is **large, slow, and losing money**
  (2026 H1: −24.35M NT$, revenue shrinking). Its size is a burden, not a moat.

## 2. Market & sequencing (beachhead)

**診所 (clinics) first → 長照 (LTC) later.** Reasons:
- Clinics are the most profitable and most in need; LTC alone can't fund itself (even the leader loses money).
- **Same TW Core base** → clinic work extends to LTC by swapping the IG profile (not a rewrite).
  This structurally mitigates the beachhead trap: cash market and mission market share one foundation.

**Architecture rule (anti-trap):** build clinic features on the shared TW Core layer from day one so
every dollar earned in clinics accrues toward LTC. (See `docs/04-ig-landscape.md`.)

## 3. ICP (ideal customer)

**A doctor who just left a hospital to open their own clinic.**
- At a hospital they had a team handling 事前審查 / billing; solo, they drown in it.
- They just hit the "problem patient / 事前審查 核刪" walls hospitals shielded them from.
- They carry the **醫界學長學弟 network** = the low-cost word-of-mouth channel.

## 4. Product (two layers)

### Layer A — Cash engine: 事前審查 / 金流 pre-check tool
An AI/FHIR layer **on top of** existing clinic systems (not a rival HIS). Value:
- auto-assemble the request `Bundle` (less typing),
- **pre-submit rule check**: block drug↔indication mismatches before they cause 核刪 (clawback),
- rejection prediction + fix suggestions; manage the 補件/申復 loop.
- Rides the 2026 FHIR mandate; requires FHIR + 健保 rule 眉角 + AI — rare combination.
- This repo (`nhi-pas-fhir`) is the seed of Layer A.

### Layer B — Trust/GTM engine: "1+1" doctor network
A doctor-to-doctor shared-intelligence platform for the "unspeakable" problems (fraud-pattern
patients, dispute patterns) hospitals never face but clinic owners do.
- **Legal landmine (must solve FIRST):** a named "problem patient blacklist" = 個資法 / 誹謗 / 拒診
  discrimination risk. **Must be de-identified *pattern* intelligence, not a named list.** Converting
  "unspeakable blacklist" → "lawful pattern knowledge" needs security + de-identification expertise
  (Tom's edge) — that conversion *is* the moat.
- Purpose: give doctors something money can't buy (protective intel) → they trust the platform →
  they adopt Layer A (the paid tool). Network builds channel; tool monetises.

## 5. Monetisation (TODO — draft)

- Layer A: SaaS subscription per clinic (seat / volume). `# TODO: price discovery with real clinics.`
- Layer B: likely free/community (the hook), or membership. `# TODO: model.`
- Anchor to the goal: target = enough recurring cash for [goal] + [goal], not max ARR.
- `# TODO: unit economics — CAC via 學長學弟 (low), churn, break-even clinic count.`

## 6. Competitive read

| | [competitor] ([competitor]) | Us |
|---|---|---|
| Size | ~1000 ppl, legacy | small, AI-native |
| Economics | losing money on LTC SaaS | lifestyle-scale target |
| Speed | slow (policy re-work burden) | fast |
| Domain | strong (their moat) | building via [employer] immersion + 照服員 background |
| Integration | many custom legacy | build-once on TW Core |

Do **not** fight head-on as "a better [competitor]". Win the gaps: clinic-owner UX, de-identified
compliance intel, FHIR-native pre-check, LTC bridge.

## 7. Resources needed (錢 + 系統 — to prepare)

- **People:** at least one team member with the **醫界 network / doctor identity** (channel + legal
  insider for Layer B). `# TODO: confirm who on "我們" has this.`
- **Domain access:** the tacit 健保/HIS 眉角 → via [employer] (a means, not an end). `# TODO: verify [employer] lets
  Tom touch 金流/FHIR before committing.`
- **Capital:** low. 194M-net-worth buffer + dividend flex-switch fund runway; **do NOT pour savings
  into a market where even the leader loses money.** Bootstrap from Layer-A clinic revenue.
- **Infra:** dev env (done); later a FHIR sandbox (HAPI FHIR) + validator; hosting TBD.
- **Certs (marketability, not blockers):** HL7 FHIR cert, PMP, ISO 27001.

## 8. Open questions (resolve before scaling)

1. Who on the team has the doctor / 學長學弟 network?
2. Legal structure for Layer B de-identification (get an opinion early).
3. Does [employer] actually grant 金流/FHIR exposure? (join-decision gate)
4. Layer A pricing & break-even clinic count.
5. Is "1+1" and the 金流 tool one platform or two products? (likely one platform, two modules.)
