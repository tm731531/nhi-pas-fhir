using System.Runtime.CompilerServices;
using Hl7.Fhir.Model;
using NhiPasFhir.Core;
using static NhiPasFhir.Core.FhirBuild;

namespace NhiPasFhir.Plugins;

// IG 對照: 電子處方箋與調劑 (tw.gov.mohw.nhi.empd 0.2.1) — 官方範例 Bundle-bun-01-ep (電子處方箋, document, 13 resources).
// Every code/system/value transcribed VERBATIM from the official example (fabricated data, no PHI).

/// <summary>電子處方箋 (e-prescription) case — reproduces the official example Bundle-bun-01-ep: a FHIR
/// *document* Bundle spined by Composition-EMPD with 6 sections (Coverage / body-weight / 2×diagnosis /
/// 2×medication), referencing Patient/Organization/Practitioner/Encounter/Observation/Condition/Coverage
/// /Medication/MedicationRequest. A different IG from pas (nhi.empd) → implements ICaseAssembler directly.</summary>
public sealed class EmpdAssembler : IgAssemblerBase
{
    public const string IgId = "tw.gov.mohw.nhi.empd#0.2.1";
    public const string Case = "e-prescription";
    public override string Ig => IgId;
    public override string CaseType => Case;
    protected override string CanonicalBase => Sys.EmpdBase;

    [ModuleInitializer]
    internal static void Register() => AssemblerFactory.Register(IgId, Case, () => new EmpdAssembler());

    // P / R / Cc come from IgAssemblerBase.

    public override Bundle Assemble(PACase c)
    {
        var patient = new Patient
        {
            Id = "pat-ep", Meta = P("Patient-EMPD"),
            Extension = { new Extension(Sys.ExtPersonAge, new Age { Value = 39, System = Sys.Ucum, Code = "a" }) },
            Identifier =
            {
                // pat-id-1 constraint: at least one 國民身分證/護照/居留證 identifier (type NNxxx/PPN/PRC).
                new Identifier
                {
                    Use = Identifier.IdentifierUse.Official,
                    Type = Cc(Sys.V2_0203, "NNxxx"),
                    System = Sys.Moi, Value = c.Patient.GetValueOrDefault("id_card", "Z199999829"),
                },
                new Identifier
                {
                    Use = Identifier.IdentifierUse.Official,
                    Type = Cc(Sys.V2_0203, "MR"),
                    System = Sys.Tmip, Value = "A12345",
                },
            },
            Name = { new HumanName { Use = HumanName.NameUse.Usual, Text = c.Patient.GetValueOrDefault("name", "甄○康") } },
            Gender = AdministrativeGender.Female,
            BirthDate = c.Patient.GetValueOrDefault("birth_date", "1985-01-02"),
        };

        var org = new Organization
        {
            Id = "org-01-ep", Meta = P("Organization-EMPD"),
            Identifier = { new Identifier { Type = Cc(Sys.V2_0203, "PRN"), System = Sys.EmpdOrgIdTw, Value = "3531020884" } },
            Name = "洪文武診所",
            Telecom = { new ContactPoint { System = ContactPoint.ContactPointSystem.Phone, Value = "02-29765731", Use = ContactPoint.ContactPointUse.Work } },
            Address = { new Address { Use = Address.AddressUse.Work, Text = "新北市三重區集美街162號1樓" } },
        };

        var doctor = new Practitioner
        {
            Id = "pra-02-ep", Meta = P("Practitioner-EMPD"),
            Identifier = { new Identifier { Type = Cc(Sys.V2_0203, "MD"), System = Sys.DepMohwDoma, Value = "DC001" } },
            Name = { new HumanName { Use = HumanName.NameUse.Official, Text = "洪文武" } },
            Telecom = { new ContactPoint { System = ContactPoint.ContactPointSystem.Phone, Value = "02-29765731", Use = ContactPoint.ContactPointUse.Work } },
            Qualification =
            {
                new Practitioner.QualificationComponent
                {
                    Identifier = { new Identifier { Use = Identifier.IdentifierUse.Official, System = Sys.CdmisFda, Value = "SCM8888888888" } },
                    Code = Cc(Sys.Snomed, "158965000"),
                },
            },
        };

        var enc = new Encounter
        {
            Id = "enc-01-ep", Meta = P("Encounter-EMPD"),
            Identifier =
            {
                new Identifier { System = Sys.EmpdMedicalEncounterId, Value = "1101020012B234567890" },
                new Identifier { System = Sys.EmpdFuncSequenceNumber, Value = "001" },
            },
            Status = Encounter.EncounterStatus.Finished,
            Class = new Coding(Sys.EmpdOutpatientCaseType, "01") { Display = "西醫一般案件" },
            Type = { Cc(Sys.EmpdPartCode, "A12", "醫學中心；一般門診") },
            ServiceType = Cc(Sys.ServiceDeptTreatmentNhiTw, "00", "不分科"),
            Subject = R("Patient/pat-ep"),
            Period = new Period { Start = "2026-07-21" },
        };

        var obs = new Observation
        {
            Id = "obs-ep", Meta = P("Observation-EMPD-BodyWeight"),
            Status = ObservationStatus.Final,
            Category = { Cc(Sys.ObsCategory, "vital-signs", "Vital Signs") },
            Code = Cc(Sys.Loinc, "29463-7", "Body weight"),
            Subject = R("Patient/pat-ep"),
            Effective = new FhirDateTime("2023-12-21"),
            Value = new Quantity { Value = 50, Unit = "kg", System = Sys.Ucum, Code = "kg" },
        };

        var cond01 = new Condition
        {
            Id = "con-01-ep", Meta = P("Condition-EMPD"),
            ClinicalStatus = Cc(Sys.ConditionClinical, "active", "Active"),
            Category = { Cc(Sys.Loinc, "29548-5", "Diagnosis Narrative") },
            Code = Cc(Sys.Icd10cmTw, "H10.30", "未明示側性之急性結膜炎"),
            Subject = R("Patient/pat-ep"),
        };

        var cond05 = new Condition
        {
            Id = "con-05-ep", Meta = P("Condition-EMPD"),
            ClinicalStatus = Cc(Sys.ConditionClinical, "active", "Active"),
            Category = { Cc(Sys.Loinc, "29548-5", "Diagnosis Narrative") },
            Code = Cc(Sys.Icd10cmTw, "F90.0", "注意力不足過動症，不專注主顯型"),
            Subject = R("Patient/pat-ep"),
        };

        var cov = new Coverage
        {
            Id = "cov-01-ep", Meta = P("Coverage-EMR"),
            Extension = { new Extension(Sys.EmpdExtPaymentCategory, Cc(Sys.EmpdPaymentCategory, "4", "普通疾病")) },
            Status = FinancialResourceStatusCodes.Active,
            Type = Cc(Sys.EmpdNhiIdentityType, "00", "健保", text: "健保"),
            Beneficiary = R("Patient/pat-ep"),
            Payor = { R("Organization/org-nhi-ep") },
        };

        var med01 = new Medication
        {
            Id = "med-01-ep", Meta = P("Medication-EMPD"),
            Code = Cc(Sys.EmpdNhiMedicationCs, "A000015421", "YEN KUANG EYE DROPS", text: "SULFAMETHOXAZOLE SODIUM / 磺胺甲噁唑鈉"),
            Form = Cc(Sys.OrderableDrugForm, "OPDROP", text: "Ophthalmic Drops"),
            Ingredient =
            {
                new Medication.IngredientComponent
                {
                    Item = Cc(Sys.Snomed, "363528007", "Sulfamethoxazole (substance)", text: "Sulfamethoxazole Sodium"),
                    Strength = new Ratio
                    {
                        Numerator = new Quantity { Value = 20, Unit = "mg", System = Sys.Ucum, Code = "mg" },
                        Denominator = new Quantity { Value = 1, Unit = "ml", System = Sys.Ucum, Code = "ml" },
                    },
                },
            },
        };

        var med07 = new Medication
        {
            Id = "med-07-ep", Meta = P("Medication-EMPD"),
            Code = Cc(Sys.EmpdNhiMedicationCs, "BC27080100", text: "Methylphenidate Hydrochloride"),
            Form = Cc(Sys.OrderableDrugForm, "TAB", text: "錠劑"),
            Ingredient =
            {
                new Medication.IngredientComponent
                {
                    Item = Cc(Sys.Snomed, "42163009", "Methylphenidate hydrochloride (substance)", text: "Methylphenidate hydrochloride"),
                    Strength = new Ratio
                    {
                        Numerator = new Quantity { Value = 10, Unit = "mg", System = Sys.Ucum, Code = "mg" },
                        Denominator = new Quantity { Value = 1, Unit = "Tablet", System = Sys.Ucum, Code = "{tbl}" },
                    },
                },
            },
        };

        var medReq01 = new MedicationRequest
        {
            Id = "med-req-01-ep", Meta = P("MedicationRequest-EMPD"),
            Extension = { new Extension(Sys.EmpdExtTotalDuration, new PositiveInt(7)) },
            Identifier =
            {
                new Identifier { Use = Identifier.IdentifierUse.Usual, System = Sys.EmpdPrescriptionId, Value = "Med000001" },
                new Identifier { Use = Identifier.IdentifierUse.Secondary, Value = "1" },
            },
            Status = MedicationRequest.MedicationrequestStatus.Active,
            Intent = MedicationRequest.MedicationRequestIntent.Order,
            Category =
            {
                Cc(Sys.EmpdTypeOfPrescription, "A"),
                Cc(Sys.EmpdOrderType, "1"),
                Cc(Sys.EmpdSelfpayStatus, "N", "非自費"),
            },
            Medication = R("Medication/med-01-ep"),
            Subject = R("Patient/pat-ep"),
            Insurance = { R("Coverage/cov-01-ep") },
            DosageInstruction =
            {
                new Dosage
                {
                    Timing = new Timing { Repeat = new Timing.RepeatComponent { Frequency = 3 }, Code = Cc(Sys.GtsAbbrev, "TID") },
                    Route = Cc(Sys.MedicationPathTw, "OD"),
                    DoseAndRate = { new Dosage.DoseAndRateComponent { Dose = new Quantity { Value = 1, Unit = "drop", System = Sys.Ucum, Code = "[drp]" } } },
                },
            },
            DispenseRequest = new MedicationRequest.DispenseRequestComponent
            {
                ValidityPeriod = new Period { Start = "2026-07-21", End = "2026-07-24" },
                NumberOfRepeatsAllowed = 1,
                Quantity = new Quantity { Value = 5, Unit = "mL", System = Sys.Ucum, Code = "mL" },
                ExpectedSupplyDuration = new Duration { Value = 3 },
            },
            Substitution = new MedicationRequest.SubstitutionComponent
            {
                Allowed = new FhirBoolean(true),
                Reason = new CodeableConcept { Text = "不可替代時始需註明" },
            },
        };

        var medReq07 = new MedicationRequest
        {
            Id = "med-req-07-ep", Meta = P("MedicationRequest-EMPD"),
            Extension = { new Extension(Sys.EmpdExtTotalDuration, new PositiveInt(28)) },
            Identifier =
            {
                new Identifier { Use = Identifier.IdentifierUse.Usual, System = Sys.EmpdPrescriptionId, Value = "Med000004" },
                new Identifier { Use = Identifier.IdentifierUse.Secondary, Value = "1" },
            },
            Status = MedicationRequest.MedicationrequestStatus.Active,
            Intent = MedicationRequest.MedicationRequestIntent.Order,
            Category =
            {
                Cc(Sys.EmpdTypeOfPrescription, "D"),
                Cc(Sys.EmpdOrderType, "1"),
                Cc(Sys.EmpdSelfpayStatus, "N", "非自費"),
            },
            Medication = R("Medication/med-07-ep"),
            Subject = R("Patient/pat-ep"),
            Insurance = { R("Coverage/cov-01-ep") },
            DosageInstruction =
            {
                new Dosage
                {
                    Timing = new Timing { Repeat = new Timing.RepeatComponent { Frequency = 2 }, Code = Cc(Sys.GtsAbbrev, "BID") },
                    Route = Cc(Sys.MedicationPathTw, "PO"),
                    DoseAndRate = { new Dosage.DoseAndRateComponent { Dose = new Quantity { Value = 1, Unit = "tablet", System = Sys.Ucum, Code = "{tbl}" } } },
                },
            },
            DispenseRequest = new MedicationRequest.DispenseRequestComponent
            {
                ValidityPeriod = new Period { Start = "2026-08-24", End = "2026-08-26" },
                NumberOfRepeatsAllowed = 1,
                Quantity = new Quantity { Value = 14, Unit = "tablet", System = Sys.Ucum, Code = "{tbl}" },
                ExpectedSupplyDuration = new Duration { Value = 7 },
            },
        };

        var comp = new Composition
        {
            Id = "com-01-ep", Meta = P("Composition-EMPD"),
            Status = CompositionStatus.Final,
            Type = Cc(Sys.Loinc, "57833-6", "Prescription for medication"),
            Subject = R("Patient/pat-ep"),
            Encounter = R("Encounter/enc-01-ep"),
            Date = "2024-02-19T14:30:00+01:00",
            Author = { R("Organization/org-01-ep"), R("Practitioner/pra-02-ep") },
            Title = "電子處方箋",
            Custodian = R("Organization/org-01-ep"),
            Section =
            {
                Section("29762-2", "Social history Narrative", "Coverage/cov-01-ep"),
                Section("85353-1", "Vital signs, weight, height, head circumference, oxygen saturation and BMI panel", "Observation/obs-ep"),
                Section("29548-5", "Diagnosis Narrative", "Condition/con-01-ep"),
                Section("29548-5", null, "Condition/con-05-ep"),
                Section("29551-9", "Medication prescribed Narrative Narrative", "MedicationRequest/med-req-01-ep",
                    new Extension(Sys.EmpdExtCombinedPrescriptionNote, new FhirBoolean(true))),
                Section("29551-9", null, "MedicationRequest/med-req-07-ep"),
            },
        };

        // Entry order mirrors the official example exactly. (Coverage.payor → Organization/org-nhi-ep is a
        // dangling reference in the official example too — it is NOT a bundle entry there, so we omit it.)
        var ordered = new List<Resource> { comp, patient, org, doctor, enc, obs, cond01, cond05, cov, med01, med07, medReq01, medReq07 };
        return WrapBundle("bun-01-ep", "Bundle-EMPD", Bundle.BundleType.Document, ordered,
            identifier: new Identifier { System = Sys.EmpdPrescriptionId, Value = "Med000001" },
            timestamp: new DateTimeOffset(2026, 8, 31, 14, 30, 0, TimeSpan.FromHours(1)));
    }

    private static Composition.SectionComponent Section(string loinc, string? text, string entryRef, Extension? ext = null)
    {
        var s = new Composition.SectionComponent { Code = Cc(Sys.Loinc, loinc, text: text) };
        if (ext != null) s.Extension.Add(ext);
        s.Entry.Add(R(entryRef));
        return s;
    }
}
