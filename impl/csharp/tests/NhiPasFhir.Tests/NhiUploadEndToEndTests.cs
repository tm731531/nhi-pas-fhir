using System;
using System.IO;
using System.Linq;
using System.Text;
using Hl7.Fhir.Model;
using NhiPasFhir;
using NhiPasFhir.Core;
using NhiPasFhir.Ingest.NhiUpload;
using Xunit;

// Full ingest thread: Big5 每日上傳 XML -> parser -> mapper -> existing converter -> FHIR Bundle.
public class NhiUploadEndToEndTests
{
    private const string Xml =
        "<?xml version=\"1.0\" encoding=\"Big5\"?>" +
        "<RECS><REC>" +
          "<MSH><h1>1234567890</h1><h2>11305</h2><h3>1</h3></MSH>" +
          "<MB1><A12>A123456789</A12><A13>0850312</A13><A17>11305201030</A17><A18>0001</A18><D19>E1140</D19><D20>王小明</D20></MB1>" +
          "<MB2><p1>1</p1><p2>BC12345100</p2><p3>測試藥品</p3><p5>TIDPC</p5><p6>PO</p6><p7>30</p7><p8>5</p8></MB2>" +
          "<MB2><p1>2</p1><p2>47029C</p2><p7>1</p7><p8>200</p8></MB2>" +
        "</REC></RECS>";

    private static byte[] Big5()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(950).GetBytes(Xml);
    }

    [Fact]
    public void Produces_patient_claim_and_orders()
    {
        var bundle = NhiUploadPipeline.ToFhir(Big5());
        var patient = bundle.Entry.Select(e => e.Resource).OfType<Patient>().Single();
        Assert.Equal("A123456789", patient.Identifier.Single().Value);
        Assert.Equal("1996-03-12", patient.BirthDate);           // A13 ROC -> 西元
        var claim = bundle.Entry.Select(e => e.Resource).OfType<Claim>().Single();
        Assert.Equal(2, claim.Item.Count);                       // two MB2 lines
        Assert.Single(bundle.Entry.Select(e => e.Resource).OfType<MedicationRequest>()); // p1=1
        Assert.Single(bundle.Entry.Select(e => e.Resource).OfType<Procedure>());          // p1=2
    }

    [Fact]
    public void Output_is_structurally_valid_base_r4()
    {
        // No 媒體申報/上傳 FHIR IG exists -> gate is base-R4 structural validity via strict Firely reparse.
        var json = NhiPas.ToJson(NhiUploadPipeline.ToFhir(Big5()));
        var reparsed = FhirJson.Parse<Bundle>(json);             // throws if structurally invalid
        Assert.NotEmpty(reparsed.Entry);
    }

    [Fact]
    public void Matches_golden()
    {
        var json = NhiPas.ToJson(NhiUploadPipeline.ToFhir(Big5()));
        var path = Path.Combine(AppContext.BaseDirectory, "goldens", "nhi-upload-bundle.json");
        var golden = File.ReadAllText(path);
        Assert.Equal(golden.Trim(), json.Trim());
    }
}
