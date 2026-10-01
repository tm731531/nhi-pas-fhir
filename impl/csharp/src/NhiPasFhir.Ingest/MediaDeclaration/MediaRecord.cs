namespace NhiPasFhir.MediaDeclaration;

/// <summary>A parsed NHI medical-expense media-declaration record — the 3-segment shape
/// 總表段(t) / 點數清單段(d) / 醫令清單段(p) from the spec (版更 112.08.25). Fields are keyed by the
/// official field IDs (t1, d3, p4, …). One d-case here (門診 one visit) with its p order-lines; a real
/// file has one t and many d/p groups. See spec/docs/media-declaration-to-fhir.md.</summary>
public sealed class MediaRecord
{
    /// <summary>總表段 — batch header (t1 資料格式, t2 服務機構代號, t3 費用年月, t6 申報日期, …).</summary>
    public Dictionary<string, string> Summary { get; } = new();

    /// <summary>點數清單段 — one 就醫案件 (d3 身分證, d9 就醫日期, d11 生日, d19 主診斷, d30 醫師, …).</summary>
    public Dictionary<string, string> Case { get; } = new();

    /// <summary>醫令清單段 — order lines (p3 醫令類別, p4 項目代號, p10 總量, p12 點數, …).</summary>
    public List<Dictionary<string, string>> Orders { get; } = new();

    public string? T(string id) => Summary.TryGetValue(id, out var v) ? v : null;
    public string? D(string id) => Case.TryGetValue(id, out var v) ? v : null;
}
