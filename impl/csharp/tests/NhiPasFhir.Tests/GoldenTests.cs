using System.IO;
using NhiPasFhir;
using NhiPasFhir.Core;
using Xunit;

/// <summary>Regression lock: each case type's serialized bundle must reproduce its golden file byte-for-byte.
/// Firely's serializer is deterministic, so any change to the assembled output makes these tests fail loudly.
/// To intentionally update a golden: run samples/Emit and copy build/*.cs.json over goldens/*.golden.json.</summary>
public class GoldenTests
{
    private static string GoldenPath(string name) =>
        Path.Combine(AppContext.BaseDirectory, "goldens", name);

    private static void AssertReproducesGolden(PACase c, string goldenFile)
    {
        var expected = File.ReadAllText(GoldenPath(goldenFile));
        var actual = NhiPas.ToJson(NhiPas.Build(c));
        Assert.Equal(expected, actual);   // byte-for-byte
    }

    [Fact] public void CancerDrug_reproduces_golden()
        => AssertReproducesGolden(Samples.CancerDrugCase(), "cancer-drug.golden.json");

    [Fact] public void Immunologic_reproduces_golden()
        => AssertReproducesGolden(Samples.ImmunologicCase(), "immunologic-agent.golden.json");
}
