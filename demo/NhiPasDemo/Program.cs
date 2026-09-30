var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpClient(); // used by the demo to reach the CQF-Ruler CQL server

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// ── Fake NHI PAS receiver (示範用) ──────────────────────────────────────────────
// Stands in for 健保's real submission endpoint so the "送" step has a real place to POST to.
// It ACCEPTS the claim (收件) and returns a ClaimResponse with Outcome=Queued (審核中) — deliberately
// NOT an approval, to make 收件≠核准 concrete. Production points HttpPasSubmitter at the real endpoint.
app.MapPost("/fake-nhi/submit", async (HttpRequest req) =>
{
    using var reader = new StreamReader(req.Body);
    var text = await reader.ReadToEndAsync();
    var bundle = NhiPasFhir.Core.FhirJson.Parse<Hl7.Fhir.Model.Bundle>(text);

    Hl7.Fhir.Model.Resource? First(string type) =>
        bundle.Entry.Select(e => e.Resource).FirstOrDefault(r => r?.TypeName == type);
    var patient = First("Patient");
    var claim = First("Claim");
    var org = First("Organization");

    var rc = new NhiPasFhir.ResponseCase(
        ResponseId: "FAKE-ACK-0001",
        PatientRef: patient is not null ? $"Patient/{patient.Id}" : "Patient/unknown",
        HospitalRef: org is not null ? $"Organization/{org.Id}" : "Organization/unknown",
        ClaimRef: claim is not null ? $"Claim/{claim.Id}" : "Claim/unknown",
        Created: "2026-09-29",
        Disposition: "【假收件端示範】已受理,審核中 — 這不是真健保,也不是核定結果。",
        OverallApproveCode: "0", // 0 = 審核中 (claim-level)
        Items: new[] { new NhiPasFhir.ResponseItem(1, "0") }); // per-order detail, 0 = 審核中

    var respBundle = NhiPasFhir.ResponseBuilder.Build(rc);
    var cr = respBundle.Entry.Select(e => e.Resource).OfType<Hl7.Fhir.Model.ClaimResponse>().First();
    cr.Outcome = Hl7.Fhir.Model.ClaimProcessingCodes.Queued; // 審核中:收下但還沒判

    return Results.Content(NhiPasFhir.NhiPas.ToJson(respBundle), "application/fhir+json");
});

app.Run();
