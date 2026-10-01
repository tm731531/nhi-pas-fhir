using PCSC;

namespace NhiPasFhir.CardReader;

/// <summary>PC/SC transport for the 健保卡 基本資料段. SELECT (健保 AID) + READ (GET DATA) APDUs are
/// transcribed verbatim from tw-nhi-icc-service (magiclen, MIT) src/card/mod.rs — do not edit without
/// re-verifying against a real card (a wrong APDU can return garbage or lock a card). Reads only the basic
/// segment (no 醫事人員卡 needed); 就醫序號/寫卡/上傳 need HCA+SAM and are out of scope.</summary>
public sealed class NhiCardReader
{
    // Source: tw-nhi-icc-service (MIT), src/card/mod.rs APDU_SELECT / APDU_READ.
    public static readonly byte[] ApduSelect =
        { 0x00, 0xA4, 0x04, 0x00, 0x10, 0xD1, 0x58, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0x00 };
    public static readonly byte[] ApduRead = { 0x00, 0xCA, 0x11, 0x00, 0x02, 0x00, 0x00 };

    public IReadOnlyList<string> ListReaders()
    {
        using var ctx = ContextFactory.Instance.Establish(SCardScope.System);
        return ctx.GetReaders();
    }

    /// <summary>Connect to the first reader, SELECT + READ the basic segment, parse it. Throws if no reader,
    /// no card, or the card does not respond with a valid basic segment (fail loud — never a fake Patient).</summary>
    public NhiCardBasic ReadFirstCard()
    {
        using var ctx = ContextFactory.Instance.Establish(SCardScope.System);
        var readers = ctx.GetReaders();
        if (readers.Length == 0) throw new InvalidOperationException("no PC/SC reader attached");

        using var reader = ctx.ConnectReader(readers[0], SCardShareMode.Shared, SCardProtocol.Any);

        var resp = new byte[258];
        var sel = reader.Transmit(ApduSelect, resp);
        if (sel < 2) throw new InvalidOperationException("健保卡 SELECT failed (unsupported reader or no card)");

        var read = reader.Transmit(ApduRead, resp);
        if (read < 57) throw new InvalidOperationException($"健保卡 READ returned {read} bytes (<57); not a 健保卡?");

        // Drop the trailing status word (SW1 SW2) if present beyond the 57-byte segment.
        var segment = resp[..57];
        return NhiCardBasicParser.Parse(segment);
    }
}
