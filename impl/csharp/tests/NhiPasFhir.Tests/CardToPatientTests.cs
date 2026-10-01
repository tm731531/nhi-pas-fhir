using System.Linq;
using Hl7.Fhir.Model;
using NhiPasFhir;
using NhiPasFhir.Core;
using NhiPasFhir.CardReader;
using Xunit;

// 健保卡 基本資料 -> base-R4 FHIR Patient (identifier=身分證, birthDate, gender). No profile claimed.
public class CardToPatientTests
{
    private static NhiCardBasic Card() =>
        new("000012345678", "王小明", "A123456789", "1996-03-12", CardSex.Male, "2016-06-01");

    [Fact]
    public void Maps_identifier_birthdate_and_gender()
    {
        var p = CardToPatient.ToPatient(Card());
        Assert.Equal("A123456789", p.Identifier.Single().Value);
        Assert.Equal("1996-03-12", p.BirthDate);
        Assert.Equal(AdministrativeGender.Male, p.Gender);
    }

    [Fact]
    public void Output_is_structurally_valid_base_r4()
    {
        var json = NhiPas.ToJson(CardToPatient.ToPatient(Card()));
        var reparsed = FhirJson.Parse<Patient>(json);   // throws if structurally invalid
        Assert.Equal("A123456789", reparsed.Identifier.Single().Value);
    }
}
