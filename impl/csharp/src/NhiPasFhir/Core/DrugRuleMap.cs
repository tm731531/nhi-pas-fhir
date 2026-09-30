namespace NhiPasFhir.Core;

/// <summary>Single source of truth for the 藥碼 → CQL 規則 mapping (1:N — a drug may be gated by several
/// rules, all of which must pass). This is the index the CQL pre-check (查) uses to pick which official
/// rule(s) to run for a given drug.
///
/// **Honest scope.** Seeded with the ONE drug rule proven end-to-end on the faithful engine
/// (乳癌 Abemaciclib → Library-BCAbemaciclibRule1 + its dependency closure in cql-engine/rules/). The NHI
/// catalogue is ~66 rules, published as the DRAFT CQL IG `tw.gov.mohw.nhi.cql` (v0.0.1) — which is not
/// yet distributed as a fetchable FHIR package. Adding more is a **data task, never fabrication**: drop
/// the official `Library-*.json` into cql-engine/rules/, load it into the engine, verify it runs, then
/// add its drug↔rule row here. Until a rule is actually loaded, the 查 step returns NotEvaluated for that
/// drug (fail-loud), it does NOT guess a verdict.</summary>
public static class DrugRuleMap
{
    /// <summary>藥碼 → 1..N ruleId. Every rule here has its Library resource vendored in cql-engine/rules/.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Default =
        new Dictionary<string, IReadOnlyList<string>>
        {
            // 乳癌 Abemaciclib — verified end-to-end on CQF-Ruler (68 defines, InCodeSystem exercised).
            ["BC27730100"] = new[] { "BCAbemaciclibRule1" },
        };

    /// <summary>Drug codes that currently have a loaded CQL rule (the 查 step can predict 核刪/補件 for these).</summary>
    public static IReadOnlyCollection<string> CoveredDrugs => Default.Keys.ToList();
}
