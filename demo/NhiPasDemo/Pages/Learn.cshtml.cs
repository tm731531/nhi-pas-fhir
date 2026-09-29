using Markdig;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NhiPasDemo.Pages;

/// <summary>Renders spec/docs/cql-explained.md (the 10-question CQL/FHIR walk-through) so the demo
/// teaches the concepts, not just runs a flow. Source of truth stays the repo doc — we render it live.</summary>
public class LearnModel : PageModel
{
    private readonly IWebHostEnvironment _env;
    public LearnModel(IWebHostEnvironment env) => _env = env;

    public HtmlString Html { get; private set; } = HtmlString.Empty;
    public string? Missing { get; private set; }

    public void OnGet()
    {
        var path = Path.GetFullPath(
            Path.Combine(_env.ContentRootPath, "..", "..", "spec", "docs", "cql-explained.md"));
        if (!System.IO.File.Exists(path)) { Missing = path; return; }

        var md = System.IO.File.ReadAllText(path);
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        Html = new HtmlString(Markdown.ToHtml(md, pipeline));
    }
}
