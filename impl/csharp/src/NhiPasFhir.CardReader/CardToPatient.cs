using Hl7.Fhir.Model;

namespace NhiPasFhir.CardReader;

/// <summary>Builds a base-R4 FHIR Patient from a 健保卡 基本資料段 record. A card read is demographics, not
/// a PA case, so NO profile is claimed (no Patient-twpas / TWCorePatient) — just structurally-valid base R4.
/// 身分證 identifier system is the NHI national-id namespace; TODO: confirm the official canonical URL.</summary>
public static class CardToPatient
{
    // TODO: confirm official identifier system URL for the 身分證 (national id).
    private const string NationalIdSystem = "https://nhicore.nhi.gov.tw/national-id";

    public static Patient ToPatient(NhiCardBasic c) => new()
    {
        Id = "pat-card",
        Identifier = { new Identifier(NationalIdSystem, c.IdNo) },
        Name = { new HumanName { Text = c.FullName } },
        BirthDate = c.BirthDateIso,
        Gender = c.Sex == CardSex.Male ? AdministrativeGender.Male : AdministrativeGender.Female,
    };
}
