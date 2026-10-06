using System;
using System.Collections.Generic;
using System.Net.Http;
using NhiPasFhir;
using NhiPasFhir.Core;
using Xunit;

/// <summary>維度3 end-to-end against the REAL faithful engine (option C). Requires the CQF-Ruler
/// server up with the rules loaded:  cd cql-engine/server && docker compose up -d && node load-libraries.mjs
/// If the server isn't reachable the tests report as SKIPPED (not passed) via [SkippableFact], so CI
/// without the server stays green while telling the truth. Live counterpart to CqlTests.cs (fakes the engine).</summary>
public class CqfRulerIntegrationTests
{
    private const string BaseUrl = "http://localhost:8095/fhir";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    private static bool ServerUp()
    {
        try { return Http.GetAsync($"{BaseUrl}/metadata").GetAwaiter().GetResult().IsSuccessStatusCode; }
        catch { return false; }
    }

    [SkippableFact]
    public async System.Threading.Tasks.Task Engine_runs_the_real_rule_and_returns_named_results()
    {
        Skip.IfNot(ServerUp(), "CQF-Ruler not reachable on :8095 — start it to run the live CQL test");

        var engine = new CqfRulerCqlEngine(Http, BaseUrl);
        var bundle = NhiPas.Build(Samples.CancerDrugCase());

        var results = await engine.EvaluateAsync("BCAbemaciclibRule1", bundle);

        // The rule ran to completion (dozens of defines), and InCodeSystem worked (ICD data seen).
        Assert.True(results.Count > 30);
        Assert.True(results.ContainsKey("乳癌Abemaciclib申請結果_布林"));
        Assert.Equal(true, results["主要疾病ICD資料存在"]); // InCodeSystem membership evaluated
    }

    [SkippableFact]
    public async System.Threading.Tasks.Task PreCheck_blocks_the_myeloma_bundle_against_the_breast_cancer_rule()
    {
        Skip.IfNot(ServerUp(), "CQF-Ruler not reachable on :8095 — start it to run the live CQL test");

        var engine = new CqfRulerCqlEngine(Http, BaseUrl);
        var cql = new CqlPreCheck(engine, DrugRuleMap.Default);   // the committed 藥碼→規則 map

        // Use a real IG L01EF03 (Abemaciclib) code so DrugRuleMap selects BCAbemaciclibRule1. The sample's
        // default drug_code (BC27730100) is not an IG-listed NHI code, so it would be NotEvaluated — see
        // the DrugRuleMap correction. Override just the code; the case stays a myeloma (C90.00) case.
        var myeloma = Samples.CancerDrugCase();
        var data = new Dictionary<string, object>(myeloma.Data) { ["drug_code"] = "BC27640100" };
        var r = await Pipeline.RunAsync(myeloma with { Data = data }, cql: cql);

        // The bundle is a myeloma case; the breast-cancer Abemaciclib rule must not pass it.
        Assert.NotNull(r.Cql);
        Assert.NotEqual(CqlOutcome.Pass, r.Cql!.Outcome);
        Assert.True(r.Blocked);
    }

    // Newly-vendored CRC*/HCC* rules must actually EVALUATE on the faithful engine: each runs to its
    // verdict 布林 define with no OperationOutcome (EvaluateAsync throws on an evaluation error, so a
    // returned verdict key is proof the whole rule + its CodeConcept/Reusable deps translated and ran).
    [SkippableTheory]
    [InlineData("CRCRegorafenibRule1", "大腸直腸癌Regorafenib申請結果_布林")]
    [InlineData("CRCBevacizumabRule1", "大腸直腸癌Bevacizumab第一線治療申請結果_布林")]
    [InlineData("HCCSorafenibRule1", "肝癌Sorafenib申請結果_布林")]
    [InlineData("HCCAtezoDurvaRule1", "肝癌AtezoDurva申請結果_布林")]
    public async System.Threading.Tasks.Task CRC_HCC_rules_evaluate_to_their_verdict(string ruleId, string verdictKey)
    {
        Skip.IfNot(ServerUp(), "CQF-Ruler not reachable on :8095 — start it to run the live CQL test");

        var engine = new CqfRulerCqlEngine(Http, BaseUrl);
        var bundle = NhiPas.Build(Samples.CancerDrugCase());

        var results = await engine.EvaluateAsync(ruleId, bundle);

        Assert.True(results.Count > 10, $"{ruleId} evaluated too few defines ({results.Count})");
        Assert.True(results.ContainsKey(verdictKey), $"{ruleId} missing verdict define '{verdictKey}'");
    }
}
