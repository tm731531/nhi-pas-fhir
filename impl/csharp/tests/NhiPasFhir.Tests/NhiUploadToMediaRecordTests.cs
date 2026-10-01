using System;
using NhiPasFhir.Ingest.NhiUpload;
using Xunit;

// 每日上傳 (MSH/MB1/MB2) -> 媒體申報 MediaRecord (t/d/p), so the existing converter is reused unchanged.
public class NhiUploadToMediaRecordTests
{
    private static NhiUploadRecord Sample() => new()
    {
        Msh = new Msh { H1 = "1234567890", H2 = "11305" },
        Mb1 = new Mb1 { A12 = "A123456789", A13 = "0850312", A17 = "11305201030", A18 = "0001", D19 = "E1140" },
        Orders =
        {
            new Mb2 { P1 = "1", P2 = "BC12345100", P5 = "TIDPC", P6 = "PO", P7 = "30", P8 = "5" },
            new Mb2 { P1 = "2", P2 = "47029C", P7 = "1", P8 = "200" },
        },
    };

    [Fact]
    public void Maps_header_case_and_order_fields_to_media_ids()
    {
        var m = NhiUploadToMediaRecord.Map(Sample());
        Assert.Equal("1234567890", m.T("t2"));
        Assert.Equal("A123456789", m.D("d3"));
        Assert.Equal("0850312", m.D("d11"));
        Assert.Equal("1130520", m.D("d9"));     // A17 first 7 chars (YYYMMDD), time dropped
        Assert.Equal("0001", m.D("d29"));
        Assert.Equal("E1140", m.D("d19"));
        Assert.Equal(2, m.Orders.Count);
        Assert.Equal("1", m.Orders[0]["p3"]);   // 醫令類別 from MB2.p1
        Assert.Equal("BC12345100", m.Orders[0]["p4"]); // code from MB2.p2
        Assert.Equal("TIDPC", m.Orders[0]["p7"]);      // freq from MB2.p5
        Assert.Equal("PO", m.Orders[0]["p9"]);         // route from MB2.p6
        Assert.Equal("30", m.Orders[0]["p10"]);        // qty from MB2.p7
        Assert.Equal("5", m.Orders[0]["p11"]);         // price from MB2.p8
        Assert.Equal("2", m.Orders[1]["p3"]);
    }

    [Fact]
    public void Missing_national_id_fails_loud()
    {
        var bad = Sample();
        bad.Mb1.A12 = "";
        var ex = Assert.Throws<ArgumentException>(() => NhiUploadToMediaRecord.Map(bad));
        Assert.Contains("A12", ex.Message);   // names the missing field — never a blank Patient id
    }
}
