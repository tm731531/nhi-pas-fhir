using Hl7.Fhir.Model;
using NhiPasFhir.Core;

namespace NhiPasFhir.Plugins;

/// <summary>Shared base for the sibling-IG assemblers (重大傷病 / 電子處方箋 / NGS / 傳染病 / EMR). These
/// IGs are each their own document/message/collection Bundle under their own canonical, so — unlike the
/// pas-shaped <see cref="AbstractCaseAssembler"/> (Claim-centric) — this base only factors out the four
/// things every one of them repeated: the profile Meta, a relative reference, a CodeableConcept helper,
/// and the entry-wrapping loop (fullUrl = {canonical}/{Type}/{id}). A subclass supplies its
/// <see cref="CanonicalBase"/> and builds its own resources; behaviour is byte-identical to the
/// hand-rolled versions (the golden tests are the safety net).</summary>
public abstract class IgAssemblerBase : ICaseAssembler
{
    public abstract string Ig { get; }
    public abstract string CaseType { get; }

    /// <summary>The IG canonical, e.g. https://nhicore.nhi.gov.tw/ci. Drives Profile() and the entry fullUrls.</summary>
    protected abstract string CanonicalBase { get; }

    protected string Sd => $"{CanonicalBase}/StructureDefinition";

    /// <summary>Meta with this IG's profile canonical for <paramref name="name"/>.</summary>
    protected Meta P(string name) => new() { Profile = new[] { $"{Sd}/{name}" } };

    /// <summary>A relative reference ("Patient/pat-1").</summary>
    protected static ResourceReference R(string typeSlashId) => new(typeSlashId);

    /// <summary>CodeableConcept with a single coding (+ optional display) and optional text.</summary>
    protected static CodeableConcept Cc(string sys, string code, string? display = null, string? text = null)
        => new() { Coding = { new Coding(sys, code) { Display = display } }, Text = text };

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
