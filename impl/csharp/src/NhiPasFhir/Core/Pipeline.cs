using Hl7.Fhir.Model;

namespace NhiPasFhir.Core;

public sealed record PipelineResult(
    List<Finding> Findings, bool Blocked, Bundle? Bundle, string Advisory, CqlFinding? Cql = null);

/// <summary>Framework flow: (seed) PRE-CHECK → ASSEMBLE → optional CQL self-check (on the Bundle).
/// The seed drug↔indication check runs on the neutral PACase BEFORE assembly. The official CQL check runs
/// AFTER assembly because it evaluates the Bundle itself (context Patient). CQL is opt-in: pass an
/// <see cref="ICqlPreCheck"/> to enable it; omit it (default) to run without CQL. Advisory only.</summary>
public static class Pipeline
{
    private const string AdvisoryText =
        "Pre-check is decision-support only; it does not guarantee reimbursement. " +
        "Final responsibility rests with the clinician and NHI adjudication.";

    public static PipelineResult Run(PACase c, bool stopOnBlock = true, ICqlPreCheck? cql = null)
    {
        cql ??= NoCqlPreCheck.Instance;

        // ① seed pre-check (drug↔indication) — on PACase, before assembly.
        var pairs = new List<(string, string)>();
        if (c.Data.TryGetValue("drug_code", out var d) && c.Data.TryGetValue("indication", out var i))
            pairs.Add(((string)d, (string)i));
        var findings = PreCheck.Check(pairs);
        if (PreCheck.HasBlocking(findings) && stopOnBlock)
            return new PipelineResult(findings, true, null, AdvisoryText);

        // ② assemble the Bundle.
        var bundle = AssemblerFactory.ForCase(c).Assemble(c);

        // ③ CQL self-check — only when enabled — on the assembled Bundle.
        CqlFinding? cqlResult = null;
        if (cql.Enabled && c.Data.TryGetValue("drug_code", out var drug))
            cqlResult = cql.Evaluate(bundle, (string)drug);

        var blocked = PreCheck.HasBlocking(findings)
            || cqlResult?.Outcome is CqlOutcome.WouldBeRejected or CqlOutcome.DataMissing;
        return new PipelineResult(findings, blocked, bundle, AdvisoryText, cqlResult);
    }
}
