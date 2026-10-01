using System.Text;

namespace NhiPasFhir.MediaDeclaration;

/// <summary>Reads/writes the readable segment format used by the samples and the reference converter:
/// one line per segment, <c>&lt;seg&gt;|&lt;id&gt;=&lt;value&gt;|…</c>, keyed by the official field IDs.
/// This is a teaching stand-in — the real filing is fixed-width / XML per the spec — so this parser
/// demonstrates the FIELD MAPPING, not the byte-level layout. See spec/docs/media-declaration-to-fhir.md.</summary>
public static class MediaDeclarationParser
{
    public static MediaRecord Parse(string text)
    {
        var rec = new MediaRecord();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var parts = line.Split('|');
            var seg = parts[0];
            var fields = new Dictionary<string, string>();
            foreach (var p in parts.Skip(1))
            {
                var eq = p.IndexOf('=');
                if (eq > 0) fields[p[..eq]] = p[(eq + 1)..];
            }
            switch (seg)
            {
                case "t": foreach (var kv in fields) rec.Summary[kv.Key] = kv.Value; break;
                case "d": foreach (var kv in fields) rec.Case[kv.Key] = kv.Value; break;
                case "p": rec.Orders.Add(fields); break;
            }
        }
        return rec;
    }

    public static string ParseFile(string path) => File.ReadAllText(path);

    /// <summary>Serialize a record back to the segment text (used by the FHIR -> media reverse path).</summary>
    public static string Write(MediaRecord rec)
    {
        var sb = new StringBuilder();
        WriteSeg(sb, "t", rec.Summary);
        WriteSeg(sb, "d", rec.Case);
        foreach (var order in rec.Orders) WriteSeg(sb, "p", order);
        return sb.ToString();
    }

    private static void WriteSeg(StringBuilder sb, string seg, Dictionary<string, string> fields)
    {
        sb.Append(seg);
        foreach (var kv in fields) sb.Append('|').Append(kv.Key).Append('=').Append(kv.Value);
        sb.Append('\n');
    }
}
