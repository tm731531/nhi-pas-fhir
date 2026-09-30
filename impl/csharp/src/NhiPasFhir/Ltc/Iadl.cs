namespace NhiPasFhir.Ltc;

/// <summary>IADL — Lawton–Brody Instrumental Activities of Daily Living (工具性日常生活活動量表), the standard
/// 8-item instrument used alongside ADL in Taiwan 長照 assessment. Each item is able (1) / unable (0);
/// total 0-8 (higher = more independent). This models the standard items; how IADL feeds the 失能等級 is
/// part of the official CMS tool (see <see cref="LtcBenefit"/>), not re-derived here.</summary>
public sealed record Iadl(
    bool Telephone,   // 使用電話
    bool Shopping,    // 上街購物
    bool Cooking,     // 食物烹調
    bool Housekeeping,// 家務維持
    bool Laundry,     // 洗衣服
    bool Transport,   // 外出交通
    bool Medication,  // 服用藥物
    bool Finances)    // 處理財務
{
    /// <summary>Number of IADL activities the person can do independently (0-8).</summary>
    public int Total => new[] { Telephone, Shopping, Cooking, Housekeeping, Laundry, Transport, Medication, Finances }
        .Count(able => able);
}
