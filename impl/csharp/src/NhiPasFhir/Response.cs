using Hl7.Fhir.Model;

namespace NhiPasFhir;

/// <summary>One adjudicated item of an NHI response. ApproveCode ∈ nhi-approve-comment:
/// 0 審核中 · 1 同意 · 2 不予同意 · 3 部份同意 · 4 補件 · 5 退件 · 6 不予同意(對應手術亦不支付) · 7 改核.</summary>
public sealed record ResponseItem(int ItemSequence, string ApproveCode, int ApprovedValue = 0);

/// <summary>The NHI's decision on a submitted Claim (核准/駁回/補件…). References resolve to the
/// original request bundle. Neutral, non-FHIR.</summary>
public sealed record ResponseCase(
    string ResponseId,     // NHI 核定文號 (ClaimResponse.identifier)
    string PatientRef,     // e.g. "Patient/pat-1"
    string HospitalRef,    // e.g. "Organization/org-hosp"
    string ClaimRef,       // the Claim being answered, e.g. "Claim/cla-1"
    string Created,
    string Disposition,    // 審畢結果摘要
    IReadOnlyList<ResponseItem> Items);

/// <summary>維度 2 — builds Bundle-response-twpas carrying a ClaimResponse-twpas. Values from the caller;
/// no fabricated FHIR fields. Proven by the official validator like every other output.</summary>
public static class ResponseBuilder
{
    public static Bundle Build(ResponseCase r)
    {
        var cr = new ClaimResponse
        {
            Id = "claim-response",
            Meta = new Meta { Profile = new[] { $"{Sys.Sd}/ClaimResponse-twpas" } },
            Identifier = { new Identifier { Value = r.ResponseId } },
            Status = FinancialResourceStatusCodes.Active,
            Type = new CodeableConcept(Sys.ClaimType, "institutional"),
            Use = ClaimUseCode.Preauthorization,
            Patient = new ResourceReference(r.PatientRef),
            Created = r.Created,
            Insurer = new ResourceReference("Organization/org-nhi"),
            Requestor = new ResourceReference(r.HospitalRef),
            Request = new ResourceReference(r.ClaimRef),
            Outcome = ClaimProcessingCodes.Complete,
            Disposition = r.Disposition,
        };
        foreach (var it in r.Items)
            cr.Item.Add(new ClaimResponse.ItemComponent
            {
                ItemSequence = it.ItemSequence,
                Adjudication = { new ClaimResponse.AdjudicationComponent
                    { Category = new CodeableConcept(Sys.Adjudication, "submitted"), Reason = new CodeableConcept(Sys.CsApproveComment, it.ApproveCode) } },
                Detail = { new ClaimResponse.ItemDetailComponent
                    { DetailSequence = 1, Adjudication = { new ClaimResponse.AdjudicationComponent
                        { Category = new CodeableConcept(Sys.Adjudication, "submitted"), Reason = new CodeableConcept(Sys.CsApproveComment, it.ApproveCode), Value = it.ApprovedValue } } } },
            });

        var bundle = new Bundle
        {
            Id = "bun-response",
            Meta = new Meta { Profile = new[] { $"{Sys.Sd}/Bundle-response-twpas" } },
            Type = Bundle.BundleType.Searchset,
        };
        bundle.Entry.Add(new Bundle.EntryComponent { FullUrl = $"{Sys.PasBase}/ClaimResponse/{cr.Id}", Resource = cr });
        return bundle;
    }
}
