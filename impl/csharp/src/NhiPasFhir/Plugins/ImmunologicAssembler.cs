using System.Runtime.CompilerServices;
using Hl7.Fhir.Model;
using NhiPasFhir.Core;

namespace NhiPasFhir.Plugins;

/// <summary>免疫製劑事前審查 (registered; body is a large scoped follow-up).
/// Proves extensibility: a case = one subclass + register. But Bundle-immunologic-agent-twpas mandates
/// ~35 resource slices (SOAP note + blood group + allergy + evidence + self-assessment ClaimResponse),
/// so the body is modelled from the IG one resource at a time (like 癌藥 45→0). Fails loud until then.</summary>
public sealed class ImmunologicAssembler : AbstractCaseAssembler
{
    public const string IgId = "tw.gov.mohw.nhi.pas#1.2.6";
    public const string Case = "immunologic-agent";
    public override string Ig => IgId;
    public override string CaseType => Case;

    [ModuleInitializer]
    internal static void Register() => AssemblerFactory.Register(IgId, Case, () => new ImmunologicAssembler());

    protected override CaseParts BuildCase(PACase c, Patient patient, Practitioner doctor, Organization hospital)
        => throw new NotImplementedException(
            "immunologic-agent requires ~35 resources per Bundle-immunologic-agent-twpas " +
            "(SOAP note + blood group + allergy + evidence + self-assessment ClaimResponse). " +
            "Scoped follow-up — model from the IG one resource at a time, validate each. See spec/docs/CASE-CATALOG.md.");
}
