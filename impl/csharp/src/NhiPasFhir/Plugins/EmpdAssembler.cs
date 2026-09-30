using System.Runtime.CompilerServices;
using Hl7.Fhir.Model;
using NhiPasFhir.Core;

namespace NhiPasFhir.Plugins;

// IG 對照: 電子處方箋與調劑 (tw.gov.mohw.nhi.empd 0.1.0) — 官方範例 Bundle-bun-ep (電子處方箋, document, 10 resources).
// Every code/system/value transcribed VERBATIM from the official example (fabricated data, no PHI).

/// <summary>電子處方箋 (e-prescription) case — reproduces the official example Bundle-bun-ep: a FHIR
/// *document* Bundle spined by Composition-EMPD with 4 sections (Coverage / vital-signs / diagnosis /
/// medication), referencing Patient/Practitioner/Organization/Encounter/Observation/Condition/Coverage
/// /Medication/MedicationRequest. A different IG from pas (nhi.empd) → implements ICaseAssembler directly.</summary>
public sealed class EmpdAssembler : ICaseAssembler
{
    public const string IgId = "tw.gov.mohw.nhi.empd#0.1.0";
    public const string Case = "e-prescription";
    public string Ig => IgId;
    public string CaseType => Case;

    [ModuleInitializer]
    internal static void Register() => AssemblerFactory.Register(IgId, Case, () => new EmpdAssembler());

    private static Meta P(string name) => new() { Profile = new[] { $"{Sys.EmpdSd}/{name}" } };
    private static ResourceReference R(string typeSlashId) => new(typeSlashId);
    private static CodeableConcept Cc(string sys, string code, string? display = null, string? text = null)
        => new() { Coding = { new Coding(sys, code) { Display = display } }, Text = text };

    public Bundle Assemble(PACase c)
    {
        var patient = new Patient
        {
            Id = "pat-ep", Meta = P("Patient-EMPD"),
            Extension = { new Extension(Sys.ExtPersonAge, new Age { Value = 39, System = Sys.Ucum, Code = "a" }) },
            Identifier = { new Identifier
            {
                Use = Identifier.IdentifierUse.Official,
                Type = Cc(Sys.V2_0203, "MR", "Medical record number"),
                System = Sys.MoiSlash, Value = c.Patient.GetValueOrDefault("id_card", "Z199999829"),
            } },
            Name = { new HumanName { Use = HumanName.NameUse.Official, Text = c.Patient.GetValueOrDefault("name", "甄○康") } },
            Gender = AdministrativeGender.Female,
            BirthDate = c.Patient.GetValueOrDefault("birth_date", "1985-01-02"),
        };

        var org = new Organization
        {
            Id = "org-ep", Meta = P("Organization-EMPD"),
            Identifier = { new Identifier { Type = Cc(Sys.V2_0203, "PRN"), System = Sys.Hpio, Value = "3531020884" } },
            Name = "洪文武診所",
            Telecom = { new ContactPoint { System = ContactPoint.ContactPointSystem.Phone, Value = "02-29765731", Use = ContactPoint.ContactPointUse.Work } },
            Address = { new Address { Use = Address.AddressUse.Work, Text = "新北市三重區集美街162號1樓" } },
        };

        var doctor = new Practitioner
        {
            Id = "pra-ep", Meta = P("Practitioner-EMPD"),
            Name = { new HumanName { Use = HumanName.NameUse.Official, Text = "洪文武" } },
            Telecom = { new ContactPoint { System = ContactPoint.ContactPointSystem.Phone, Value = "02-29765731", Use = ContactPoint.ContactPointUse.Work } },
            Qualification =
            {
                new Practitioner.QualificationComponent
                {
                    Identifier = { new Identifier { Use = Identifier.IdentifierUse.Official, System = Sys.MohwSlash, Value = "A1*****8028" } },
                    Code = Cc(Sys.Snomed, "223366009"),
                },
            },
        };

        var enc = new Encounter
        {
            Id = "enc-ep", Meta = P("Encounter-EMPD"),
            Status = Encounter.EncounterStatus.Finished,
            Class = new Coding(Sys.EmpdCaseType, "01") { Display = "西醫一般案件" },
            ServiceType = Cc(Sys.Snomed, "394609007", text: "普通外科"),
            Subject = R("Patient/pat-ep"),
            Period = new Period { Start = "2023-02-23" },
        };

        var obs = new Observation
        {
            Id = "obs-ep", Meta = P("Observation-EMPD-BodyWeight"),
            Status = ObservationStatus.Final,
            Category = { Cc(Sys.ObsCategory, "vital-signs", "Vital Signs") },
            Code = Cc(Sys.Loinc, "29463-7", "Body weight"),
            Subject = R("Patient/pat-ep"),
            Effective = new FhirDateTime("2023-12-21"),
            Performer = { R("Practitioner/pra-ep") },
            Value = new Quantity { Value = 50, Unit = "kg", System = Sys.Ucum, Code = "kg" },
        };

        var cond = new Condition
        {
            Id = "con-ep", Meta = P("Condition-EMPD"),
            ClinicalStatus = Cc(Sys.ConditionClinical, "active", "Active"),
            Category = { Cc(Sys.CondCategory, "problem-list-item", "Problem List Item") },
            Code = Cc(Sys.Icd10cmTw, "A00.9", "霍亂"),
            Subject = R("Patient/pat-ep"),
            Note = { new Annotation { Text = "001" } },
        };

        var cov = new Coverage
        {
            Id = "cov-ep", Meta = P("Coverage-EMR"),
            Status = FinancialResourceStatusCodes.Active,
            Type = Cc(Sys.EmpdPaymentCategory, "1", "職業傷害"),
            Beneficiary = R("Patient/pat-ep"),
            Payor = { R("Patient/pat-ep") },
        };

        var med = new Medication
        {
            Id = "med-01-ep", Meta = P("Medication-EMPD"),
            Code = Cc(Sys.MedicationNhiTw, "A000015421", text: "YEN KUANG EYE DROPS"),
            Form = Cc(Sys.OrderableDrugForm, "TAB", text: "TAB"),
            Ingredient =
            {
                new Medication.IngredientComponent
                {
                    Item = new CodeableConcept { Coding = { new Coding { Display = "Alemtuzamab (substance)" } } },
                    Strength = new Ratio
                    {
                        Numerator = new Quantity { Value = 250, System = Sys.Ucum, Code = "mg" },
                        Denominator = new Quantity { Value = 250, System = Sys.Ucum, Code = "mg" },
                    },
                },
            },
        };

        var medReq = new MedicationRequest
        {
            Id = "med-req-01-ep", Meta = P("MedicationRequest-EMPD"),
            Extension = { new Extension(Sys.EmpdExtTotalDuration, new Quantity { Value = 7, Unit = "days" }) },
            Identifier =
            {
                new Identifier { System = Sys.MoiSlash, Value = "Med000001" },
                new Identifier { Value = "7" },
            },
            Status = MedicationRequest.MedicationrequestStatus.Active,
            Intent = MedicationRequest.MedicationRequestIntent.Order,
            Category =
            {
                Cc(Sys.EmpdTypeOfPrescription, "A"),
                Cc(Sys.EmpdOrderType, "1"),
                Cc(Sys.EmpdSelfpayStatus, "01", "非自費"),
            },
            Medication = R("Medication/med-01-ep"),
            Subject = R("Patient/pat-ep"),
            Insurance = { R("Coverage/cov-ep") },
            Note = { new Annotation { Text = "A" }, new Annotation { Text = "否" } },
            DosageInstruction =
            {
                new Dosage
                {
                    Timing = new Timing { Repeat = new Timing.RepeatComponent { Frequency = 4 } },
                    Route = Cc(Sys.MedicationPathTw, "PO"),
                    DoseAndRate = { new Dosage.DoseAndRateComponent { Dose = new Quantity { Value = 1, Unit = "顆" } } },
                },
            },
            DispenseRequest = new MedicationRequest.DispenseRequestComponent
            {
                ValidityPeriod = new Period { Start = "2024-01-12T00:00:00+08:00", End = "2024-01-19T00:00:00+08:00" },
                NumberOfRepeatsAllowed = 1,
                Quantity = new Quantity { Value = 12, Unit = "顆", System = Sys.Ucum },
                ExpectedSupplyDuration = new Duration { Value = 3 },
            },
            Substitution = new MedicationRequest.SubstitutionComponent
            {
                Allowed = new FhirBoolean(true),
                Reason = new CodeableConcept { Text = "不可替代時始需註明" },
            },
        };

        var comp = new Composition
        {
            Id = "com-ep", Meta = P("Composition-EMPD"),
            Status = CompositionStatus.Final,
            Type = Cc(Sys.Loinc, "57833-6", "Prescription for medication"),
            Subject = R("Patient/pat-ep"),
            Encounter = R("Encounter/enc-ep"),
            Date = "2024-02-19T14:30:00+01:00",
            Author = { R("Organization/org-ep"), R("Practitioner/pra-ep") },
            Title = "電子處方箋",
            Custodian = R("Organization/org-ep"),
            Section =
            {
                Section("29762-2", "Social history Narrative", "Coverage/cov-ep"),
                Section("85353-1", "Vital signs, weight, height, head circumference, oxygen saturation and BMI panel", "Observation/obs-ep"),
                Section("29548-5", "Diagnosis Narrative", "Condition/con-ep"),
                Section("29551-9", "Medication prescribed Narrative Narrative", "Medication/med-01-ep", "MedicationRequest/med-req-01-ep"),
            },
        };

        var ordered = new List<Resource> { comp, patient, org, doctor, enc, obs, cond, cov, med, medReq };
        var bundle = new Bundle
        {
            Id = "bun-ep", Meta = P("Bundle-EMPD"), Type = Bundle.BundleType.Document,
            Identifier = new Identifier { System = Sys.MoiSlash, Value = "bun-10" },
            Timestamp = new DateTimeOffset(2024, 2, 19, 14, 30, 0, TimeSpan.FromHours(1)),
        };
        foreach (var r in ordered)
            bundle.Entry.Add(new Bundle.EntryComponent { FullUrl = $"{Sys.EmpdBase}/{r.TypeName}/{r.Id}", Resource = r });
        return bundle;
    }

    private static Composition.SectionComponent Section(string loinc, string text, params string[] entryRefs)
    {
        var s = new Composition.SectionComponent { Code = Cc(Sys.Loinc, loinc, text: text) };
        foreach (var r in entryRefs) s.Entry.Add(R(r));
        return s;
    }
}
