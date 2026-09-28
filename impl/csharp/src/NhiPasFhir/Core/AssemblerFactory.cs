namespace NhiPasFhir.Core;

/// <summary>Registry: pick the case assembler by (ig, case_type). Callers stay generic; unknown = fail loud.</summary>
public static class AssemblerFactory
{
    private static readonly Dictionary<(string ig, string caseType), Func<ICaseAssembler>> Registry = new();

    public static void Register(string ig, string caseType, Func<ICaseAssembler> factory)
        => Registry[(ig, caseType)] = factory;

    public static ICaseAssembler ForCase(PACase pacase)
    {
        var key = (pacase.Ig, pacase.CaseType);
        if (!Registry.TryGetValue(key, out var f))
            throw new KeyNotFoundException($"no assembler registered for {key}; registered: {string.Join(", ", Registry.Keys)}");
        return f();
    }

    public static IReadOnlyCollection<(string, string)> Registered => Registry.Keys.ToList();
}
