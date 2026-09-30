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
}
