using System.Linq;
using Hl7.Fhir.Model;
using NhiPasFhir;
using NhiPasFhir.Core;
using NhiPasFhir.MediaDeclaration;
using Xunit;

// 醫療費用申報 (媒體申報) <-> FHIR converter. Mapping verified against the spec (版更 112.08.25);
// see spec/docs/media-declaration-to-fhir.md.
public class MediaDeclarationTests
{
    private const string Sample =
        "t|t1=10|t2=1234567890|t3=11305|t4=2|t5=1|t6=1130610\n" +
        "d|d1=01|d2=000001|d3=A123456789|d11=0850312|d8=03|d9=1130520|d10=1130520|d19=E1140|d20=I10|d29=0001|d30=B234567890|d15=000\n" +
        "p|p3=1|p13=001|p4=BC12345100|p5=1|p7=TIDPC|p9=PO|p1=30|p10=30|p11=5|p12=150\n" +
        "p|p3=2|p13=002|p4=47029C|p10=1|p11=200|p12=200|p16=B234567890\n";

    [Theory]
    [InlineData("0850312", "1996-03-12")]  // 民國85 -> 1996
    [InlineData("1130520", "2024-05-20")]  // 民國113 -> 2024
    [InlineData("11305", "2024-05")]       // YYYMM
    public void RocDate_to_iso(string roc, string iso) => Assert.Equal(iso, RocDate.ToIso(roc));

    [Theory]
    [InlineData("1996-03-12", "0850312")]
    [InlineData("2024-05", "11305")]
    public void RocDate_to_roc(string iso, string roc) => Assert.Equal(roc, RocDate.ToRoc(iso));

    [Fact]
    public void Forward_maps_case_and_orders()
    {
        var bundle = MediaDeclarationConverter.ToFhir(MediaDeclarationParser.Parse(Sample));
        var patient = bundle.Entry.Select(e => e.Resource).OfType<Patient>().Single();
        Assert.Equal("A123456789", patient.Identifier.Single().Value);   // d3
        Assert.Equal("1996-03-12", patient.BirthDate);                   // d11 ROC->西元

        var claim = bundle.Entry.Select(e => e.Resource).OfType<Claim>().Single();
        Assert.Equal(ClaimUseCode.Claim, claim.Use);
        Assert.Equal("2024-06-10", claim.Created);                       // t6
        Assert.Equal(2, claim.Diagnosis.Count);                          // d19 + d20
        Assert.Equal("principal", claim.Diagnosis[0].Type.Single().Coding.Single().Code);
        Assert.Equal(2, claim.Item.Count);                               // two p-lines
        Assert.Equal(150m, ((Money)claim.Item[0].Net).Value);           // p12

        Assert.Single(bundle.Entry.Select(e => e.Resource).OfType<MedicationRequest>()); // p3=1
        Assert.Single(bundle.Entry.Select(e => e.Resource).OfType<Procedure>());          // p3=2
    }

    [Fact]
    public void Forward_output_is_cardinality_valid_base_r4()
    {
        // base R4 Claim requires insurance[1..*]; strict Firely parse throws if a required element is missing.
        var bundle = MediaDeclarationConverter.ToFhir(MediaDeclarationParser.Parse(Sample));
        var json = NhiPas.ToJson(bundle);
        var reparsed = FhirJson.Parse<Bundle>(json);   // would throw DeserializationFailedException if invalid
        Assert.Equal(bundle.Entry.Count, reparsed.Entry.Count);
    }

    [Fact]
    public void Reverse_recomputes_totals_and_preserves_verified_fields()
    {
        var bundle = MediaDeclarationConverter.ToFhir(MediaDeclarationParser.Parse(Sample));
        var rec = MediaDeclarationConverter.ToMediaRecord(bundle);

        Assert.Equal("1", rec.T("t37"));          // 件數 = 1 case
        Assert.Equal("350", rec.T("t38"));        // 點數 = Σ p12 (150+200), recomputed not trusted
        Assert.Equal("A123456789", rec.D("d3"));  // patient id round-trips
        Assert.Equal("E1140", rec.D("d19"));      // principal diagnosis round-trips
        Assert.Equal("0850312", rec.D("d11"));    // 西元->ROC back
        Assert.Equal(2, rec.Orders.Count);        // two order lines
        Assert.Equal("BC12345100", rec.Orders[0]["p4"]);
    }
}
