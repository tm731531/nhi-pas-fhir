using System.Runtime.CompilerServices;
using Hl7.Fhir.Model;
using NhiPasFhir.Core;

namespace NhiPasFhir.Plugins;

// IG 對照: 傳染病檢驗報告 (tw.gov.mohw.cdc.twidir 0.1.1) — 官方範例 Bundle-bundle-request-ser-min (message, 10 resources).
// Every code/system/value transcribed VERBATIM from the official example (fabricated data, no PHI).

/// <summary>傳染病檢驗報告 (notifiable-disease lab report) case — reproduces the official example
/// Bundle-bundle-request-ser-min: a FHIR *message* Bundle headed by a MessageHeader (event = Laboratory
/// report) whose focus is DiagnosticReport + Patient + Observation + Condition + Device, plus Specimen +
/// two Organizations (reporting / sending hospital) + Practitioner. A CDC IG (twidir), not an NHI IG.</summary>
public sealed class TwidirAssembler : ICaseAssembler
{
    public const string IgId = "tw.gov.mohw.cdc.twidir#0.1.1";
    public const string Case = "notifiable-disease-report";
    public string Ig => IgId;
    public string CaseType => Case;

    [ModuleInitializer]
    internal static void Register() => AssemblerFactory.Register(IgId, Case, () => new TwidirAssembler());

    private static Meta P(string name) => new() { Profile = new[] { $"{Sys.TwidirSd}/{name}" } };
    private static Meta Pt(string canonical) => new() { Profile = new[] { canonical } };
    private static ResourceReference R(string typeSlashId) => new(typeSlashId);
    private static CodeableConcept Cc(string sys, string code, string? display = null, string? text = null)
        => new() { Coding = { new Coding(sys, code) { Display = display } }, Text = text };

    public Bundle Assemble(PACase c)
    {
        var patient = new Patient
        {
            Id = "patient-passport-min", Meta = P("patient-reporting"),
            Identifier =
            {
                new Identifier { Type = Cc(Sys.V2_0203, "PPN"), System = Sys.Boca, Value = c.Patient.GetValueOrDefault("id_card", "888800371") },
                new Identifier { Use = Identifier.IdentifierUse.Official, Type = Cc(Sys.V2_0203, "MR"), System = Sys.TpechSlash, Value = "88001555" },
            },
            Name = { new HumanName { Use = HumanName.NameUse.Usual, Text = c.Patient.GetValueOrDefault("name", "陳嘉明") } },
            Gender = AdministrativeGender.Male,
            BirthDate = c.Patient.GetValueOrDefault("birth_date", "1995-06-26"),
            Address = { new Address { PostalCodeElement = new FhirString { Extension = { new Extension(Sys.ExtTwPostalCode, new CodeableConcept(Sys.TwcorePostal3, "106")) } } } },
        };

        var orgSend = new Organization
        {
            Id = "organizationsendhospid-min", Meta = P("organization-sendhospid-reporting"),
            Identifier = { new Identifier { Type = Cc(Sys.V2_0203, "PRN"), System = Sys.TwcoreOrgId, Value = "0101090517" } },
            Type = { Cc(Sys.TwidirOrgType, "sendhospid") },
            Name = "臺北市立聯合醫院",
        };
        var orgHosp = new Organization
        {
            Id = "organizationhospid-min", Meta = P("organization-hospid-reporting"),
            Identifier = { new Identifier { Type = Cc(Sys.V2_0203, "PRN"), System = Sys.TwcoreOrgId, Value = "0101090517" } },
            Type = { Cc(Sys.TwidirOrgType, "hospid") },
            Name = "臺北市立聯合醫院",
        };
        var doctor = new Practitioner
        {
            Id = "practitioner-min", Meta = Pt(Sys.PractitionerTwcore),
            Identifier = { new Identifier { Use = Identifier.IdentifierUse.Official, Type = Cc(Sys.V2_0203, "MD"), System = Sys.TphMohw, Value = "KP00017" } },
            Name = { new HumanName { Use = HumanName.NameUse.Official, Text = "王依昇", Family = "Wang", Given = new[] { "Yi Sheng" } } },
        };

        var device = new Device
        {
            Id = "device-min", Meta = P("device-reporting"),
            DeviceName = { new Device.DeviceNameComponent { Name = "檢驗儀器名稱", Type = DeviceNameType.UserFriendlyName } },
            Type = Cc(Sys.TwidirDeviceType, "device"),
        };

        var specimen = new Specimen
        {
            Id = "specimen-min", Meta = P("specimen-reporting"),
            Type = Cc(Sys.TwidirLoincPartSystem, "LP7567-3"),
            ReceivedTime = "2023-04-13T18:41:08.000Z",
            Collection = new Specimen.CollectionComponent { Collected = new FhirDateTime("2023-04-13T13:06:05.000Z") },
        };

        var obs = new Observation
        {
            Id = "observation-ser-min", Meta = P("observation-reporting"),
            Identifier = { new Identifier { System = Sys.LimsCdc, Value = "99-112-061301" } },
            Status = ObservationStatus.Final,
            Code = Cc(Sys.Loinc, "22315-6", text: "Hepatitis A virus Ab.IgM"),
            Subject = R("Patient/patient-passport-min"),
            Effective = new FhirDateTime("2023-04-14T09:01:56.000Z"),
            Interpretation = { Cc(Sys.V3InterpObs, "NEG") },
            Specimen = R("Specimen/specimen-min"),
            Device = R("Device/device-min"),
        };

        var diaRep = new DiagnosticReport
        {
            Id = "diagnosticreport-ser-min", Meta = P("diagnosticreport-reporting"),
            Identifier = { new Identifier { Value = "202300000001" } },
            Status = DiagnosticReport.DiagnosticReportStatus.Final,
            Code = Cc(Sys.Loinc, "11502-2", "Laboratory report"),
            Subject = R("Patient/patient-passport-min"),
            Performer = { R("Organization/organizationhospid-min"), R("Organization/organizationsendhospid-min"), R("Practitioner/practitioner-min") },
            Result = { R("Observation/observation-ser-min") },
        };

        var cond = new Condition
        {
            Id = "condition-ser-min", Meta = P("condition-reporting"),
            Identifier = { new Identifier { Type = Cc(Sys.TwidirIdentifierType, "nidrs"), Value = "1124500128872" } },
            ClinicalStatus = Cc(Sys.ConditionClinical, "active"),
            Category = { Cc(Sys.CondCategory, "encounter-diagnosis") },
            Code = Cc(Sys.Icd10cm2021Tw, "B15", text: "急性型病毒性肝炎"),
            Subject = R("Patient/patient-passport-min"),
            Onset = new FhirDateTime("2023-04-13T10:03:45.000Z"),
            Stage = { new Condition.StageComponent { Assessment = { R("DiagnosticReport/diagnosticreport-ser-min") } } },
        };

        var header = new MessageHeader
        {
            Id = "messageheader-resquest-ser-min", Meta = P("messageheader-request-reporting"),
            Event = new Coding(Sys.Loinc, "11502-2") { Display = "Laboratory report" },
            Source = new MessageHeader.MessageSourceComponent { Endpoint = "https://tpech.gov.taipei/" },
            Focus =
            {
                R("DiagnosticReport/diagnosticreport-ser-min"), R("Patient/patient-passport-min"),
                R("Observation/observation-ser-min"), R("Condition/condition-ser-min"), R("Device/device-min"),
            },
        };

        var ordered = new List<Resource> { header, cond, diaRep, obs, specimen, patient, orgSend, orgHosp, doctor, device };
        var bundle = new Bundle
        {
            Id = "bundle-request-ser-min", Meta = P("bundle-request-reporting"), Type = Bundle.BundleType.Message,
            Identifier = new Identifier { System = "https://www.cdc.gov.tw/", Value = "01010905170415100000000" },
            Timestamp = new DateTimeOffset(2023, 4, 15, 10, 0, 0, TimeSpan.Zero),
        };
        foreach (var r in ordered)
            bundle.Entry.Add(new Bundle.EntryComponent { FullUrl = $"{Sys.TwidirBase}/{r.TypeName}/{r.Id}", Resource = r });
        return bundle;
    }
}
