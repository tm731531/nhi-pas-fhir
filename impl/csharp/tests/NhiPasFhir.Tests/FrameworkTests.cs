using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using NhiPasFhir;
using NhiPasFhir.Core;
using NhiPasFhir.Plugins;
using Xunit;

public class FrameworkTests
{
    [Fact] // C6
    public void Factory_dispatches_cancer_drug()
    {
        var a = AssemblerFactory.ForCase(Samples.CancerDrugCase());
        Assert.Equal("cancer-drug", a.CaseType);
        Assert.IsType<CancerDrugAssembler>(a);
    }

    [Fact] // C7 fail-loud
    public void Factory_unregistered_throws()
    {
        var bad = new PACase("x", "nope", new Dictionary<string, string>(),
            new Dictionary<string, string>(), new Dictionary<string, double>(), "", new Dictionary<string, object>());
        Assert.Throws<KeyNotFoundException>(() => AssemblerFactory.ForCase(bad));
    }

    [Fact] // C2/C4
    public void Bundle_has_required_entries_and_absolute_fullUrls()
    {
        var b = NhiPas.Build(Samples.CancerDrugCase());
        Assert.Equal(Bundle.BundleType.Collection, b.Type);
        Assert.All(b.Entry, e => Assert.StartsWith("https://nhicore.nhi.gov.tw/pas/", e.FullUrl));
        var types = b.Entry.Select(e => e.Resource.TypeName).ToHashSet();
        foreach (var req in new[] { "Claim", "Encounter", "Patient", "Practitioner", "Organization", "MedicationRequest", "Coverage", "Observation" })
            Assert.Contains(req, types);
    }

    [Fact] // serialization works
    public void Serializes_to_fhir_json()
    {
        var json = NhiPas.ToJson(NhiPas.Build(Samples.CancerDrugCase()));
        Assert.Contains("\"resourceType\":\"Bundle\"", json);
        Assert.Contains("Claim-twpas", json);
    }
}
