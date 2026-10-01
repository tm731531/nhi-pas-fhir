using System.Text.Json;
using Hl7.Fhir.Model;
using NhiPasFhir;
using NhiPasFhir.Core;
using NhiPasFhir.MediaDeclaration;

// ───────────────────────────────────────────────────────────────────────────────────────────────
// 網頁 app (:8530) — serves the 插卡 UI and runs the FHIR chain. It does NOT touch the reader; it calls
// the 小工具 card-helper (:8531) for card data. 一條龍: 讀卡(→小工具) → 進料/組 FHIR → 驗證 → 送件(dry-run).
//
// 🔒 Real data: in 真卡 mode your real demographics flow through in-memory and are shown in the browser
// (localhost). This app writes NOTHING to disk and persists nothing. 病情/醫令 is a fabricated sample;
// 送件 is DRY-RUN (not transmitted).
// ───────────────────────────────────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
const string cardHelperUrl = "http://localhost:8531/card";

app.MapGet("/api/run", async (string? mode, IHttpClientFactory hcf) =>
{
    // ── ① 讀卡 — demo mode uses a fabricated card; live mode calls the 小工具 (:8531) ──────────────
    CardDto card;
    if (mode == "demo")
    {
        card = CardDto.Fabricated();
    }
    else
    {
        try
        {
            var http = hcf.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(5);
            var raw = await http.GetStringAsync(cardHelperUrl);
            card = JsonSerializer.Deserialize<CardDto>(raw, jsonOpts)!;
            if (!card.Ok)
                return Results.Json(new { ok = false, stage = "讀卡", error = card.Error ?? "小工具回報無法讀卡(請插卡)。" });
        }
        catch (Exception e)
        {
            return Results.Json(new { ok = false, stage = "讀卡",
                error = $"連不到讀卡小工具 (:8531):{e.Message}。請先啟動 card-helper 並插卡,或改用示範模式。" });
        }
    }

    // ── ② 進料/組 FHIR — YOUR identity (real in 真卡 mode) + a fabricated sample 醫令 → Bundle ──────
    var rec = new MediaRecord();
    rec.Summary["t2"] = "DEMO-HOSP-0001";                 // 服務機構代號 (demo)
    rec.Summary["t6"] = "1130610";                        // 申報日期 (sample)
    rec.Case["d3"] = card.IdNo ?? "";                     // ← 你的身分證 (真卡模式=真)
    rec.Case["d11"] = RocDate.ToRoc(card.BirthDateIso) ?? ""; // ← 你的生日
    rec.Case["d9"] = "1130520"; rec.Case["d10"] = "1130520";  // 就醫日期 (sample)
    rec.Case["d19"] = "E1140";                            // 主診斷 (sample 示範)
    rec.Case["d30"] = "DEMO-DR-01";                       // 醫師 (sample)
    rec.Orders.Add(new Dictionary<string, string>        // 示範醫令一條
    {
        ["p13"] = "1", ["p3"] = "1", ["p4"] = "BC12345100",
        ["p7"] = "TIDPC", ["p9"] = "PO", ["p10"] = "30", ["p11"] = "5",
    });
    var bundle = MediaDeclarationConverter.ToFhir(rec);
    var bundleJson = NhiPas.ToJson(bundle);

    // ── ③ 驗證 — base-R4 structural gate (strict reparse) ─────────────────────────────────────────
    bool valid; string validateMsg;
    try { _ = FhirJson.Parse<Bundle>(bundleJson); valid = true; validateMsg = "strict reparse OK — base-R4 structurally valid（媒體申報無 FHIR IG,故為 base R4 非 profile）"; }
    catch (Exception e) { valid = false; validateMsg = e.Message; }

    // ── ④ 送件 — DRY-RUN (not transmitted) ────────────────────────────────────────────────────────
    var submit = await DryRunPasSubmitter.Instance.SubmitAsync(bundle);

    var claim = bundle.Entry.Select(e => e.Resource).OfType<Claim>().First();
    return Results.Json(new
    {
        ok = true,
        mode = mode == "demo" ? "示範(假卡)" : "真卡",
        card = new { card.FullName, card.IdNo, card.BirthDateIso, card.Sex },
        fhir = new { resources = bundle.Entry.Count, claimItems = claim.Item.Count },
        valid,
        validateMsg,
        submit = new { submit.Status, submit.Message },
        bundleJson,
    });
});

app.Run("http://localhost:8530");

// Shape of the 小工具's /card response (property-name case-insensitive).
public sealed record CardDto
{
    public bool Ok { get; init; }
    public string? CardNo { get; init; }
    public string? FullName { get; init; }
    public string? IdNo { get; init; }
    public string? BirthDateIso { get; init; }
    public string? Sex { get; init; }
    public string? IssueDateIso { get; init; }
    public string? Error { get; init; }

    public static CardDto Fabricated() => new()
    {
        Ok = true, CardNo = "000012345678", FullName = "王小明", IdNo = "A123456789",
        BirthDateIso = "1996-03-12", Sex = "Male", IssueDateIso = "2016-06-01",
    };
}
