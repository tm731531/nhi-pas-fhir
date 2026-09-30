using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;
using NhiPasFhir;
using NhiPasFhir.Core;
using Xunit;

/// <summary>Unit tests for the 送/回 + 查 transport adapters (HttpPasSubmitter, CqfRulerCqlEngine) with a
/// stub HttpMessageHandler — so these run on every `dotnet test` without a live server (the live
/// end-to-end path is CqfRulerIntegrationTests, which no-ops when :8095 is down).</summary>
public class TransportTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _code;
        private readonly string _body;
        public StubHandler(HttpStatusCode code, string body) => (_code, _body) = (code, body);
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(_code) { Content = new StringContent(_body) });
    }

    private static HttpClient Client(HttpStatusCode code, string body) => new(new StubHandler(code, body));

    private static Bundle BundleWithPatient()
    {
        var b = new Bundle { Type = Bundle.BundleType.Collection };
        b.Entry.Add(new Bundle.EntryComponent { Resource = new Patient { Id = "pat-1" } });
        return b;
    }

    // ── HttpPasSubmitter (送/回) ──────────────────────────────────────────────
    [Fact] public async Task Submit_surfaces_queued_status_from_ClaimResponse()
    {
        var resp = ResponseBuilder.Build(new ResponseCase(
            "R1", "Patient/pat-1", "Organization/o", "Claim/c", "2026-09-30", "d", "0",
            new[] { new ResponseItem(1, "0") }));
        resp.Entry.Select(e => e.Resource).OfType<ClaimResponse>().First().Outcome = ClaimProcessingCodes.Queued;

        var s = new HttpPasSubmitter(Client(HttpStatusCode.OK, NhiPas.ToJson(resp)), "http://x/submit");
        var r = await s.SubmitAsync(BundleWithPatient());

        Assert.True(r.Accepted);
        Assert.Equal("Queued", r.Status);   // 收件 ≠ 核准
        Assert.NotNull(r.Response);
    }

    [Fact] public async Task Submit_accepts_non_bundle_body_as_received()
    {
        var s = new HttpPasSubmitter(Client(HttpStatusCode.OK, "{\"resourceType\":\"OperationOutcome\"}"), "http://x");
        var r = await s.SubmitAsync(BundleWithPatient());
        Assert.True(r.Accepted);
        Assert.Equal("received", r.Status);
    }

    [Fact] public async Task Submit_reports_http_error_as_not_accepted()
    {
        var s = new HttpPasSubmitter(Client(HttpStatusCode.BadRequest, "bad request body"), "http://x");
        var r = await s.SubmitAsync(BundleWithPatient());
        Assert.False(r.Accepted);
        Assert.Equal("400", r.Status);
    }

    // ── CqfRulerCqlEngine (查) ────────────────────────────────────────────────
    [Fact] public async Task Engine_maps_named_boolean_parameter()
    {
        var p = new Parameters();
        p.Add("乳癌Abemaciclib申請結果_布林", new FhirBoolean(true));
        var e = new CqfRulerCqlEngine(Client(HttpStatusCode.OK, NhiPas.ToJson(p)), "http://x/fhir");

        var d = await e.EvaluateAsync("R", BundleWithPatient());
        Assert.Equal(true, d["乳癌Abemaciclib申請結果_布林"]);
    }

    [Fact] public async Task Engine_fails_loud_on_evaluation_OperationOutcome()
    {
        var p = new Parameters();
        p.Parameter.Add(new Parameters.ParameterComponent
        {
            Name = "evaluation error",
            Resource = new OperationOutcome { Issue = { new OperationOutcome.IssueComponent
                { Severity = OperationOutcome.IssueSeverity.Error, Code = OperationOutcome.IssueType.Exception,
                  Details = new CodeableConcept { Text = "boom" } } } },
        });
        var e = new CqfRulerCqlEngine(Client(HttpStatusCode.OK, NhiPas.ToJson(p)), "http://x/fhir");
        await Assert.ThrowsAsync<NotSupportedException>(() => e.EvaluateAsync("R", BundleWithPatient()));
    }

    [Fact] public async Task Engine_fails_loud_on_http_500_with_status_in_message()
    {
        var e = new CqfRulerCqlEngine(Client(HttpStatusCode.InternalServerError, "<html>oops</html>"), "http://x/fhir");
        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => e.EvaluateAsync("R", BundleWithPatient()));
        Assert.Contains("500", ex.Message);
    }

    [Fact] public async Task Engine_throws_when_bundle_has_no_patient()
    {
        var e = new CqfRulerCqlEngine(Client(HttpStatusCode.OK, "{}"), "http://x/fhir");
        await Assert.ThrowsAsync<NotSupportedException>(() => e.EvaluateAsync("R", new Bundle()));
    }
}
