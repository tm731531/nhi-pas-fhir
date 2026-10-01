using System;
using System.Text;
using NhiPasFhir.CardReader;
using Xunit;

// 健保卡 基本資料段 byte layout (sourced from tw-nhi-icc-service, MIT). Parsed with fabricated bytes.
public class NhiCardBasicParserTests
{
    // Build a fabricated 57-byte basic segment: 卡號[0:12] 姓名[12:32]Big5 身分證[32:42] 生日[42:49] 性別[49] 發卡日[50:57]
    private static byte[] Segment(string cardNo, string name, string id, string birthRoc, char sex, string issueRoc)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var buf = new byte[57];
        void Put(int off, int len, byte[] src) { Array.Copy(src, 0, buf, off, Math.Min(len, src.Length)); }
        Put(0, 12, Encoding.ASCII.GetBytes(cardNo.PadRight(12)));
        Put(12, 20, Encoding.GetEncoding(950).GetBytes(name));          // Big5; remaining bytes stay 0 (NUL term)
        Put(32, 10, Encoding.ASCII.GetBytes(id));
        Put(42, 7, Encoding.ASCII.GetBytes(birthRoc));
        buf[49] = (byte)sex;
        Put(50, 7, Encoding.ASCII.GetBytes(issueRoc));
        return buf;
    }

    [Fact]
    public void Parses_all_fields_including_big5_name_and_roc_dates()
    {
        var c = NhiCardBasicParser.Parse(Segment("000012345678", "王小明", "A123456789", "0850312", 'M', "1050601"));
        Assert.Equal("000012345678", c.CardNo);
        Assert.Equal("王小明", c.FullName);                 // Big5, 0 mojibake
        Assert.Equal("A123456789", c.IdNo);
        Assert.Equal("1996-03-12", c.BirthDateIso);         // ROC 085 -> 1996
        Assert.Equal(CardSex.Male, c.Sex);
        Assert.Equal("2016-06-01", c.IssueDateIso);         // ROC 105 -> 2016
    }

    [Fact]
    public void Short_buffer_fails_loud()
    {
        var ex = Assert.Throws<ArgumentException>(() => NhiCardBasicParser.Parse(new byte[56]));
        Assert.Contains("57", ex.Message);
    }

    [Fact]
    public void Invalid_sex_byte_fails_loud()
    {
        Assert.Throws<ArgumentException>(() =>
            NhiCardBasicParser.Parse(Segment("000012345678", "王小明", "A123456789", "0850312", 'X', "1050601")));
    }
}
