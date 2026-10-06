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

    // Drug key matches Samples.CancerDrugCase()'s drug_code so the fake precheck selects the fake rule.
    // (This is a FakeEngine map, independent of the real DrugRuleMap — the key is just the lookup token.)
    private static ICqlPreCheck Enabled(Dictionary<string, object?> named, string drug = "BC27730100", string rule = "BCAbemaciclibRule1")
        => new CqlPreCheck(new FakeEngine(named),
            new Dictionary<string, IReadOnlyList<string>> { [drug] = new[] { rule } });

    [Fact] public async Task Off_by_default_no_cql_result()
    {
        var r = await Pipeline.RunAsync(Samples.CancerDrugCase());     // no ICqlPreCheck passed
        Assert.Null(r.Cql);
        Assert.NotNull(r.Bundle);
    }

    [Fact] public void DrugRuleMap_has_the_verified_rule()
    {
        // BC27640100 is one of the IG's L01EF03 (Abemaciclib) codes in Library-BCCodeConcept.
        // (The prior seed used BC27730100, which is NOT in the IG's L01EF03 define — corrected.)
        Assert.Equal(new[] { "BCAbemaciclibRule1" }, DrugRuleMap.Default["BC27640100"]);
        Assert.Contains("BC27640100", DrugRuleMap.CoveredDrugs);
        Assert.DoesNotContain("BC27730100", DrugRuleMap.CoveredDrugs); // fabricated code removed
    }

    [Fact] public async Task Uncovered_drug_is_NotEvaluated_never_fabricated()
    {
        // A drug with no loaded rule must fail loud (NotEvaluated), not guess a verdict.
        var cql = new CqlPreCheck(new FakeEngine(new()), DrugRuleMap.Default);
        var result = await cql.EvaluateAsync(NhiPas.Build(Samples.CancerDrugCase()), "ZZ99999999");
        Assert.Equal(CqlOutcome.NotEvaluated, result.Outcome);
    }

    [Fact] public async Task On_pass_when_result_bool_true()
    {
        var cql = Enabled(new() { ["乳癌Abemaciclib申請結果_布林"] = true });
        var r = await Pipeline.RunAsync(Samples.CancerDrugCase(), cql: cql);
        Assert.Equal(CqlOutcome.Pass, r.Cql!.Outcome);
        Assert.False(r.Blocked);
    }

    // The 補件/核刪 split is read from the rule's OWN 報告總結 sections (its branch-correct classifier),
    // not re-derived — so the fakes here carry a realistic 報告總結 with the rule's two labelled sections.
    private const string MissingReport =
        "【▲不符合項目 - 必要資料未填寫】\n▲ 1規則1-2：未提供荷爾蒙受體(HR)檢測資料\n";
    private const string ConditionReport =
        "【▲不符合項目 - 條件或代碼不符合】\n▲ 1條件ICD代碼檢核：主要疾病之ICD代碼未使用C50\n";

    [Fact] public async Task On_data_missing_blocks_as_補件()
    {   // boolean false + a 必要資料未填寫 item, no condition failure → 補件
        var cql = Enabled(new() { ["乳癌Abemaciclib申請結果_布林"] = false, ["報告總結"] = MissingReport });
        var r = await Pipeline.RunAsync(Samples.CancerDrugCase(), cql: cql);
        Assert.Equal(CqlOutcome.DataMissing, r.Cql!.Outcome);
        Assert.True(r.Blocked);
    }

    [Fact] public async Task On_pass_ignores_a_false_alternative_leg()
    {   // final boolean true wins even if an unused intermediate leg is false → NOT 補件
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
    {   // a 條件或代碼不符合 item → 核刪
        var cql = Enabled(new() { ["乳癌Abemaciclib申請結果_布林"] = false, ["報告總結"] = ConditionReport });
        var r = await Pipeline.RunAsync(Samples.CancerDrugCase(), cql: cql);
        Assert.Equal(CqlOutcome.WouldBeRejected, r.Cql!.Outcome);
        Assert.Contains("C50", string.Join("", r.Cql.Reasons));
        Assert.True(r.Blocked);
    }

    [Fact] public async Task When_both_sections_present_核刪_wins_and_no_reason_is_dropped()
    {   // both missing-data AND a condition failure → 核刪 (harder gate), and BOTH reasons surface
        var cql = Enabled(new() { ["乳癌Abemaciclib申請結果_布林"] = false, ["報告總結"] = ConditionReport + MissingReport });
        var r = await Pipeline.RunAsync(Samples.CancerDrugCase(), cql: cql);
        Assert.Equal(CqlOutcome.WouldBeRejected, r.Cql!.Outcome);
        var reasons = string.Join("\n", r.Cql.Reasons);
        Assert.Contains("C50", reasons);        // 核刪 reason not dropped
        Assert.Contains("荷爾蒙受體", reasons);  // 補件 reason still listed
    }

    [Fact] public async Task NotWired_engine_fails_loud()
        => await Assert.ThrowsAsync<System.NotSupportedException>(() =>
            new NotWiredCqlEngine().EvaluateAsync("BCAbemaciclibRule1", new Bundle()));
}
