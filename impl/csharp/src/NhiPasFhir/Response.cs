using Hl7.Fhir.Model;

namespace NhiPasFhir;

// IG 對照 (spec/docs/IG-TRACEABILITY.md): 規範文件邏輯模型 ResponseModel → 範例 Bundle-bun-response / ClaimResponse-twpas

/// <summary>One adjudicated item of an NHI response. ApproveCode ∈ nhi-approve-comment:
/// 0 審核中 · 1 同意 · 2 不予同意 · 3 部份同意 · 4 補件 · 5 退件 · 6 不予同意(對應手術亦不支付) · 7 改核.</summary>
public sealed record ResponseItem(int ItemSequence, string ApproveCode, int ApprovedValue = 0);

/// <summary>The NHI's decision on a submitted Claim (核准/駁回/補件…). References resolve to the
/// original request bundle. Neutral, non-FHIR.
/// Cardinality per ClaimResponse-twpas: exactly ONE claim-level item (its adjudication carries the
/// overall 審核 code), whose <c>detail[]</c> holds one entry per medical order — so <see cref="Items"/>
/// are the per-order details, and <see cref="OverallApproveCode"/> is the claim-level decision.</summary>
public sealed record ResponseCase(
    string ResponseId,        // NHI 核定文號 (ClaimResponse.identifier)
    string PatientRef,        // e.g. "Patient/pat-1"
    string HospitalRef,       // e.g. "Organization/org-hosp"
    string ClaimRef,          // the Claim being answered, e.g. "Claim/cla-1"
    string Created,
    string Disposition,       // 審畢結果摘要
    string OverallApproveCode, // claim-level nhi-approve-comment (goes on item.adjudication, no value)
    IReadOnlyList<ResponseItem> Items); // one per medical order → item.detail[]

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
        // ClaimResponse-twpas: exactly ONE item (min:1 max:1) whose adjudication is the claim-level
        // decision (reason only, no value); each medical order is an item.detail[] (min:1 max:*) with
        // its own adjudication (reason + value). Matches ClaimResponse-claim-response-example exactly.
        var item = new ClaimResponse.ItemComponent
        {
            ItemSequence = 1,
            Adjudication = { new ClaimResponse.AdjudicationComponent
                { Category = new CodeableConcept(Sys.Adjudication, "submitted"), Reason = new CodeableConcept(Sys.CsApproveComment, r.OverallApproveCode) } },
        };
        foreach (var it in r.Items)
            item.Detail.Add(new ClaimResponse.ItemDetailComponent
            {
                DetailSequence = it.ItemSequence,
                Adjudication = { new ClaimResponse.AdjudicationComponent
                    { Category = new CodeableConcept(Sys.Adjudication, "submitted"), Reason = new CodeableConcept(Sys.CsApproveComment, it.ApproveCode), Value = it.ApprovedValue } },
            });
        cr.Item.Add(item);

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
