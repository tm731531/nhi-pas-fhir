using System.Net.Http;
using System.Text;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace NhiPasFhir.Core;

/// <summary>ICqlEngine backed by a CQF-Ruler / HAPI clinical-reasoning FHIR server (option C).
/// It POSTs our Bundle to <c>Library/{ruleId}/$evaluate</c> and returns the named expression
/// results, which <see cref="CqlPreCheck"/> interprets into the three-state verdict.
///
/// The rules live in the SERVER (loaded via cql-engine/server/load-libraries.mjs), not here — so a
/// rule/IG update = reload + re-run the same Bundle, no C# change (Tom's replaceability invariant).
/// Swapping the engine = swap only this adapter. Faithful: this server implements InCodeSystem,
/// which the JS cql-execution engine does not. See spec/docs/cql-integration-notes.md §6.</summary>
public sealed class CqfRulerCqlEngine : ICqlEngine
{
    private static readonly FhirJsonSerializer Serializer = new();
    private static readonly FhirJsonParser Parser = new();

    private readonly HttpClient _http;
    private readonly string _baseUrl;

    /// <param name="baseUrl">FHIR base, e.g. http://localhost:8095/fhir</param>
    public CqfRulerCqlEngine(HttpClient http, string baseUrl)
        => (_http, _baseUrl) = (http, baseUrl.TrimEnd('/'));

    public IReadOnlyDictionary<string, object?> Evaluate(string ruleId, Bundle bundle)
    {
        var patient = bundle.Entry
            .Select(e => e.Resource)
            .OfType<Patient>()
            .FirstOrDefault()
            ?? throw new NotSupportedException("Bundle has no Patient to use as CQL subject.");

        // $evaluate against the Bundle we pass (useServerData=false → don't touch the server store).
        var input = new Parameters();
        input.Add("subject", new FhirString($"Patient/{patient.Id}"));
        input.Add("useServerData", new FhirBoolean(false));
        input.Parameter.Add(new Parameters.ParameterComponent { Name = "data", Resource = bundle });

        var body = new StringContent(Serializer.SerializeToString(input), Encoding.UTF8, "application/fhir+json");
        var resp = _http.PostAsync($"{_baseUrl}/Library/{ruleId}/$evaluate", body)
                        .GetAwaiter().GetResult();
        var text = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var outcome = Parser.Parse<Parameters>(text);

        var results = new Dictionary<string, object?>();
        foreach (var p in outcome.Parameter)
        {
            // Fail loud: the server returns an OperationOutcome named "evaluation error" on failure.
            if (p.Resource is OperationOutcome oo)
                throw new NotSupportedException(
                    "CQL server evaluation error: " +
                    string.Join("; ", oo.Issue.Select(i => i.Details?.Text ?? i.Diagnostics)));

            results[p.Name] = p.Value switch
            {
                FhirBoolean b => b.Value,
                FhirString s => s.Value,
                Integer i => i.Value,
                FhirDecimal d => d.Value,
                Date dt => dt.Value,
                FhirDateTime fdt => fdt.Value,
                _ => p.Value?.ToString(),
            };
        }
        return results;
    }
}
