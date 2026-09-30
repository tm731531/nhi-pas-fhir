namespace NhiPasFhir.Ltc;

/// <summary>長照 2.0 給付 (Long-Term Care benefits). 失能等級 (disability level) 1-8 is assigned by the
/// county 長照管理中心 using the official **CMS 照顧管理評估量表** — which weighs far more than ADL (IADL,
/// cognition, behaviour, caregiver load…). This library does NOT compute the level from ADL alone, because
/// that mapping is the official tool's, not ours (guessing it would be fabrication). What is transcribed
/// here is authoritative published data: the 照顧及專業服務 monthly ceiling per level, and the 四包錢 set.</summary>
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

    /// <summary>失能等級 → 照顧及專業服務 monthly ceiling (NT$). Level 1 = 僅輕微衰弱, not eligible (no entry).
    /// Amounts are the published 長照 給付額度 (照顧及專業服務). Levels 2-8.</summary>
    public static readonly IReadOnlyDictionary<int, int> CareAndProfessionalMonthlyCeiling =
        new Dictionary<int, int>
        {
            [2] = 10_020, [3] = 15_460, [4] = 18_580, [5] = 24_100,
            [6] = 28_070, [7] = 32_090, [8] = 36_180,
        };

    /// <summary>Is a 失能等級 eligible for LTC 2.0 benefits? (Level 1 is not.)</summary>
    public static bool IsEligible(int disabilityLevel) => CareAndProfessionalMonthlyCeiling.ContainsKey(disabilityLevel);

    /// <summary>照顧及專業服務 monthly ceiling for a level, or null if not eligible / out of range.</summary>
    public static int? CareCeiling(int disabilityLevel)
        => CareAndProfessionalMonthlyCeiling.TryGetValue(disabilityLevel, out var v) ? v : null;
}
