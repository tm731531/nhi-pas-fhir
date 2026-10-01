namespace NhiPasFhir.Ltc;

/// <summary>長照 2.0 給付 (Long-Term Care benefits) — the 四包錢 (four packages) + 部分負擔 (copay).
///
/// 失能等級 (disability level) 1-8 is assigned by the county 長照管理中心 using the official **CMS
/// 照顧管理評估量表**, which weighs eight dimensions (ADL / IADL / 認知 / 行為精神 / 特殊照護需求 / 社會支持
/// / 照顧者負荷 / 居家環境) — NOT ADL alone. So this library does NOT compute the level; passing it a level,
/// it returns the published amounts. Level 1 (無失能/僅衰弱) is not eligible.
///
/// AMOUNTS are the published 長照 給付額度 as widely cited (≈2026). They are public, non-PHI reference data,
/// but sourced from secondary summaries — **reconfirm against the official 1966 / 衛福部 給付額度表 before
/// production use**. Encoded here so the toolbox models the real benefit structure, honestly flagged.</summary>
public static class LtcBenefit
{
    /// <summary>四包錢 — the four LTC 2.0 benefit packages.</summary>
    public enum Package
    {
        CareAndProfessional,          // 照顧及專業服務
        Transport,                    // 交通接送
        AssistiveDeviceAndHomeMods,   // 輔具及居家無障礙環境改善
        Respite,                      // 喘息服務
    }

    /// <summary>身分別 — determines the copay (部分負擔) share.</summary>
    public enum Payer { General, MidLowIncome, LowIncome }   // 一般戶 / 中低收入戶 / 低收入戶

    // --- 1. 照顧及專業服務:月給付上限 by 失能等級 (2-8); level 1 not eligible ---
    public static readonly IReadOnlyDictionary<int, int> CareAndProfessionalMonthlyCeiling =
        new Dictionary<int, int>
        {
            [2] = 10_020, [3] = 15_460, [4] = 18_580, [5] = 24_100,
            [6] = 28_070, [7] = 32_090, [8] = 36_180,
        };

    // --- 2. 交通接送:月額度 by 地區類別 (1-4);僅第 4 級以上適用 ---
    public static readonly IReadOnlyDictionary<int, int> TransportMonthlyByRegionTier =
        new Dictionary<int, int> { [1] = 1_680, [2] = 1_840, [3] = 2_000, [4] = 2_400 };

    // --- 3. 輔具及居家無障礙環境改善:每 3 年上限 ---
    public const int AssistiveDeviceCeilingPer3Years = 40_000;

    // --- 4. 喘息服務:年額度 by 失能等級 band ---
    public static int? RespiteYearlyCeiling(int disabilityLevel) => disabilityLevel switch
    {
        >= 2 and <= 6 => 32_340,
        7 or 8 => 48_510,
        _ => null,   // level 1 not eligible
    };

    // --- 部分負擔 (copay share) by package × 身分別. 低收=0 always. ---
    public static decimal CopayRate(Package package, Payer payer) => (package, payer) switch
    {
        (_, Payer.LowIncome) => 0m,
        (Package.CareAndProfessional, Payer.General) => 0.16m,
        (Package.CareAndProfessional, Payer.MidLowIncome) => 0.05m,
        (Package.Respite, Payer.General) => 0.16m,
        (Package.Respite, Payer.MidLowIncome) => 0.05m,
        (Package.Transport, Payer.General) => 0.30m,
        (Package.Transport, Payer.MidLowIncome) => 0.10m,
        (Package.AssistiveDeviceAndHomeMods, Payer.General) => 0.30m,
        (Package.AssistiveDeviceAndHomeMods, Payer.MidLowIncome) => 0.10m,
        _ => 0m,
    };

    /// <summary>Is a 失能等級 eligible for LTC 2.0 benefits? (Level 1 is not.)</summary>
    public static bool IsEligible(int disabilityLevel) => CareAndProfessionalMonthlyCeiling.ContainsKey(disabilityLevel);

    /// <summary>照顧及專業服務 monthly ceiling for a level, or null if not eligible / out of range.</summary>
    public static int? CareCeiling(int disabilityLevel)
        => CareAndProfessionalMonthlyCeiling.TryGetValue(disabilityLevel, out var v) ? v : null;

    /// <summary>Out-of-pocket for a given billed amount under a package + payer (amount × copay share).</summary>
    public static decimal SelfPay(decimal amount, Package package, Payer payer)
        => decimal.Round(amount * CopayRate(package, payer), 0, System.MidpointRounding.AwayFromZero);
}
