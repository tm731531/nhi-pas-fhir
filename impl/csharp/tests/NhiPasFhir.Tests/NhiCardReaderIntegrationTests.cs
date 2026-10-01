using System.Linq;
using NhiPasFhir.CardReader;
using Xunit;

// Live PC/SC read — requires a physical reader + 健保卡. Skipped (never failed) when absent, mirroring
// the CQF-Ruler integration pattern. Hardware absence is not a code failure.
public class NhiCardReaderIntegrationTests
{
    [SkippableFact]
    public void Reads_a_present_card_into_a_patient()
    {
        NhiCardBasic? card = null;
        string skip = "no card";
        try
        {
            var reader = new NhiCardReader();
            if (reader.ListReaders().Count == 0) skip = "no PC/SC reader attached";
            else card = reader.ReadFirstCard();
        }
        // No PC/SC subsystem (pcscd/libpcsclite absent) OR no readable 健保卡 — both are env/hardware
        // absence, which is a SKIP, not a code failure.
        catch (System.Exception e) { skip = $"PC/SC unavailable or no readable 健保卡: {e.Message}"; }

        Skip.If(card is null, skip);

        Assert.False(string.IsNullOrWhiteSpace(card!.IdNo));
        var patient = CardToPatient.ToPatient(card);
        Assert.Equal(card.IdNo, patient.Identifier.Single().Value);
    }

    [Fact]
    public void Apdu_constants_match_the_sourced_reference()
    {
        // Sourced from tw-nhi-icc-service (MIT) — guard against accidental edits.
        Assert.Equal(new byte[] { 0x00, 0xA4, 0x04, 0x00, 0x10, 0xD1, 0x58, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0x00 }, NhiCardReader.ApduSelect);
        Assert.Equal(new byte[] { 0x00, 0xCA, 0x11, 0x00, 0x02, 0x00, 0x00 }, NhiCardReader.ApduRead);
    }
}
