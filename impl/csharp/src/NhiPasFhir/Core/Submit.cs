using System.Net.Http;
using System.Text;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace NhiPasFhir.Core;

/// <summary>維度 = 送/回. Result of submitting a claim Bundle to an NHI PAS receiver.
/// IMPORTANT: Accepted means the receiver TOOK the submission (收件) — it is NOT approval. The real
/// decision is carried by the returned ClaimResponse.Outcome (queued 審核中 / complete …).</summary>
public sealed record SubmitResult(bool Accepted, string Status, Bundle? Response, string Message);

/// <summary>The "送" socket — transport to NHI PAS. Same toggle/adapter shape as ICqlEngine: the demo
/// plugs a fake receiver; production plugs the real 健保 endpoint (+ HCA cert / VPN). The lib PRODUCES
/// the correct Bundle; this is the thin transport hop on top.</summary>
public interface IPasSubmitter
{
    bool Enabled { get; }
    Task<SubmitResult> SubmitAsync(Bundle claimBundle);
}

/// <summary>「未接收件端」— 預設。Assemble/check still run; nothing is transmitted anywhere.</summary>
public sealed class NoSubmitter : IPasSubmitter
{
    public static readonly NoSubmitter Instance = new();
    public bool Enabled => false;
    public Task<SubmitResult> SubmitAsync(Bundle claimBundle)
        => Task.FromResult(new SubmitResult(false, "not-submitted", null, "送件未啟用(未接收件端)。"));
}

/// <summary>「演練模式」dry-run — assemble + validate the submission payload and report what WOULD be sent,
/// but transmit nothing. Distinct from NoSubmitter (which does not even build a payload): this proves the
/// Bundle is wire-ready (serializes + strict-reparses) and reports its byte size, so a UI can show「組好了、
/// 驗過了、按真送就會送這包」without needing HCA/VPN. Real transmission is HttpPasSubmitter (gated, #13).</summary>
public sealed class DryRunPasSubmitter : IPasSubmitter
{
    public static readonly DryRunPasSubmitter Instance = new();
    public bool Enabled => true;

    public Task<SubmitResult> SubmitAsync(Bundle claimBundle)
    {
        var payload = FhirJson.Serialize(claimBundle);
        // Prove it is wire-ready: strict reparse throws if the payload is structurally invalid.
        _ = FhirJson.Parse<Bundle>(payload);
        var bytes = Encoding.UTF8.GetByteCount(payload);
        return Task.FromResult(new SubmitResult(
            false, "dry-run", null,
            $"DRY-RUN:payload 已組好並通過結構驗證({bytes} bytes),但未實送。真送需 HCA 醫事憑證 + 健保 VPN(#13)。"));
    }
}

/// <summary>Real transport: POST the claim Bundle to a PAS receiver endpoint and read back its response
/// Bundle. Demo points <c>endpoint</c> at the demo's fake receiver. Production: configure the injected
/// <c>HttpClient</c> with the HCA 醫事憑證 client cert and route it over the 健保 VPN, then point
/// <c>endpoint</c> at the real NHI endpoint — code unchanged. NOTE: the real 健保 transport may instead be
/// the installed 醫療資料傳輸共通介面 (not plain REST); if so, write one more IPasSubmitter wrapping that
/// local API — same socket. Exact binding is in the login-walled 介面規格. See
/// spec/docs/real-submission-adapter.md for the full readiness picture + the production-wiring snippet.</summary>
public sealed class HttpPasSubmitter : IPasSubmitter
{
    private readonly HttpClient _http;
    private readonly string _endpoint;
    public HttpPasSubmitter(HttpClient http, string endpoint) => (_http, _endpoint) = (http, endpoint);

    public bool Enabled => true;

    public async Task<SubmitResult> SubmitAsync(Bundle claimBundle)
    {
        var body = new StringContent(FhirJson.Serialize(claimBundle), Encoding.UTF8, "application/fhir+json");
        var resp = await _http.PostAsync(_endpoint, body).ConfigureAwait(false);
        var text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
            return new(false, ((int)resp.StatusCode).ToString(), null,
                $"收件端回 HTTP {(int)resp.StatusCode}:{text[..Math.Min(200, text.Length)]}");

        Bundle? response = null;
        try { response = FhirJson.Parse<Bundle>(text); } catch { /* non-Bundle response */ }

        // 收件成功 ≠ 核准:the state is whatever the ClaimResponse says (queued 審核中 / complete …).
        var cr = response?.Entry.Select(e => e.Resource).OfType<ClaimResponse>().FirstOrDefault();
        var status = cr?.Outcome?.ToString() ?? "received";
        return new(true, status, response, cr?.Disposition ?? "已受理。");
    }
}
