namespace NhiPasFhir.Core;

/// <summary>ROC (民國) date conversion — a generic util (西元 = 民國 + 1911), shared by the
/// media-declaration ingest and the 健保卡 card reader. Zero-padded 民國: 3-digit year + 2-digit month
/// (+ 2-digit day) per the media-declaration spec (版更 112.08.25); the 健保卡 basic segment uses the same
/// YYYMMDD form. See spec/docs/media-declaration-to-fhir.md §6.</summary>
public static class RocDate
{
    /// <summary>ROC "YYYMMDD" or "YYYMM" -> ISO ("2010-05-01" / "2010-05"). null if not parseable.</summary>
    public static string? ToIso(string? roc)
    {
        if (string.IsNullOrWhiteSpace(roc) || !roc.All(char.IsDigit) || roc.Length < 5) return null;
        var year = int.Parse(roc[..3]) + 1911;
        if (roc.Length >= 7) return $"{year:D4}-{roc[3..5]}-{roc[5..7]}";
        return $"{year:D4}-{roc[3..5]}";
    }

    /// <summary>ISO ("2010-05-01" / "2010-05") -> ROC "YYYMMDD" / "YYYMM". null if not parseable.</summary>
    public static string? ToRoc(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return null;
        var parts = iso.Split('-');
        if (parts.Length < 2 || !int.TryParse(parts[0], out var year)) return null;
        var roc = year - 1911;
        var head = $"{roc:D3}{parts[1]}";
        return parts.Length >= 3 ? $"{head}{parts[2]}" : head;
    }
}
