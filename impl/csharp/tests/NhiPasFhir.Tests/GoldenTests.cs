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

    [Fact] public void Appeal_reproduces_golden()
        => AssertReproducesGolden(Samples.AppealCase(), "appeal.golden.json");

    [Fact] public void SelfAssessment_reproduces_golden()
        => AssertReproducesGolden(Samples.SelfAssessmentCase(), "self-assessment.golden.json");

    [Fact] public void CatastrophicIllness_reproduces_golden()   // 重大傷病 (nhi.ci) — validated 0 errors
        => AssertReproducesGolden(Samples.CatastrophicIllnessCase(), "catastrophic-illness.golden.json");

    [Fact] public void EPrescription_reproduces_golden()         // 電子處方箋 (nhi.empd) — validated 0 errors
        => AssertReproducesGolden(Samples.EPrescriptionCase(), "e-prescription.golden.json");

    [Fact] public void UuidStyle_reproduces_golden()
    {
        var expected = File.ReadAllText(GoldenPath("uuid.golden.json"));
        var actual = NhiPas.ToJson(NhiPas.ToUuidStyle(NhiPas.Build(Samples.SelfAssessmentCase())));
        Assert.Equal(expected, actual);
    }

    [Fact] public void Response_reproduces_golden()
    {
        var expected = File.ReadAllText(GoldenPath("response.golden.json"));
        var actual = NhiPas.ToJson(NhiPas.BuildResponse(Samples.ResponseCase()));
        Assert.Equal(expected, actual);
    }

    [Fact] public void Outcome_reproduces_golden()
    {
        var expected = File.ReadAllText(GoldenPath("outcome.golden.json"));
        var actual = NhiPas.ToJson(NhiPas.BuildOutcome(Samples.ErrorOutcome()));
        Assert.Equal(expected, actual);
    }

    public static IEnumerable<object[]> VariantCases()
    {
        foreach (var (file, _) in NhiPasFhir.Variants.All()) yield return new object[] { file };
    }

    [Theory] [MemberData(nameof(VariantCases))]
    public void Variant_reproduces_golden(string file)
    {
        var resource = NhiPasFhir.Variants.All().First(v => v.File == file).Resource;
        var expected = File.ReadAllText(GoldenPath($"variant-{file}.golden.json"));
        Assert.Equal(expected, NhiPas.ToJson(resource));
    }
}
