using Hl7.Fhir.Model;

namespace NhiPasFhir;

/// <summary>One processing issue in an OperationOutcome-twpas. DetailsCode ∈ operation-outcome CodeSystem
/// (e.g. "MSG_PARAM_INVALID").</summary>
public sealed record OutcomeIssue(string Severity, string Code, string DetailsCode);

/// <summary>維度2 (error path) — builds OperationOutcome-twpas, the error report the platform returns
/// when a submission cannot be processed. Values from the caller.</summary>
public static class OutcomeBuilder
{
    public static OperationOutcome Build(params OutcomeIssue[] issues)
    {
        var oo = new OperationOutcome { Id = "error", Meta = new Meta { Profile = new[] { $"{Sys.Sd}/Operationoutcome-twpas" } } };
        foreach (var it in issues)
            oo.Issue.Add(new OperationOutcome.IssueComponent
            {
                Severity = Enum.Parse<OperationOutcome.IssueSeverity>(it.Severity, true),
                Code = Enum.Parse<OperationOutcome.IssueType>(it.Code, true),
                Details = new CodeableConcept(Sys.OperationOutcomeCs, it.DetailsCode),
            });
        return oo;
    }
}
