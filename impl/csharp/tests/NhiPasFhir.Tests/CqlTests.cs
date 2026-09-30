using System.Collections.Generic;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;
using NhiPasFhir;
using NhiPasFhir.Core;
using Xunit;

/// <summary>維度3 — the CQL self-check slot: toggle (off/on) + three-state interpretation. Uses a fake
/// ICqlEngine so we test the framework glue (drug→rule, closure call, output→verdict) without a real runtime.</summary>
public class CqlTests
{
    // Fake engine: returns canned named expression results keyed by what the test wants to simulate.
    private sealed class FakeEngine : ICqlEngine
    {
        private readonly Dictionary<string, object?> _named;
        public FakeEngine(Dictionary<string, object?> named) => _named = named;
        public Task<IReadOnlyDictionary<string, object?>> EvaluateAsync(string ruleId, Bundle bundle)
            => Task.FromResult<IReadOnlyDictionary<string, object?>>(_named);
    }

    private static ICqlPreCheck Enabled(Dictionary<string, object?> named, string drug = "BC27730100", string rule = "BCAbemaciclibRule1")
        => new CqlPreCheck(new FakeEngine(named),
            new Dictionary<string, IReadOnlyList<string>> { [drug] = new[] { rule } });

    [Fact] public async Task Off_by_default_no_cql_result()
    {
        var r = await Pipeline.RunAsync(Samples.CancerDrugCase());     // no ICqlPreCheck passed
        Assert.Null(r.Cql);
        Assert.NotNull(r.Bundle);
    }

    [Fact] public async Task On_pass_when_result_bool_true()
    {
        var cql = Enabled(new() { ["乳癌Abemaciclib申請結果_布林"] = true });
        var r = await Pipeline.RunAsync(Samples.CancerDrugCase(), cql: cql);
        Assert.Equal(CqlOutcome.Pass, r.Cql!.Outcome);
        Assert.False(r.Blocked);
    }

    [Fact] public async Task On_data_missing_blocks_as_補件()
    {   // final boolean false + a missing-data flag false → 補件
        var cql = Enabled(new() { ["乳癌Abemaciclib申請結果_布林"] = false, ["1規則1-2=有填荷爾蒙受體(HR)檢測資料"] = false });
        var r = await Pipeline.RunAsync(Samples.CancerDrugCase(), cql: cql);
        Assert.Equal(CqlOutcome.DataMissing, r.Cql!.Outcome);
        Assert.True(r.Blocked);
    }

    [Fact] public async Task On_pass_ignores_a_false_alternative_leg()
    {   // final boolean true wins even if an unused intermediate 有填 leg is false → NOT 補件
        var cql = Enabled(new() { ["乳癌Abemaciclib申請結果_布林"] = true, ["1規則1-2-1=有填ER檢測資料"] = false });
        var r = await Pipeline.RunAsync(Samples.CancerDrugCase(), cql: cql);
        Assert.Equal(CqlOutcome.Pass, r.Cql!.Outcome);
        Assert.False(r.Blocked);
    }

    [Fact] public async Task On_missing_verdict_is_NotEvaluated_not_rejected()
    {   // no 申請結果_布林 key → cannot judge → NotEvaluated (never default to 核刪)
        var cql = Enabled(new() { ["某中間值"] = false });
        var r = await Pipeline.RunAsync(Samples.CancerDrugCase(), cql: cql);
        Assert.Equal(CqlOutcome.NotEvaluated, r.Cql!.Outcome);
        Assert.False(r.Blocked);
    }

    [Fact] public async Task On_condition_not_met_blocks_as_核刪()
    {
        var cql = Enabled(new() { ["乳癌Abemaciclib申請結果_布林"] = false, ["報告總結"] = "HER2 未達陰性" });
        var r = await Pipeline.RunAsync(Samples.CancerDrugCase(), cql: cql);
        Assert.Equal(CqlOutcome.WouldBeRejected, r.Cql!.Outcome);
        Assert.Contains("HER2", string.Join("", r.Cql.Reasons));
        Assert.True(r.Blocked);
    }

    [Fact] public async Task NotWired_engine_fails_loud()
        => await Assert.ThrowsAsync<System.NotSupportedException>(() =>
            new NotWiredCqlEngine().EvaluateAsync("BCAbemaciclibRule1", new Bundle()));
}
