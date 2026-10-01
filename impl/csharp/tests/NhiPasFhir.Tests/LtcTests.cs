using System;
using NhiPasFhir.Ltc;
using Xunit;

// 長照 (non-FHIR) assessment model — standard instruments (Barthel ADL / Lawton IADL) + published benefit table.
public class LtcTests
{
    [Fact] public void Barthel_totals_and_bands()
    {
        var independent = new BarthelIndex(10, 15, 5, 10, 5, 15, 10, 10, 10, 10);
        Assert.Equal(100, independent.Total);
        Assert.Equal(DependencyBand.Independent, independent.Band);

        var totalDependence = new BarthelIndex(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        Assert.Equal(0, totalDependence.Total);
        Assert.Equal(DependencyBand.Total, totalDependence.Band);

        var moderate = new BarthelIndex(10, 10, 5, 10, 0, 10, 5, 10, 10, 10); // 80
        Assert.Equal(80, moderate.Total);
        Assert.Equal(DependencyBand.Moderate, moderate.Band);
    }

    [Fact] public void Barthel_rejects_out_of_range_score()
    {
        // 7 is not an allowed Feeding score {0,5,10} — must fail loud, not silently accept.
        Assert.Throws<ArgumentOutOfRangeException>(() => new BarthelIndex(7, 15, 5, 10, 5, 15, 10, 10, 10, 10).Validate());
    }

    [Fact] public void Iadl_counts_independent_activities()
    {
        var iadl = new Iadl(Telephone: true, Shopping: true, Cooking: false, Housekeeping: true,
                            Laundry: false, Transport: true, Medication: true, Finances: false);
        Assert.Equal(5, iadl.Total);
    }

    [Fact] public void LtcBenefit_ceilings_are_the_published_amounts()
    {
        Assert.Equal(10_020, LtcBenefit.CareCeiling(2));
        Assert.Equal(36_180, LtcBenefit.CareCeiling(8));
        Assert.False(LtcBenefit.IsEligible(1));   // 僅輕微衰弱 — not eligible
        Assert.Null(LtcBenefit.CareCeiling(1));
        Assert.Null(LtcBenefit.CareCeiling(9));   // out of range
    }

    [Fact] public void LtcBenefit_four_packages_published_amounts()
    {
        Assert.Equal(1_680, LtcBenefit.TransportMonthlyByRegionTier[1]);   // 交通接送 by region tier
        Assert.Equal(2_400, LtcBenefit.TransportMonthlyByRegionTier[4]);
        Assert.Equal(40_000, LtcBenefit.AssistiveDeviceCeilingPer3Years);  // 輔具/居家無障礙 per 3 yrs
        Assert.Equal(32_340, LtcBenefit.RespiteYearlyCeiling(6));          // 喘息 2-6 級
        Assert.Equal(48_510, LtcBenefit.RespiteYearlyCeiling(8));          // 喘息 7-8 級
        Assert.Null(LtcBenefit.RespiteYearlyCeiling(1));                   // level 1 not eligible
    }

    [Fact] public void LtcBenefit_30_additions()
    {
        Assert.Equal(60_000, LtcBenefit.SmartDeviceRentalCeilingPer3Years);   // 3.0 智慧科技輔具租賃 (3yr, amount secondary)
        Assert.Equal(180_000, LtcBenefit.ResidentialInstitutionYearly);        // 3.0 住宿式 18萬/年 (primary-confirmed, 1966)
        Assert.Equal(15_000, LtcBenefit.ResidentialInstitutionMonthlyMax);     // 月上限 15,000
    }

    [Fact] public void LtcBenefit_copay_by_payer_and_package()
    {
        // 低收入戶 = 0 across the board
        Assert.Equal(0m, LtcBenefit.CopayRate(LtcBenefit.Package.Transport, LtcBenefit.Payer.LowIncome));
        // 一般戶: 照顧/喘息 16%, 交通/輔具 30%
        Assert.Equal(0.16m, LtcBenefit.CopayRate(LtcBenefit.Package.CareAndProfessional, LtcBenefit.Payer.General));
        Assert.Equal(0.30m, LtcBenefit.CopayRate(LtcBenefit.Package.AssistiveDeviceAndHomeMods, LtcBenefit.Payer.General));
        // 中低收: 照顧 5%
        Assert.Equal(0.05m, LtcBenefit.CopayRate(LtcBenefit.Package.CareAndProfessional, LtcBenefit.Payer.MidLowIncome));
        // self-pay = amount × share
        Assert.Equal(1_603m, LtcBenefit.SelfPay(10_020m, LtcBenefit.Package.CareAndProfessional, LtcBenefit.Payer.General));
    }
}
