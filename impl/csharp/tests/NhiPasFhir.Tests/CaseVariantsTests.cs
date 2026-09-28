using System.Linq;
using Hl7.Fhir.Model;
using NhiPasFhir;
using Xunit;

/// <summary>維度1 (申報別×案件別) + 維度2 (核定回應) — each validated to 0 errors; here we lock the shape.</summary>
public class CaseVariantsTests
{
    private static Claim ClaimOf(Bundle b) => b.Entry.Select(e => e.Resource).OfType<Claim>().Single();

    [Fact] public void Appeal_sets_subType_and_carries_original_acceptance_number()
    {
        var claim = ClaimOf(NhiPas.Build(Samples.AppealCase()));
        Assert.Equal("3", claim.SubType.Coding[0].Code);                              // 申復
        Assert.Contains(claim.Identifier, i => i.Value == "202405301000002");        // old_acpt_no (invariant applType)
    }

    [Fact] public void SelfAssessment_sets_priority_and_embeds_self_assessment_response()
    {
        var b = NhiPas.Build(Samples.SelfAssessmentCase());
        Assert.Equal("3", ClaimOf(b).Priority.Coding[0].Code);                        // 自主審查
        var cr = b.Entry.Select(e => e.Resource).OfType<ClaimResponse>().Single();
        Assert.Equal("ClaimResponse-self-assessment-twpas", cr.Meta.Profile.Single().Split('/').Last());
        Assert.NotEmpty(cr.Extension.Where(x => x.Url.EndsWith("extension-claimResponse-requestor")));
    }

    [Fact] public void Response_builds_searchset_bundle_with_decision()
    {
        var b = NhiPas.BuildResponse(Samples.ResponseCase());
        Assert.Equal(Bundle.BundleType.Searchset, b.Type);                            // fixed by Bundle-response-twpas
        var cr = b.Entry.Select(e => e.Resource).OfType<ClaimResponse>().Single();
        Assert.Equal("ClaimResponse-twpas", cr.Meta.Profile.Single().Split('/').Last());
        Assert.Equal("1", cr.Item[0].Adjudication[0].Reason.Coding[0].Code);         // 1 = 同意
        Assert.NotEmpty(cr.Item[0].Detail);                                          // detail min=1
    }
}
