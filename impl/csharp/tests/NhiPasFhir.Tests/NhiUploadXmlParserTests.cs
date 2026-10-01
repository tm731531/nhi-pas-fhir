using System.Text;
using NhiPasFhir.Ingest.NhiUpload;
using Xunit;

// 健保署 每日上傳 XML is Big5 (cp950). Parser must decode it and split REC records faithfully.
public class NhiUploadXmlParserTests
{
    // Fabricated two-REC upload. Built as a UTF-8 string, encoded to Big5 bytes to exercise the real path.
    private const string Xml =
        "<?xml version=\"1.0\" encoding=\"Big5\"?>" +
        "<RECS>" +
          "<REC>" +
            "<MSH><h1>1234567890</h1><h2>11305</h2><h3>1</h3></MSH>" +
            "<MB1><A12>A123456789</A12><A13>0850312</A13><A17>1130520</A17><A18>0001</A18><D19>E1140</D19><D20>王小明</D20></MB1>" +
            "<MB2><p1>1</p1><p2>BC12345100</p2><p3>測試藥品</p3><p5>TIDPC</p5><p6>PO</p6><p7>30</p7><p8>5</p8></MB2>" +
            "<MB2><p1>2</p1><p2>47029C</p2><p7>1</p7><p8>200</p8></MB2>" +
          "</REC>" +
          "<REC>" +
            "<MSH><h1>1234567890</h1><h2>11305</h2><h3>1</h3></MSH>" +
            "<MB1><A12>B234567890</A12><A13>0900101</A13><A17>1130521</A17><D19>I10</D19><D20>陳大文</D20></MB1>" +
            "<MB2><p1>1</p1><p2>AC54321000</p2><p7>14</p7><p8>3</p8></MB2>" +
          "</REC>" +
        "</RECS>";

    private static byte[] Big5() { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); return Encoding.GetEncoding(950).GetBytes(Xml); }

    [Fact]
    public void Parses_each_rec_with_its_mb2_lines()
    {
        var recs = NhiUploadXmlParser.Parse(Big5());
        Assert.Equal(2, recs.Count);
        Assert.Equal("A123456789", recs[0].Mb1.A12);
        Assert.Equal(2, recs[0].Orders.Count);
        Assert.Single(recs[1].Orders);
        Assert.Equal("AC54321000", recs[1].Orders[0].P2);
    }

    [Fact]
    public void Decodes_big5_chinese_without_mojibake()
    {
        var recs = NhiUploadXmlParser.Parse(Big5());
        Assert.Equal("王小明", recs[0].Mb1.D20);   // SC-005: exact characters, no mojibake
        Assert.Equal("陳大文", recs[1].Mb1.D20);
    }
}
