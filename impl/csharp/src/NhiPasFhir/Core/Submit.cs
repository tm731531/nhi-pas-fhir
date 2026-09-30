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

/// <summary>Real transport: POST the claim Bundle to a PAS receiver endpoint and read back its response
/// Bundle. Demo points <c>endpoint</c> at the demo's fake receiver; production points it at the real
/// 健保 endpoint. Only the URL (and, in production, the auth/cert) changes — the flow is identical.</summary>
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
