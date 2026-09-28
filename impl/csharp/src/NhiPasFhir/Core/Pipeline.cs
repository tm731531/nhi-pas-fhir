using Hl7.Fhir.Model;

namespace NhiPasFhir.Core;

public sealed record PipelineResult(List<Finding> Findings, bool Blocked, Bundle? Bundle, string Advisory);

/// <summary>Framework flow: PRE-CHECK (block 核刪) → ASSEMBLE. Advisory only; blocks stop assembly.</summary>
public static class Pipeline
{
    private const string AdvisoryText =
        "Pre-check is decision-support only; it does not guarantee reimbursement. " +
        "Final responsibility rests with the clinician and NHI adjudication.";

    public static PipelineResult Run(PACase c, bool stopOnBlock = true)
    {
        var pairs = new List<(string, string)>();
        if (c.Data.TryGetValue("drug_code", out var d) && c.Data.TryGetValue("indication", out var i))
            pairs.Add(((string)d, (string)i));
        var findings = PreCheck.Check(pairs);
        var blocked = PreCheck.HasBlocking(findings);
        if (blocked && stopOnBlock)
            return new PipelineResult(findings, true, null, AdvisoryText);
        var bundle = AssemblerFactory.ForCase(c).Assemble(c);
        return new PipelineResult(findings, blocked, bundle, AdvisoryText);
    }
}
