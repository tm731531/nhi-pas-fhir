using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace NhiPasFhir;

// IG 對照 (spec/docs/IG-TRACEABILITY.md): 範例 Bundle-bun-uuid-example — 自主審查以 urn:uuid fullUrl/reference 引用

/// <summary>Rewrites a bundle to the urn:uuid entry-referencing style used by the official
/// Bundle-bun-uuid-example: each entry's fullUrl becomes <c>urn:uuid:X</c> and every internal Type/id
/// reference is rewritten to point at it. UUIDs are deterministic (derived from the resource's Type/id),
/// so the output is stable and golden-testable. Same content/profiles as the Resource/id style — only the
/// referencing convention differs.</summary>
public static class UuidStyle
{
    public static Bundle ToUuidStyle(Bundle bundle)
    {
        var map = new Dictionary<string, string>();
        foreach (var e in bundle.Entry)
        {
            if (e.Resource is null) continue; // defensive: a public API may be handed a sparse bundle
            var key = $"{e.Resource.TypeName}/{e.Resource.Id}";
            map[key] = "urn:uuid:" + DeterministicUuid(key);
        }

        var root = JsonNode.Parse(bundle.ToJson())!.AsObject();
        foreach (var entry in root["entry"]!.AsArray())
        {
            var eo = entry!.AsObject();
            var res = eo["resource"]!.AsObject();
            eo["fullUrl"] = map[$"{res["resourceType"]}/{res["id"]}"];
            RewriteReferences(res, map);
        }
        return Core.FhirJson.Parse<Bundle>(root.ToJsonString());
    }

    private static void RewriteReferences(JsonNode? node, IReadOnlyDictionary<string, string> map)
    {
        switch (node)
        {
            case JsonObject o:
                if (o["reference"] is JsonValue v && v.TryGetValue<string>(out var r) && map.TryGetValue(r, out var uuid))
                    o["reference"] = uuid;
                foreach (var kv in o) RewriteReferences(kv.Value, map);
                break;
            case JsonArray a:
                foreach (var x in a) RewriteReferences(x, map);
                break;
        }
    }

    private static string DeterministicUuid(string key)
        => new Guid(MD5.HashData(Encoding.UTF8.GetBytes(key))).ToString();
}
