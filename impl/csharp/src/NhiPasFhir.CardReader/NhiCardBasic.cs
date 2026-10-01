using System.Text;
using NhiPasFhir.Core;

namespace NhiPasFhir.CardReader;

/// <summary>健保卡 性別.</summary>
public enum CardSex { Male, Female }

/// <summary>Parsed 健保卡 基本資料段. Fields + offsets transcribed from tw-nhi-icc-service (magiclen, MIT)
/// src/card/nhi_card_basic.rs. ROC dates are already converted to ISO (西元). No PHI — fabricated only.</summary>
public sealed record NhiCardBasic(
    string CardNo, string FullName, string IdNo, string BirthDateIso, CardSex Sex, string IssueDateIso);

/// <summary>Parses the 健保卡 基本資料段 byte buffer. Layout (source: tw-nhi-icc-service, MIT):
/// 卡號[0:12] / 姓名[12:32] Big5 NUL-terminated / 身分證[32:42] / 生日[42:49] ROC YYYMMDD / 性別[49] 'M'/'F'
/// / 發卡日[50:57] ROC YYYMMDD. Fails loud on a buffer shorter than 57 or an invalid sex byte.</summary>
public static class NhiCardBasicParser
{
    static NhiCardBasicParser() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public static NhiCardBasic Parse(byte[] data)
    {
        if (data.Length < 57)
            throw new ArgumentException($"健保卡 基本資料段 must be at least 57 bytes; got {data.Length}", nameof(data));

        var cardNo = Encoding.ASCII.GetString(data, 0, 12).Trim();

        // 姓名: Big5, NUL-terminated within [12:32].
        var nameEnd = 12;
        while (nameEnd < 32 && data[nameEnd] != 0) nameEnd++;
        var fullName = Encoding.GetEncoding(950).GetString(data, 12, nameEnd - 12).Trim();

        var idNo = Encoding.ASCII.GetString(data, 32, 10).Trim();
        var birthIso = RocDate.ToIso(Encoding.ASCII.GetString(data, 42, 7))
                       ?? throw new ArgumentException("健保卡 生日 is not a valid 民國 date", nameof(data));
        var sex = data[49] switch
        {
            (byte)'M' => CardSex.Male,
            (byte)'F' => CardSex.Female,
            _ => throw new ArgumentException($"健保卡 性別 byte invalid: 0x{data[49]:X2} (expected 'M'/'F')", nameof(data)),
        };
        var issueIso = RocDate.ToIso(Encoding.ASCII.GetString(data, 50, 7))
                       ?? throw new ArgumentException("健保卡 發卡日 is not a valid 民國 date", nameof(data));

        return new NhiCardBasic(cardNo, fullName, idNo, birthIso, sex, issueIso);
    }
}
