using Hl7.Fhir.Model;
using NhiPasFhir.Core;
using Xunit;

// Direct tests for the two shared layers extracted in the refactor, so each is proven ONCE in isolation
// (not only transitively via the per-IG goldens): the Fhir datatype builders, and IgAssemblerBase.WrapBundle.
public class FhirBuildTests
{
    [Fact] public void Cc_builds_codeableconcept_with_coding_display_text()
    {
        var cc = FhirBuild.Cc("sys", "code", "disp", "txt");
        Assert.Equal("sys", cc.Coding[0].System);
        Assert.Equal("code", cc.Coding[0].Code);
        Assert.Equal("disp", cc.Coding[0].Display);
        Assert.Equal("txt", cc.Text);
    }

    [Fact] public void Cc_defaults_display_and_text_to_null()
    {
        var cc = FhirBuild.Cc("sys", "code");
        Assert.Null(cc.Coding[0].Display);
        Assert.Null(cc.Text);
    }

    [Fact] public void Cd_builds_a_single_coding()
    {
        var cd = FhirBuild.Cd("sys", "code", "disp");
        Assert.Equal("sys", cd.System);
        Assert.Equal("code", cd.Code);
        Assert.Equal("disp", cd.Display);
    }

    [Fact] public void R_is_relative_AbsRef_is_absolute()
    {
        Assert.Equal("Patient/p1", FhirBuild.R("Patient/p1").Reference);
        Assert.Equal("https://x.org/Patient/p1", FhirBuild.AbsRef("https://x.org", "Patient/p1").Reference);
    }

    // --- WrapBundle contract (the base's only logic), proven directly via a tiny test subclass. ---
    private sealed class ProbeAssembler : IgAssemblerBase
    {
        public override string Ig => "test"; public override string CaseType => "probe";
        protected override string CanonicalBase => "https://example.org/ig";
        public override Bundle Assemble(PACase c) => throw new System.NotSupportedException();
        public Bundle Wrap(IEnumerable<Resource> ordered, Identifier? id = null, System.DateTimeOffset? ts = null)
            => WrapBundle("b1", "MyBundle", Bundle.BundleType.Collection, ordered, id, ts);
    }

    [Fact] public void WrapBundle_sets_profile_type_and_canonical_fullUrls()
    {
        var b = new ProbeAssembler().Wrap(new Resource[] { new Patient { Id = "p1" }, new Observation { Id = "o2" } });
        Assert.Equal("b1", b.Id);
        Assert.Equal(Bundle.BundleType.Collection, b.Type);
        Assert.Equal("https://example.org/ig/StructureDefinition/MyBundle", b.Meta.Profile.Single());
        Assert.Equal("https://example.org/ig/Patient/p1", b.Entry[0].FullUrl);   // fullUrl = {canonical}/{Type}/{id}
        Assert.Equal("https://example.org/ig/Observation/o2", b.Entry[1].FullUrl);
        Assert.Null(b.Identifier);   // omitted when not supplied
        Assert.Null(b.Timestamp);
    }

    [Fact] public void WrapBundle_carries_optional_identifier_and_timestamp()
    {
        var ts = new System.DateTimeOffset(2024, 1, 2, 3, 4, 5, System.TimeSpan.Zero);
        var b = new ProbeAssembler().Wrap(new Resource[] { new Patient { Id = "p1" } },
            id: new Identifier("urn:x", "id1"), ts: ts);
        Assert.Equal("id1", b.Identifier.Value);
        Assert.Equal(ts, b.Timestamp);
    }
}
