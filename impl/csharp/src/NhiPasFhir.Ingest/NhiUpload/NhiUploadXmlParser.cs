using System.Text;
using System.Xml.Linq;

namespace NhiPasFhir.Ingest.NhiUpload;

/// <summary>Parses a 健保署 每日上傳 XML file (`RECS>REC>MSH/MB1/MB2`, Big5/cp950) into NhiUploadRecords.
/// Element names follow the upload format (go-tw-his-parser his_import.go). Decoding is Big5 — a UTF-8
/// assumption would mojibake Chinese 姓名/診斷, so the Big5 provider is registered and used explicitly.</summary>
public static class NhiUploadXmlParser
{
    static NhiUploadXmlParser() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public static List<NhiUploadRecord> ParseFile(string path) => Parse(File.ReadAllBytes(path));

    public static List<NhiUploadRecord> Parse(byte[] big5Bytes)
    {
        var xml = Encoding.GetEncoding(950).GetString(big5Bytes);
        var root = XDocument.Parse(xml).Root
                   ?? throw new InvalidOperationException("每日上傳 XML has no root RECS element");

        var recs = new List<NhiUploadRecord>();
        foreach (var rec in root.Elements("REC"))
        {
            var msh = rec.Element("MSH");
            var mb1 = rec.Element("MB1");
            var r = new NhiUploadRecord
            {
                Msh = new Msh { H1 = V(msh, "h1"), H2 = V(msh, "h2"), H3 = V(msh, "h3") },
                Mb1 = new Mb1
                {
                    A01 = V(mb1, "A01"), A11 = V(mb1, "A11"), A12 = V(mb1, "A12"), A13 = V(mb1, "A13"),
                    A14 = V(mb1, "A14"), A17 = V(mb1, "A17"), A18 = V(mb1, "A18"), A23 = V(mb1, "A23"),
                    D19 = V(mb1, "D19"), D20 = V(mb1, "D20"), D31 = V(mb1, "D31"), D32 = V(mb1, "D32"),
                },
            };
            foreach (var mb2 in rec.Elements("MB2"))
                r.Orders.Add(new Mb2
                {
                    P1 = V(mb2, "p1"), P2 = V(mb2, "p2"), P3 = V(mb2, "p3"), P5 = V(mb2, "p5"),
                    P6 = V(mb2, "p6"), P7 = V(mb2, "p7"), P8 = V(mb2, "p8"),
                    D27 = V(mb2, "D27"), D36 = V(mb2, "D36"),
                });
            recs.Add(r);
        }
        return recs;
    }

    private static string V(XElement? parent, string name) => parent?.Element(name)?.Value?.Trim() ?? "";
}
