using Markdig;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NhiPasDemo.Pages;

/// <summary>Renders spec/docs/cql-wiring.md — "how CQL is wired into our framework". Same live-render
/// approach as LearnModel; the repo doc stays the source of truth.</summary>
public class WiringModel : PageModel
{
    private readonly IWebHostEnvironment _env;
    public WiringModel(IWebHostEnvironment env) => _env = env;

    public HtmlString Html { get; private set; } = HtmlString.Empty;
    public string? Missing { get; private set; }

    public void OnGet()
    {
        var path = Path.GetFullPath(
            Path.Combine(_env.ContentRootPath, "..", "..", "spec", "docs", "cql-wiring.md"));
        if (!System.IO.File.Exists(path)) { Missing = path; return; }

        var md = System.IO.File.ReadAllText(path);
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        Html = new HtmlString(Markdown.ToHtml(md, pipeline));
    }
}
