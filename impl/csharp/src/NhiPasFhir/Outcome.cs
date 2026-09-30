using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;

namespace NhiPasFhir;

// IG 對照 (spec/docs/IG-TRACEABILITY.md): Profile Operationoutcome-twpas → 範例 OperationOutcome-error-example

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
                // Parse by FHIR EnumLiteral (handles hyphenated codes like not-found/business-rule),
                // not by .NET enum member name which would throw on the hyphen.
                Severity = EnumUtility.ParseLiteral<OperationOutcome.IssueSeverity>(it.Severity.ToLowerInvariant())
                    ?? throw new ArgumentException($"Unknown issue severity: {it.Severity}"),
                Code = EnumUtility.ParseLiteral<OperationOutcome.IssueType>(it.Code.ToLowerInvariant())
                    ?? throw new ArgumentException($"Unknown issue type: {it.Code}"),
                Details = new CodeableConcept(Sys.OperationOutcomeCs, it.DetailsCode),
            });
        return oo;
    }
}
