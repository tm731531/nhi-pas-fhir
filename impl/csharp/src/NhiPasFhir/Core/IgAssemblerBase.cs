using Hl7.Fhir.Model;
using NhiPasFhir.Core;

namespace NhiPasFhir.Core;

/// <summary>THE single base for every FHIR IG assembler — because there is one standard: a set of
/// resources, each conforming to a profile under the IG's canonical, wrapped in a Bundle whose entries
/// are fullUrl'd by that canonical. It factors out exactly that: the canonical (<see cref="CanonicalBase"/>),
/// the profile Meta (<see cref="P"/>), and the entry-wrapping loop (<see cref="WrapBundle"/>). Every
/// assembler extends it — the sibling IGs (重大傷病/電子處方箋/NGS/傳染病/EMR) directly, and the pas family
/// via <see cref="AbstractCaseAssembler"/>, which specialises this with a Claim-centric Template Method.
/// Generic FHIR datatype construction (CodeableConcept / Coding / reference) is NOT here — that is universal
/// and composed from <see cref="FhirBuild"/> (<c>using static NhiPasFhir.Core.FhirBuild;</c>), so a change
/// to it never ripples through this hierarchy. Covered directly by FhirBuildTests (WrapBundle) + the goldens.</summary>
public abstract class IgAssemblerBase : ICaseAssembler
{
    public abstract string Ig { get; }
    public abstract string CaseType { get; }

    /// <summary>The IG canonical, e.g. https://nhicore.nhi.gov.tw/ci. Drives P() and the entry fullUrls.</summary>
    protected abstract string CanonicalBase { get; }

    protected string Sd => $"{CanonicalBase}/StructureDefinition";

    /// <summary>Meta with this IG's profile canonical for <paramref name="name"/>.</summary>
    protected Meta P(string name) => new() { Profile = new[] { $"{Sd}/{name}" } };

    /// <summary>Wrap the ordered resources into a Bundle: id + profile Meta + type (+ optional identifier /
    /// timestamp), each entry keyed by fullUrl = {CanonicalBase}/{Type}/{id}. Same shape every IG used.</summary>
    protected Bundle WrapBundle(string id, string bundleProfile, Bundle.BundleType type,
        IEnumerable<Resource> ordered, Identifier? identifier = null, DateTimeOffset? timestamp = null)
    {
        var bundle = new Bundle { Id = id, Meta = P(bundleProfile), Type = type };
        if (identifier != null) bundle.Identifier = identifier;
        if (timestamp is { } ts) bundle.Timestamp = ts;
        foreach (var r in ordered)
            bundle.Entry.Add(new Bundle.EntryComponent { FullUrl = $"{CanonicalBase}/{r.TypeName}/{r.Id}", Resource = r });
        return bundle;
    }

    public abstract Bundle Assemble(PACase c);
}
