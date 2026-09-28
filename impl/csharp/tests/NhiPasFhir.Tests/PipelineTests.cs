using System.Collections.Generic;
using System.Linq;
using NhiPasFhir;
using NhiPasFhir.Core;
using NhiPasFhir.Plugins;
using Xunit;

public class PipelineTests
{
    private static PACase CaseWith(string drug, string indication)
    {
        var c = Samples.CancerDrugCase();
        var data = new Dictionary<string, object>(c.Data) { ["drug_code"] = drug, ["indication"] = indication };
        return c with { Data = data };
    }

    [Fact] public void PreCheck_allows_good_pair()
        => Assert.Null(PreCheck.CheckPair("KC009612B5", "C50P1"));

    [Fact] public void PreCheck_blocks_bad_pair()
    {
        var f = PreCheck.CheckPair("KC009612B5", "C99X9");
        Assert.NotNull(f);
        Assert.Equal("error", f!.Severity);
        Assert.Contains("核刪", f.Message);
    }

    [Fact] public void PreCheck_unknown_drug_warns_not_blocks()
    {
        var f = PreCheck.CheckPair("ZZUNKNOWN", "C50P1");
        Assert.Equal("warning", f!.Severity);
        Assert.False(PreCheck.HasBlocking(new[] { f }));
    }

    [Fact] public void Pipeline_blocks_bad_pair_and_skips_assembly()
    {
        var r = Pipeline.Run(CaseWith("KC009612B5", "C99X9"));
        Assert.True(r.Blocked);
        Assert.Null(r.Bundle);
        Assert.Contains("does not guarantee", r.Advisory);
    }

    [Fact] public void Pipeline_clear_when_no_indication_assembles()
    {
        var r = Pipeline.Run(Samples.CancerDrugCase());
        Assert.False(r.Blocked);
        Assert.NotNull(r.Bundle);
        Assert.Equal(9, r.Bundle!.Entry.Count);
    }

    [Fact] public void Factory_has_two_case_types()
    {
        var reg = AssemblerFactory.Registered.ToHashSet();
        Assert.Contains(("tw.gov.mohw.nhi.pas#1.2.6", "cancer-drug"), reg);
        Assert.Contains(("tw.gov.mohw.nhi.pas#1.2.6", "immunologic-agent"), reg);
    }

    [Fact] public void Immunologic_dispatches_and_fails_loud()
    {
        var c = Samples.CancerDrugCase() with { CaseType = "immunologic-agent" };
        var a = AssemblerFactory.ForCase(c);
        Assert.IsType<ImmunologicAssembler>(a);
        Assert.Throws<System.NotImplementedException>(() => a.Assemble(c));
    }
}
