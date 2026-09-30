namespace NhiPasFhir.Ltc;

// 長照 (Long-Term Care) domain — NON-FHIR. Taiwan LTC has no published FHIR IG; it runs on structured
// assessment scales (量表). This models the standard instruments faithfully. See spec/docs/ltc-model.md.

/// <summary>Barthel Index (巴氏量表) — the standard 10-item ADL (日常生活活動) instrument, total 0-100,
/// used across Taiwan 長照 / 外籍看護 / 長照險. Item scores are transcribed from the standard instrument;
/// each item only accepts its defined values (fail-loud on anything else, so a fabricated score can't
/// slip through). Higher = more independent.</summary>
public sealed record BarthelIndex(
    int Feeding,     // 進食            {0,5,10}
    int Transfer,    // 移位 (床↔椅)    {0,5,10,15}
    int Grooming,    // 個人衛生        {0,5}
    int Toileting,   // 如廁            {0,5,10}
    int Bathing,     // 洗澡            {0,5}
    int Mobility,    // 平地行走 50m    {0,5,10,15}
    int Stairs,      // 上下樓梯        {0,5,10}
    int Dressing,    // 穿脫衣褲鞋襪    {0,5,10}
    int Bowels,      // 大便控制        {0,5,10}
    int Bladder)     // 小便控制        {0,5,10}
{
    private static readonly int[] S05 = { 0, 5 };
    private static readonly int[] S0510 = { 0, 5, 10 };
    private static readonly int[] S051015 = { 0, 5, 10, 15 };

    /// <summary>Validate every item against its allowed set. Throws on an out-of-range (fabricated) score.</summary>
    public BarthelIndex Validate()
    {
        Check(nameof(Feeding), Feeding, S0510); Check(nameof(Transfer), Transfer, S051015);
        Check(nameof(Grooming), Grooming, S05); Check(nameof(Toileting), Toileting, S0510);
        Check(nameof(Bathing), Bathing, S05); Check(nameof(Mobility), Mobility, S051015);
        Check(nameof(Stairs), Stairs, S0510); Check(nameof(Dressing), Dressing, S0510);
        Check(nameof(Bowels), Bowels, S0510); Check(nameof(Bladder), Bladder, S0510);
        return this;
    }

    private static void Check(string item, int value, int[] allowed)
    {
        if (Array.IndexOf(allowed, value) < 0)
            throw new ArgumentOutOfRangeException(item, value, $"Barthel item {item} must be one of {{{string.Join(",", allowed)}}}");
    }

    /// <summary>Total 0-100 (validated).</summary>
    public int Total => Validate() is var _ ? Feeding + Transfer + Grooming + Toileting + Bathing
                                             + Mobility + Stairs + Dressing + Bowels + Bladder : 0;

    /// <summary>Standard Barthel dependency band from the total.</summary>
    public DependencyBand Band => Total switch
    {
        <= 20 => DependencyBand.Total,        // 完全依賴
        <= 60 => DependencyBand.Severe,       // 嚴重依賴
        <= 90 => DependencyBand.Moderate,     // 中度依賴
        < 100 => DependencyBand.Mild,         // 輕度依賴
        _ => DependencyBand.Independent,       // 完全獨立 (100)
    };
}

/// <summary>Standard Barthel dependency bands (完全依賴 / 嚴重 / 中度 / 輕度 / 獨立). NOT the 長照 失能等級
/// (1-8) — that is derived by the official CMS 照顧管理評估量表, which uses more than ADL (see LtcBenefit).</summary>
public enum DependencyBand { Total, Severe, Moderate, Mild, Independent }
