using NhiPasFhir.MediaDeclaration;

namespace NhiPasFhir.Ingest.NhiUpload;

/// <summary>Normalizes a 每日上傳 record (MSH/MB1/MB2) into the 媒體申報 MediaRecord vocabulary (t/d/p) so
/// MediaDeclarationConverter is reused unchanged. Every target is sourced (see spec/docs and the plan's
/// translation table); 姓名 (D20) and 藥品名稱 (p3) have no faithful MediaRecord/converter target and are
/// intentionally NOT mapped — mapping them would require inventing a field, which the repo's #1 rule forbids.</summary>
public static class NhiUploadToMediaRecord
{
    public static MediaRecord Map(NhiUploadRecord rec)
    {
        // 身分證 is the Patient primary key — fail loud rather than emit a blank/fabricated identifier.
        if (string.IsNullOrWhiteSpace(rec.Mb1.A12))
            throw new ArgumentException("每日上傳 record missing required 身分證 (MB1.A12); cannot build Patient identifier", nameof(rec));

        var m = new MediaRecord();

        // 總表段 t
        Put(m.Summary, "t2", rec.Msh.H1);   // 醫事機構代號 -> 服務機構代號
        Put(m.Summary, "t3", rec.Msh.H2);   // 費用年月 (YYYMM)

        // 點數清單段 d
        Put(m.Case, "d3", rec.Mb1.A12);     // 身分證
        Put(m.Case, "d11", rec.Mb1.A13);    // 出生日期 (ROC)
        var visit = Ymd(rec.Mb1.A17);       // 就診日期時間 -> YYYMMDD
        Put(m.Case, "d9", visit);           // 就醫日期
        Put(m.Case, "d10", visit);          // 單次就醫: 迄日 = 起日
        Put(m.Case, "d29", rec.Mb1.A18);    // 就醫序號 (IC 卡)
        Put(m.Case, "d19", rec.Mb1.D19);    // 主診斷

        // 醫令清單段 p
        var seq = 1;
        foreach (var o in rec.Orders)
        {
            var p = new Dictionary<string, string>();
            Put(p, "p13", seq.ToString()); // 醫令序 (generated — 每日上傳 MB2 has none)
            Put(p, "p3", o.P1);            // 醫令類別
            Put(p, "p4", o.P2);            // 醫令代碼
            Put(p, "p7", o.P5);            // 頻率
            Put(p, "p9", o.P6);            // 途徑
            Put(p, "p10", o.P7);           // 總量
            Put(p, "p11", o.P8);           // 單價
            // NOT mapped: o.P3 藥品名稱 (converter keys on code p4, not name) — TODO if names ever needed.
            m.Orders.Add(p);
            seq++;
        }
        return m;
    }

    /// <summary>ROC YYYMMDD(HHMMSS) -> YYYMMDD (drop any time); pass through shorter/empty unchanged.</summary>
    private static string Ymd(string roc) => roc.Length >= 7 ? roc[..7] : roc;

    private static void Put(Dictionary<string, string> d, string key, string value)
    {
        if (!string.IsNullOrEmpty(value)) d[key] = value;
    }
}
