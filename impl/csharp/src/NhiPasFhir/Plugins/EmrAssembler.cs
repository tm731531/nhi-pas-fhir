using System.Runtime.CompilerServices;
using Hl7.Fhir.Model;
using NhiPasFhir.Core;

namespace NhiPasFhir.Plugins;

// IG 對照: 電子病歷交換單張 EMR (tw.gov.mohw.emr 0.2.0) — 官方範例 Bundle-example-IC (檢驗檢查, document, 8 resources).
// Every code/system/value transcribed VERBATIM from the official example (fabricated data, no PHI).
// The example mixes ABSOLUTE and RELATIVE references per field — reproduced exactly (Abs vs Rel below).

/// <summary>電子病歷交換單張 (EMR exchange sheet) — reproduces the official example Bundle-example-IC
/// (檢驗檢查 / InspectionCheck): a FHIR *document* Bundle spined by InspectionCheckComposition, referencing
/// Organization + Patient + Observation(CBC) + Specimen + two Practitioners + Encounter. A twcore/emr IG.</summary>
public sealed class EmrAssembler : ICaseAssembler
{
    public const string IgId = "tw.gov.mohw.emr#0.2.0";
    public const string Case = "inspection-check";
    public string Ig => IgId;
    public string CaseType => Case;

    [ModuleInitializer]
    internal static void Register() => AssemblerFactory.Register(IgId, Case, () => new EmrAssembler());

    private static Meta P(string name) => new() { Profile = new[] { $"{Sys.EmrSd}/{name}" } };
    private static ResourceReference Rel(string typeSlashId) => new(typeSlashId);
    private static ResourceReference Abs(string typeSlashId) => new($"{Sys.EmrBase}/{typeSlashId}");
    private static CodeableConcept Cc(string sys, string code, string? display = null, string? text = null)
        => new() { Coding = { new Coding(sys, code) { Display = display } }, Text = text };

    public Bundle Assemble(PACase c)
    {
        var org = new Organization
        {
            Id = "IC-Org1", Meta = P("InspectionCheckOrganization"),
            Identifier = { new Identifier { Value = "0401180014" } },
            Name = "國立臺灣大學醫學院附設醫院",
        };

        // Patient — NNxxx identifier carries a nested identifier-suffix extension on its Coding.code element.
        var nnxxx = new Coding(Sys.V2_0203, "NNxxx");
        var suffixExt = new Extension { Url = Sys.ExtIdentifierSuffix };
        suffixExt.Extension.Add(new Extension("suffix", new FhirString("TWN")));
        suffixExt.Extension.Add(new Extension("valueSet", new Canonical(Sys.Iso3166_1_3)));
        nnxxx.CodeElement!.Extension.Add(suffixExt);   // CodeElement is set by the Coding(system, code) ctor
        var patient = new Patient
        {
            Id = "IC-Pat2", Meta = P("InspectionCheckPatient"),
            Identifier =
            {
                new Identifier { Use = Identifier.IdentifierUse.Official, Type = Cc(Sys.V2_0203, "MR", "Medical record number"), System = Sys.IdCard, Value = "123456" },
                new Identifier { Use = Identifier.IdentifierUse.Official, Type = new CodeableConcept { Coding = { nnxxx } }, System = Sys.IdCard, Value = c.Patient.GetValueOrDefault("id_card", "A123456789") },
            },
            Active = true,
            Name = { new HumanName { Use = HumanName.NameUse.Official, Text = c.Patient.GetValueOrDefault("name", "黃睿駿") } },
            Gender = AdministrativeGender.Male,
            BirthDate = c.Patient.GetValueOrDefault("birth_date", "1999-01-01"),
        };

        var pra5 = new Practitioner
        {
            Id = "IC-Pra5", Meta = P("InspectionCheckPractitioner"),
            Identifier = { new Identifier { Use = Identifier.IdentifierUse.Official, Type = Cc(Sys.V2_0203, "MD"), System = Sys.IdCard, Value = "C12345" } },
            Name = { new HumanName { Text = "賴護士" } },
        };
        var pra7 = new Practitioner
        {
            Id = "IC-Pra7", Meta = P("InspectionCheckPractitioner"),
            Identifier = { new Identifier { Use = Identifier.IdentifierUse.Official, Type = Cc(Sys.V2_0203, "MD"), System = Sys.IdCard, Value = "AA0001" } },
            Name = { new HumanName { Text = "賴一施" } },
        };

        var specimen = new Specimen
        {
            Id = "IC-Spe4", Meta = P("InspectionCheckSpecimen"),
            Subject = Abs("Patient/IC-Pat2"),
            Collection = new Specimen.CollectionComponent { BodySite = Cc(Sys.Snomed, "106004", text: "Posterior carpal region") },
        };

        var obs = new Observation
        {
            Id = "IC-Obs3", Meta = P("InspectionCheckObservation"),
            Identifier = { new Identifier { System = Sys.Vghtpe, Value = "9876" } },
            Status = ObservationStatus.Final,
            Category = { Cc(Sys.ObsCategory, "laboratory", "Laboratory", "Laboratory") },
            Code = Cc(Sys.Loinc, "6690-2", "Leukocytes [#/volume] in Blood by Automated count", "全套血液檢查 CBC-Ｉ"),
            Subject = Rel("Patient/IC-Pat2"),
            Effective = new Period { Start = "2022-03-30", End = "2022-04-06" },
            Performer = { Rel("Practitioner/IC-Pra7") },
            Interpretation = { Cc(Sys.V3InterpObs, "RR", text: "正常") },
            Note = { new Annotation { Text = "無" } },
            BodySite = Cc(Sys.Snomed, "420135007", "Whole blood", "血液"),
            Specimen = Rel("Specimen/IC-Spe4"),
            Component =
            {
                new Observation.ComponentComponent
                {
                    Code = Cc(Sys.Loinc, "58410-2", "CBC panel - Blood by Automated count"),
                    Value = new Quantity { Value = 7.33m, Unit = "10^3/ul" },
                    ReferenceRange = { new Observation.ReferenceRangeComponent { Text = "3.8-10.0" } },
                },
            },
        };

        var enc = new Encounter
        {
            Id = "IC-Enc6", Meta = P("InspectionCheckEncounter"),
            Identifier = { new Identifier { System = Sys.Vghtpe, Value = "Encounter唯一碼" } },
            Status = Encounter.EncounterStatus.Finished,
            Class = new Coding(Sys.V3ActCode, "OBSENC") { Display = "observation encounter" },
            ServiceType = Cc(Sys.Snomed, "394609007", text: "Medical Services"),
            Subject = Abs("Patient/IC-Pat2"),
            Participant = { new Encounter.ParticipantComponent { Individual = Abs("Practitioner/IC-Pra5") } },
            Period = new Period { Start = "2022-03-30" },
            ServiceProvider = Abs("Organization/IC-Org1"),
        };

        var comp = new Composition
        {
            Id = "IC-Com8", Meta = P("InspectionCheckComposition"),
            Status = CompositionStatus.Final,
            Type = Cc(Sys.Loinc, "11502-2", "Laboratory report", "檢驗檢查"),
            Subject = Abs("Patient/IC-Pat2"),
            Encounter = Abs("Encounter/IC-Enc6"),
            Date = "2023-01-04T13:01:56+08:00",
            Author = { Abs("Organization/IC-Org1"), Abs("Practitioner/IC-Pra5"), Abs("Practitioner/IC-Pra7") },
            Title = "檢驗檢查",
            Section =
            {
                new Composition.SectionComponent
                {
                    Title = "檢驗檢查中的檢驗資料",
                    Code = Cc(Sys.Loinc, "30954-2"),
                    Entry = { Abs("Observation/IC-Obs3"), Abs("Specimen/IC-Spe4") },
                },
            },
        };

        var ordered = new List<Resource> { comp, org, patient, obs, specimen, pra5, enc, pra7 };
        var bundle = new Bundle
        {
            Id = "example-IC", Meta = P("InspectionCheckBundle"), Type = Bundle.BundleType.Document,
            Identifier = new Identifier { System = Sys.TwcoreIndex, Value = "Bundle-EMR" },
            Timestamp = new DateTimeOffset(2023, 1, 4, 13, 1, 56, TimeSpan.FromHours(8)),
        };
        foreach (var r in ordered)
            bundle.Entry.Add(new Bundle.EntryComponent { FullUrl = $"{Sys.EmrBase}/{r.TypeName}/{r.Id}", Resource = r });
        return bundle;
    }
}
