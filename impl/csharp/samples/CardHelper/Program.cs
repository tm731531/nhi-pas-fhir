using System.Net;
using System.Text;
using System.Text.Json;
using NhiPasFhir.CardReader;

// ───────────────────────────────────────────────────────────────────────────────────────────────
// 小工具 card-helper — the ONLY piece that touches the PC/SC reader.
//
// Exposes GET http://localhost:8531/card : reads the 健保卡 基本資料段 and returns it as JSON.
// Does NO FHIR work (the 網頁 app does that). Mirrors tw-nhi-icc-service: a thin local card bridge.
//
// 🔒 Real data: returns YOUR real demographics to localhost only — it never writes to disk or the repo.
// If no reader/card is present it returns 503 with an error (it does NOT fabricate — reading a card is
// the one thing this tool must be honest about).
// ───────────────────────────────────────────────────────────────────────────────────────────────

const string prefix = "http://localhost:8531/";
var listener = new HttpListener();
listener.Prefixes.Add(prefix);
listener.Start();
Console.WriteLine($"🔌 card-helper (小工具) listening on {prefix}card  — reads 健保卡 via PC/SC, returns JSON.");
Console.WriteLine("   Press Ctrl+C to stop.");

var json = new JsonSerializerOptions { WriteIndented = true };

while (true)
{
    var ctx = listener.GetContext();
    var res = ctx.Response;
    // CORS so a browser page (served from another origin/port) can call this directly if desired.
    res.AddHeader("Access-Control-Allow-Origin", "*");

    if (ctx.Request.HttpMethod == "OPTIONS") { res.StatusCode = 204; res.Close(); continue; }

    if (ctx.Request.Url?.AbsolutePath != "/card") { res.StatusCode = 404; res.Close(); continue; }

    string body;
    try
    {
        var card = new NhiCardReader().ReadFirstCard();
        res.StatusCode = 200;
        body = JsonSerializer.Serialize(new
        {
            ok = true,
            card.CardNo, card.FullName, card.IdNo, card.BirthDateIso,
            sex = card.Sex.ToString(), card.IssueDateIso,
        }, json);
    }
    catch (Exception e)
    {
        // No reader / no card / no PC-SC subsystem — honest error, never a fabricated card.
        res.StatusCode = 503;
        body = JsonSerializer.Serialize(new { ok = false, error = e.Message }, json);
    }

    res.ContentType = "application/json; charset=utf-8";
    var bytes = Encoding.UTF8.GetBytes(body);
    res.OutputStream.Write(bytes, 0, bytes.Length);
    res.Close();
    Console.WriteLine($"  /card -> {res.StatusCode}");
}
