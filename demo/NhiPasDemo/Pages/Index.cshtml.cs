using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NhiPasFhir;
using NhiPasFhir.Core;

namespace NhiPasDemo.Pages;

/// <summary>The demo's single page: pick a case → the NhiPasFhir lib assembles a Bundle and runs the
/// pipeline (產→驗→查) → we render the three-state verdict + the Bundle. CQL is opt-in and talks to the
/// running CQF-Ruler server; if it's down we fall back to assembling without it and say so.</summary>
public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    public IndexModel(IHttpClientFactory httpFactory, IConfiguration config)
        => (_httpFactory, _config) = (httpFactory, config);

    public static readonly (string Key, string Label)[] Cases =
    {
        ("cancer", "癌症藥物送核 — 乳癌 Abemaciclib"),
        ("immunologic", "免疫製劑 (Immunologic)"),
        ("appeal", "申復 (Appeal)"),
        ("self", "自主審查 (Self-assessment)"),
    };

    [BindProperty] public string SelectedCase { get; set; } = "cancer";
    [BindProperty] public bool RunCql { get; set; }

    public bool HasResult { get; private set; }
    public bool Blocked { get; private set; }
    public string Advisory { get; private set; } = "";
    public IReadOnlyList<Finding> Findings { get; private set; } = Array.Empty<Finding>();
    public CqlFinding? Cql { get; private set; }
    public string? CqlError { get; private set; }
    public string BundleJson { get; private set; } = "";
    public int EntryCount { get; private set; }
    public IReadOnlyList<(string Type, int Count)> ResourceCounts { get; private set; } =
        Array.Empty<(string, int)>();

    private static PACase CaseFor(string key) => key switch
    {
        "immunologic" => Samples.ImmunologicCase(),
        "appeal" => Samples.AppealCase(),
        "self" => Samples.SelfAssessmentCase(),
        _ => Samples.CancerDrugCase(),
    };

    public void OnGet() { }

    public void OnPost()
    {
        var c = CaseFor(SelectedCase);

        ICqlPreCheck? cql = null;
        if (RunCql)
        {
            var baseUrl = _config["Cql:BaseUrl"] ?? "http://localhost:8095/fhir";
            var engine = new CqfRulerCqlEngine(_httpFactory.CreateClient(), baseUrl);
            // drug → rule(s). Only the cancer sample's drug is mapped in this demo; others → NotEvaluated.
            var map = new Dictionary<string, IReadOnlyList<string>> { ["BC27730100"] = new[] { "BCAbemaciclibRule1" } };
            cql = new CqlPreCheck(engine, map);
        }

        PipelineResult result;
        try
        {
            result = Pipeline.Run(c, cql: cql);
        }
        catch (Exception ex)
        {
            // CQL server unreachable (or evaluation error) — assemble without CQL and surface it.
            CqlError = ex.Message;
            result = Pipeline.Run(c);
        }

        Findings = result.Findings;
        Blocked = result.Blocked;
        Advisory = result.Advisory;
        Cql = result.Cql;
        if (result.Bundle is not null)
        {
            EntryCount = result.Bundle.Entry.Count;
            BundleJson = Prettify(NhiPas.ToJson(result.Bundle));
            ResourceCounts = result.Bundle.Entry
                .Where(e => e.Resource is not null)
                .GroupBy(e => e.Resource.TypeName)
                .Select(g => (g.Key, g.Count()))
                .OrderByDescending(x => x.Item2)
                .ThenBy(x => x.Key)
                .ToList();
        }
        HasResult = true;
    }

    private static string Prettify(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch { return json; }
    }
}
