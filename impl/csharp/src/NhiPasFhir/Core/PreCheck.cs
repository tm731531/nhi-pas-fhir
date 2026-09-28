namespace NhiPasFhir.Core;

/// <summary>A drug may only be claimed against these 給付適應症 codes (else 核刪 / clawback).</summary>
public sealed record DrugIndicationRule(string DrugCode, IReadOnlySet<string> AllowedIndications);

public sealed record Finding(string Severity, string DrugCode, string? IndicationCode, string Message);

/// <summary>Pre-submit drug↔indication check. Seeded with the two verified IG rules; the full set is a
/// data-load from the 預檢規則 CQL IG (TODO). Advisory only (Constitution III); fail-loud (IV).</summary>
public static class PreCheck
{
    // TODO: load the full rule set from the 預檢規則 CQL IG. Seeded subset verified from the pas IG.
    private static readonly Dictionary<string, DrugIndicationRule> Seed = new()
    {
        ["KC009612B5"] = new("KC009612B5", new HashSet<string> { "C50P1", "C50P2", "C50R1", "C16R1" }),
        ["KC010892B5"] = new("KC010892B5", new HashSet<string> { "C50P1", "C50P2", "C50P3", "C50P4", "C50P5", "C50R1", "C16R1" }),
    };

    public static Finding? CheckPair(string drug, string indication)
    {
        if (!Seed.TryGetValue(drug, out var rule))
            return new Finding("warning", drug, indication,
                $"drug {drug}: no rule loaded (seed subset only) — cannot verify indication {indication}. " +
                "TODO: load full rule set from 預檢規則 CQL IG.");
        if (!rule.AllowedIndications.Contains(indication))
            return new Finding("error", drug, indication,
                $"drug {drug} with indication {indication} would be 核刪: allowed indications are " +
                $"{{{string.Join(", ", rule.AllowedIndications.OrderBy(x => x))}}}.");
        return null;
    }

    public static List<Finding> Check(IEnumerable<(string drug, string indication)> pairs)
        => pairs.Select(p => CheckPair(p.drug, p.indication)).Where(f => f != null).Cast<Finding>().ToList();

    public static bool HasBlocking(IEnumerable<Finding> findings) => findings.Any(f => f.Severity == "error");
}
