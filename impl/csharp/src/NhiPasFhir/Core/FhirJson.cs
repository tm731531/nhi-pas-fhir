using System.Text.Json;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace NhiPasFhir.Core;

/// <summary>FHIR JSON (de)serialization for internal transport (engine requests, submit, round-trips).
/// Uses the current System.Text.Json-based Firely API (<c>.ForFhir()</c>) instead of the obsolete
/// <c>FhirJsonParser</c>/<c>FhirJsonSerializer</c>. NOTE: this is NOT the golden-output path — the
/// byte-for-byte reference JSON is produced by <c>NhiPas.ToJson</c>; this helper is only for wire I/O.</summary>
public static class FhirJson
{
    private static readonly JsonSerializerOptions Options =
        new JsonSerializerOptions().ForFhir(ModelInfo.ModelInspector);

    public static string Serialize(Resource resource) => JsonSerializer.Serialize(resource, Options);

    public static T Parse<T>(string json) where T : Resource
        => (T)JsonSerializer.Deserialize<Resource>(json, Options)!;
}
