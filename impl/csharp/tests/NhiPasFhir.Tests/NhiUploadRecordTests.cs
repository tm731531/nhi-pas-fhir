using NhiPasFhir.Ingest.NhiUpload;
using Xunit;

// The in-memory shape of one 健保署 每日上傳 record (MSH + MB1 + MB2*).
public class NhiUploadRecordTests
{
    [Fact]
    public void Record_holds_header_case_and_order_lines()
    {
        var rec = new NhiUploadRecord
        {
            Msh = new Msh { H1 = "1234567890", H2 = "11305", H3 = "1" },
            Mb1 = new Mb1 { A12 = "A123456789", A13 = "0850312", A17 = "1130520", A18 = "0001", D19 = "E1140", D20 = "王小明" },
            Orders = { new Mb2 { P1 = "1", P2 = "BC12345100", P5 = "TIDPC", P6 = "PO", P7 = "30", P8 = "5" } },
        };

        Assert.Equal("1234567890", rec.Msh.H1);
        Assert.Equal("A123456789", rec.Mb1.A12);
        Assert.Equal("王小明", rec.Mb1.D20);
        Assert.Single(rec.Orders);
        Assert.Equal("BC12345100", rec.Orders[0].P2);
    }
}
